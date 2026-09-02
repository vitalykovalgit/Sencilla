-- The client-facing read surface: every notification flattened to the users it reaches, with
-- that user's read stamp resolved.
--
-- Safe ONLY behind the matrix constraint userId={user}.Id on resource 'notificationinboxview' —
-- the view itself contains no authorization, exactly like the CROSS JOIN it uses for system
-- notifications. Every query carries WHERE UserId = @me, which SQL Server pushes down into each
-- UNION branch, so the cross join collapses to one user before anything else runs.
--
-- Scopes 100+ are app-defined and are NOT resolved here: an app either expands them to Scope=1
-- rows when it writes, or supplies its own recipient view.
CREATE VIEW [NotificationInboxView] AS
WITH [Audience] AS (
    -- UNION, not UNION ALL: a notification reaching one user through two targets (addressed
    -- personally AND via a role) must be a single row — Id is the entity key.
    SELECT t.NotificationId, t.TargetGuid AS UserId
    FROM [NotificationTarget] t
    WHERE t.Scope = 1

    UNION

    -- Exact role match. Note this does NOT expand sec.Role.Parent, whereas the permission matrix
    -- does — "roles for audience" and "roles for permissions" are deliberately not the same set,
    -- so targeting Editor does not reach Owner.
    SELECT t.NotificationId, ur.UserId
    FROM [NotificationTarget] t
        JOIN [sec].[UserRole] ur ON ur.[Role] = t.TargetInt
    WHERE t.Scope = 2

    UNION

    SELECT t.NotificationId, u.Id
    FROM [NotificationTarget] t
        CROSS JOIN [sec].[User] u
    WHERE t.Scope = 3 AND u.DeletedDate IS NULL
)
SELECT
    n.Id,
    a.UserId,
    n.[Type],
    n.Data,
    n.CreatedDate,
    r.ReadDate,

    -- Derived because the client filters on it. A bool renders through the typed comparison path
    -- (IsUnread == True); an "is null" filter on ReadDate has no reliable representation in the
    -- query-string binder.
    CAST(CASE WHEN r.ReadDate IS NULL THEN 1 ELSE 0 END AS BIT) AS IsUnread

FROM [Audience] a
    JOIN [Notification] n ON n.Id = a.NotificationId
    OUTER APPLY (
        -- An explicit per-notification read row, or a watermark at/after this notification's
        -- creation. MAX so the two shapes coexist without a second branch downstream.
        SELECT MAX(nr.ReadDate) AS ReadDate
        FROM [NotificationRead] nr
        WHERE nr.UserId = a.UserId
          AND (nr.NotificationId = n.Id
               OR (nr.NotificationId IS NULL AND nr.ReadDate >= n.CreatedDate))
    ) r;
