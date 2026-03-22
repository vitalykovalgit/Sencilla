# Secret Scanning

This skill provides procedural guidance for configuring GitHub secret scanning — detecting leaked credentials, preventing secret pushes, defining custom patterns, and managing alerts.

## When to Use This Skill

Use this skill when the request involves:

- Enabling or configuring secret scanning for a repository or organization
- Setting up push protection to block secrets before they reach the repository
- Defining custom secret patterns with regular expressions
- Resolving a blocked push from the command line
- Triaging, dismissing, or remediating secret scanning alerts
- Configuring delegated bypass for push protection
- Excluding directories from secret scanning via `secret_scanning.yml`
- Understanding alert types (user, partner, push protection)
- Enabling validity checks or extended metadata checks

## How Secret Scanning Works

Secret scanning automatically detects exposed credentials across:

- Entire Git history on all branches
- Issue descriptions, comments, and titles (open and closed)
- Pull request titles, descriptions, and comments
- GitHub Discussions titles, descriptions, and comments
- Wikis and secret gists

### Availability

| Repository Type | Availability |
|---|---|
| Public repos | Automatic, free |
| Private/internal (org-owned) | Requires GitHub Secret Protection on Team/Enterprise Cloud |
| User-owned | Enterprise Cloud with Enterprise Managed Users |

## Core Workflow — Enable Secret Scanning

### Step 1: Enable Secret Protection

1. Navigate to repository **Settings** → **Advanced Security**
2. Click **Enable** next to "Secret Protection"
3. Confirm by clicking **Enable Secret Protection**

For organizations, use security configurations to enable at scale:
- Settings → Advanced Security → Global settings → Security configurations

### Step 2: Enable Push Protection

Push protection blocks secrets during the push process — before they reach the repository.

1. Navigate to repository **Settings** → **Advanced Security**
2. Enable "Push protection" under Secret Protection

Push protection blocks secrets in:
- Command line pushes
- GitHub UI commits
- File uploads
- REST API requests
- REST API content creation endpoints

### Step 3: Configure Exclusions (Optional)

Create `.github/secret_scanning.yml` to auto-close alerts for specific directories:

```yaml
paths-ignore:
  - "docs/**"
  - "test/fixtures/**"
  - "**/*.example"
```

**Limits:**
- Maximum 1,000 entries in `paths-ignore`
- File must be under 1 MB
- Excluded paths also skip push protection checks

**Best practices:**
- Be as specific as possible with exclusion paths
- Add comments explaining why each path is excluded
- Review exclusions periodically — remove stale entries
- Inform the security team about exclusions

### Step 4: Enable Additional Features (Optional)

**Non-provider patterns** — detect private keys, connection strings, generic API keys:
- Settings → Advanced Security → enable "Scan for non-provider patterns"

**AI-powered generic secret detection** — uses Copilot to detect unstructured secrets like passwords:
- Settings → Advanced Security → enable "Use AI detection"

**Validity checks** — verify if detected secrets are still active:
- Settings → Advanced Security → enable "Validity checks"
- GitHub periodically tests detected credentials against provider APIs
- Status shown in alert: `active`, `inactive`, or `unknown`

**Extended metadata checks** — additional context about who owns a secret:
- Requires validity checks to be enabled first
- Helps prioritize remediation and identify responsible teams

## Core Workflow — Resolve Blocked Pushes

When push protection blocks a push from the command line:

### Option A: Remove the Secret

**If the secret is in the latest commit:**
```bash
# Remove the secret from the file
# Then amend the commit
git commit --amend --all
git push
```

**If the secret is in an earlier commit:**
```bash
# Find the earliest commit containing the secret
git log

# Start interactive rebase before that commit
git rebase -i <COMMIT-ID>~1

# Change 'pick' to 'edit' for the offending commit
# Remove the secret, then:
git add .
git commit --amend
git rebase --continue
git push
```

### Option B: Bypass Push Protection

1. Visit the URL returned in the push error message (as the same user)
2. Select a bypass reason:
   - **It's used in tests** — alert created and auto-closed
   - **It's a false positive** — alert created and auto-closed
   - **I'll fix it later** — open alert created
3. Click **Allow me to push this secret**
4. Re-push within 3 hours

### Option C: Request Bypass Privileges

If delegated bypass is enabled and you lack bypass privileges:
1. Visit the URL from the push error
2. Add a comment explaining why the secret is safe
3. Click **Submit request**
4. Wait for email notification of approval/denial
5. If approved, push the commit; if denied, remove the secret

> For detailed bypass and delegated bypass workflows, search `references/push-protection.md`.

## Custom Patterns

Define organization-specific secret patterns using regular expressions.

### Quick Setup

1. Settings → Advanced Security → Custom patterns → **New pattern**
2. Enter pattern name and regex for secret format
3. Add a sample test string
4. Click **Save and dry run** to test (up to 1,000 results)
5. Review results for false positives
6. Click **Publish pattern**
7. Optionally enable push protection for the pattern

### Scopes

Custom patterns can be defined at:
- **Repository level** — applies to that repo only
- **Organization level** — applies to all repos with secret scanning enabled
- **Enterprise level** — applies across all organizations

### Copilot-Assisted Pattern Generation

Use Copilot secret scanning to generate regex from a text description of the secret type, including optional example strings.

> For detailed custom pattern configuration, search `references/custom-patterns.md`.

## Alert Management

### Alert Types

| Type | Description | Visibility |
|---|---|---|
| **User alerts** | Secrets found in repository | Security tab |
| **Push protection alerts** | Secrets pushed via bypass | Security tab (filter: `bypassed: true`) |
| **Partner alerts** | Secrets reported to provider | Not shown in repo (provider-only) |

### Alert Lists

- **Default alerts** — supported provider patterns and custom patterns
- **Generic alerts** — non-provider patterns and AI-detected secrets (limited to 5,000 per repo)

### Remediation Priority

1. **Rotate the credential immediately** — this is the critical action
2. Review the alert for context (location, commit, author)
3. Check validity status: `active` (urgent), `inactive` (lower priority), `unknown`
4. Remove from Git history if needed (time-intensive, often unnecessary after rotation)

### Dismissing Alerts

Dismiss with a documented reason:
- **False positive** — detected string is not a real secret
- **Revoked** — credential has already been revoked
- **Used in tests** — secret is only in test code

> For detailed alert types, validity checks, and REST API, search `references/alerts-and-remediation.md`.

## Reference Files

For detailed documentation, load the following reference files as needed:

- `references/push-protection.md` — Push protection mechanics, bypass workflow, delegated bypass, user push protection
  - Search patterns: `bypass`, `delegated`, `bypass request`, `command line`, `REST API`, `user push protection`
- `references/custom-patterns.md` — Custom pattern creation, regex syntax, dry runs, Copilot regex generation, scopes
  - Search patterns: `custom pattern`, `regex`, `dry run`, `publish`, `organization`, `enterprise`, `Copilot`
- `references/alerts-and-remediation.md` — Alert types, validity checks, extended metadata, generic alerts, secret removal, REST API
  - Search patterns: `user alert`, `partner alert`, `validity`, `metadata`, `generic`, `remediation`, `git history`, `REST API`

---

## Reference: alerts-and-remediation

# Alerts and Remediation Reference

Detailed reference for secret scanning alert types, validity checks, remediation workflows, and API access.

## Alert Types

### User Alerts

Generated when secret scanning detects a supported secret in the repository.

- Displayed in the repository **Security** tab
- Created for provider patterns, non-provider patterns, custom patterns, and AI-detected secrets
- Scanning covers entire Git history on all branches

### Push Protection Alerts

Generated when a contributor bypasses push protection to push a secret.

- Displayed in the Security tab (filter: `bypassed: true`)
- Record the bypass reason chosen by the contributor
- Include the commit and file where the secret was pushed

**Bypass reasons and their alert behavior:**

| Bypass Reason | Alert Status |
|---|---|
| It's used in tests | Closed (resolved as "used in tests") |
| It's a false positive | Closed (resolved as "false positive") |
| I'll fix it later | Open |

### Partner Alerts

Generated when GitHub detects a leaked secret matching a partner's pattern.

- Sent directly to the service provider (e.g., AWS, Stripe, GitHub)
- **Not** displayed in the repository Security tab
- Provider may automatically revoke the credential
- No action required by the repository owner

## Alert Lists

### Default Alerts List

The primary view showing alerts for:
- Supported provider patterns (e.g., GitHub PATs, AWS keys, Stripe keys)
- Custom patterns defined at repo/org/enterprise level

### Generic Alerts List

Separate view (toggle from default list) showing:
- Non-provider patterns (private keys, connection strings)
- AI-detected generic secrets (passwords)

**Limitations:**
- Maximum 5,000 alerts per repository (open + closed)
- Only first 5 detected locations shown for non-provider patterns
- Only first detected location shown for AI-detected secrets
- Not shown in security overview summary views

## Paired Credentials

When a resource requires paired credentials (e.g., access key + secret key):
- Alert is only created when BOTH parts are detected in the same file
- Prevents noise from partial leaks
- Reduces false positives

## Validity Checks

Validity checks verify whether a detected secret is still active.

### How It Works

1. Enable validity checks in repository/organization settings
2. GitHub periodically sends the secret to the issuer's API
3. Validation result is displayed on the alert

### Validation Statuses

| Status | Meaning | Priority |
|---|---|---|
| `Active` | Secret is confirmed to be valid and exploitable | 🔴 Immediate |
| `Inactive` | Secret has been revoked or expired | 🟡 Lower priority |
| `Unknown` | GitHub cannot determine validity | 🟠 Investigate |

### On-Demand Validation

Click the validation button on an individual alert to trigger an immediate check.

### Privacy

GitHub makes minimal API calls (typically GET requests) to the least intrusive endpoints, selecting endpoints that don't return personal information.

## Extended Metadata Checks

Provides additional context about detected secrets when validity checks are enabled.

### Available Metadata

Depends on what the service provider shares:
- Secret owner information
- Scope and permissions of the secret
- Creation date and expiration
- Associated account or project

### Benefits

- **Deeper insight** — know who owns a secret
- **Prioritize remediation** — understand scope and impact
- **Improve incident response** — quickly identify responsible teams
- **Enhance compliance** — ensure secrets align with governance policies
- **Reduce false positives** — additional context helps determine if action is needed

### Enabling

- Requires validity checks to be enabled first
- Can be enabled at repository, organization, or enterprise level
- Available via security configurations for bulk enablement

## Remediation Workflow

### Priority: Rotate the Credential

**Always rotate (revoke and reissue) the exposed credential first.** This is more important than removing the secret from Git history.

### Step-by-Step Remediation

1. **Receive alert** — via Security tab, email notification, or webhook
2. **Assess severity** — check validity status (active = urgent)
3. **Rotate the credential** — revoke the old credential and generate a new one
4. **Update references** — update all code/config that used the old credential
5. **Investigate impact** — check logs for unauthorized use during the exposure window
6. **Close the alert** — mark as resolved with appropriate reason
7. **Optionally clean Git history** — remove from commit history (time-intensive)

### Removing Secrets from Git History

If needed, use `git filter-repo` (recommended) or `BFG Repo-Cleaner`:

```bash
# Install git-filter-repo
pip install git-filter-repo

# Remove a specific file from all history
git filter-repo --path secrets.env --invert-paths

# Force push the cleaned history
git push --force --all
```

> **Note:** Rewriting history is disruptive — it invalidates existing clones and PRs. Only do this when absolutely necessary and after rotating the credential.

### Dismissing Alerts

Choose the appropriate reason:

| Reason | When to Use |
|---|---|
| **False positive** | Detected string is not a real secret |
| **Revoked** | Credential has already been revoked/rotated |
| **Used in tests** | Secret is only in test code with acceptable risk |

Add a dismissal comment for audit trail.

## Alert Notifications

Alerts generate notifications via:
- **Email** — to repository admins, organization owners, security managers
- **Webhooks** — `secret_scanning_alert` event
- **GitHub Actions** — `secret_scanning_alert` event trigger
- **Security overview** — aggregated view at organization level

## REST API

### List Alerts

```
GET /repos/{owner}/{repo}/secret-scanning/alerts
```

Query parameters: `state` (open/resolved), `secret_type`, `resolution`, `sort`, `direction`

### Get Alert Details

```
GET /repos/{owner}/{repo}/secret-scanning/alerts/{alert_number}
```

Returns: secret type, secret value (if permitted), locations, validity, resolution status, `dismissed_comment`

### Update Alert

```
PATCH /repos/{owner}/{repo}/secret-scanning/alerts/{alert_number}
```

Body: `state` (open/resolved), `resolution` (false_positive/revoked/used_in_tests/wont_fix), `resolution_comment`

### List Alert Locations

```
GET /repos/{owner}/{repo}/secret-scanning/alerts/{alert_number}/locations
```

Returns: file path, line numbers, commit SHA, blob SHA

### Organization-Level Endpoints

```
GET /orgs/{org}/secret-scanning/alerts
```

Lists alerts across all repositories in the organization.

## Webhook Events

### `secret_scanning_alert`

Triggered when a secret scanning alert is:
- Created
- Resolved
- Reopened
- Validated (validity status changes)

Payload includes: alert number, secret type, resolution, commit SHA, and location details.

## Exclusion Configuration

### `secret_scanning.yml`

Place at `.github/secret_scanning.yml` to auto-close alerts for specific paths:

```yaml
paths-ignore:
  - "docs/**"              # Documentation with example secrets
  - "test/fixtures/**"     # Test fixture data
  - "**/*.example"         # Example configuration files
  - "samples/credentials"  # Sample credential files
```

**Limits:**
- Maximum 1,000 entries
- File must be under 1 MB
- Excluded paths are also excluded from push protection

**Alerts for excluded paths are closed as "ignored by configuration."**

---

## Reference: custom-patterns

# Custom Patterns Reference

Detailed reference for defining custom secret scanning patterns using regular expressions at the repository, organization, and enterprise level.

## Overview

Custom patterns extend secret scanning to detect organization-specific secrets not covered by default patterns. They are defined as regular expressions and can optionally enforce push protection.

## Pattern Definition

### Required Fields

| Field | Description |
|---|---|
| **Pattern name** | Human-readable name for the pattern |
| **Secret format** | Regular expression matching the secret |

### Optional Fields (via "More options")

| Field | Description |
|---|---|
| **Before secret** | Regex for content that must appear before the secret |
| **After secret** | Regex for content that must appear after the secret |
| **Additional match requirements** | Extra constraints on the match |
| **Sample test string** | Example string to validate the regex |

### Regex Syntax

Custom patterns use standard regular expressions. Common patterns:

```
# API key with prefix
MYAPP_[A-Za-z0-9]{32}

# Connection string
Server=[\w.]+;Database=\w+;User Id=\w+;Password=[^;]+

# Internal token format
myorg-token-[a-f0-9]{64}

# JWT-like pattern
eyJ[A-Za-z0-9_-]+\.eyJ[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+
```

Use filter patterns similar to GitHub Actions workflow syntax for glob-style matching in before/after fields.

## Defining Patterns by Scope

### Repository Level

1. Repository Settings → Advanced Security
2. Under "Secret Protection" → Custom patterns → **New pattern**
3. Enter pattern name, regex, and optional fields
4. **Save and dry run** to test
5. Review results (up to 1,000 matches)
6. **Publish pattern** when satisfied
7. Optionally enable push protection

**Prerequisite:** Secret Protection must be enabled on the repository.

### Organization Level

1. Organization Settings → Advanced Security → Global settings
2. Under "Custom patterns" → **New pattern**
3. Enter pattern details
4. **Save and dry run** — select repositories for testing:
   - All repositories in the organization, or
   - Up to 10 selected repositories
5. **Publish pattern** when satisfied
6. Optionally enable push protection

**Notes:**
- Push protection for org-level custom patterns only applies to repos with push protection enabled
- Organization owners and repo admins receive alerts

### Enterprise Level

1. Enterprise settings → Policies → Advanced Security → Security features
2. Under "Secret scanning custom patterns" → **New pattern**
3. Enter pattern details
4. **Save and dry run** — select up to 10 repositories
5. **Publish pattern** when satisfied
6. Optionally enable push protection

**Notes:**
- Only the pattern creator can edit or dry-run enterprise-level patterns
- Dry runs require admin access to the selected repositories
- Push protection requires enterprise-level secret scanning push protection to be enabled

## Dry Run Process

Dry runs test patterns against repository content without creating alerts.

1. Click **Save and dry run** after defining the pattern
2. Select target repositories (org/enterprise level)
3. Click **Run**
4. Review up to 1,000 sample results
5. Identify false positives
6. Edit pattern and re-run if needed
7. **Publish pattern** only when false positive rate is acceptable

> Dry runs are essential — always test before publishing to avoid alert noise.

## Managing Published Patterns

### Editing Patterns

After publishing, patterns can be edited:
1. Navigate to the custom pattern
2. Modify the regex or optional fields
3. Save and dry run to validate changes
4. Publish the updated pattern

### Enabling Push Protection

Push protection can only be enabled after a pattern is published:
1. Navigate to the published pattern
2. Click **Enable** next to push protection

**Caution:** Enabling push protection for commonly found patterns can disrupt contributor workflows.

### Disabling or Deleting Patterns

- Disable: stops new alert generation but retains existing alerts
- Delete: removes the pattern and stops all scanning for it

## Copilot-Assisted Pattern Generation

Use Copilot secret scanning to generate regex automatically:

1. Navigate to custom pattern creation
2. Select "Generate with Copilot" (if available)
3. Provide a text description of the secret type (e.g., "internal API key starting with MYORG_ followed by 40 hex characters")
4. Optionally provide example strings that should match
5. Copilot generates a regex pattern
6. Review and refine the generated regex
7. Test with dry run before publishing

## Pattern Inheritance

| Scope | Applies To |
|---|---|
| Repository | That repository only |
| Organization | All repos in the org with secret scanning enabled |
| Enterprise | All repos across all orgs with secret scanning enabled |

Organization and enterprise patterns automatically apply to new repositories when secret scanning is enabled.

## Best Practices

1. **Always dry run** before publishing — review for false positives
2. **Start specific** — narrow regexes reduce false positives
3. **Use before/after context** — adds precision without overly complex regex
4. **Test with real examples** — include sample strings that should and shouldn't match
5. **Document patterns** — name patterns clearly so teams understand what they detect
6. **Review periodically** — remove or update patterns that no longer apply
7. **Be cautious with push protection** — enable only for patterns with low false positive rates
8. **Consider Copilot** — let AI generate the initial regex, then refine manually

---

## Reference: push-protection

# Push Protection Reference

Detailed reference for GitHub push protection — preventing secrets from reaching repositories, bypass workflows, and delegated bypass configuration.

## How Push Protection Works

Push protection scans for secrets during the push process and blocks pushes containing detected secrets. It operates as a preventative control, unlike standard secret scanning which detects secrets after commit.

### What Gets Scanned

| Surface | Scanned |
|---|---|
| Command line pushes | ✅ |
| GitHub UI commits | ✅ |
| File uploads to repo | ✅ |
| REST API content creation requests | ✅ |

### Types of Push Protection

**Repository push protection:**
- Requires GitHub Secret Protection enabled
- Disabled by default; enabled by repo admin, org owner, or security manager
- Generates alerts for bypasses in the Security tab
- Can be enabled at repository, organization, or enterprise level

**User push protection:**
- Enabled by default for all GitHub.com accounts
- Blocks pushes to public repositories containing supported secrets
- Does NOT generate alerts when bypassed (unless repo also has push protection enabled)
- Managed via personal account settings

## Resolving Blocked Pushes — Command Line

When push protection blocks a push, the error message includes:
- The secret type detected
- Commit SHAs containing the secret
- File paths and line numbers
- A URL to bypass (if permitted)

### Remove Secret from Latest Commit

```bash
# Edit the file to remove the secret
# Amend the commit
git commit --amend --all

# Push again
git push
```

### Remove Secret from Earlier Commits

```bash
# 1. Review the push error for all commits containing the secret
# 2. Find the earliest commit with the secret
git log

# 3. Interactive rebase before that commit
git rebase -i <EARLIEST-COMMIT>~1

# 4. Change 'pick' to 'edit' for the offending commit(s)
# 5. Remove the secret from the file
# 6. Stage and amend
git add .
git commit --amend

# 7. Continue rebase
git rebase --continue

# 8. Push
git push
```

### Bypass Push Protection

1. Visit the URL from the error message (must be the same user who pushed)
2. Select a reason:
   - **It's used in tests** → creates a closed alert (resolved as "used in tests")
   - **It's a false positive** → creates a closed alert (resolved as "false positive")
   - **I'll fix it later** → creates an open alert
3. Click **Allow me to push this secret**
4. Re-push within **3 hours** (after that, repeat the bypass process)

> A bypass reason is required when the repo has secret scanning enabled. For public repos with only user push protection (no repo push protection), no reason is needed and no alert is generated.

## Resolving Blocked Pushes — GitHub UI

When creating or editing a file in the GitHub UI:
1. A banner appears warning about the detected secret
2. Options to remove the secret or bypass are presented inline
3. Same bypass reasons apply as command line

## Resolving Blocked Pushes — REST API

Push protection also applies to REST API content creation endpoints. When blocked:
- The API returns an error response with details about the detected secret
- Include the bypass reason in the request to proceed

## Delegated Bypass

Delegated bypass gives organizations fine-grained control over who can bypass push protection.

### How It Works

1. Organization owners/repo admins create a **bypass list** of users, roles, or teams
2. Users on the bypass list can bypass push protection directly (with a reason)
3. All other contributors must **submit a bypass request** for review
4. Bypass requests appear in the Security tab → "Push protection bypass" page
5. Requests expire after **7 days** if not reviewed

### Who Can Always Bypass (Without Request)

- Organization owners
- Security managers
- Users in teams/roles added to the bypass list
- Users with custom role having "review and manage secret scanning bypass requests" permission

### Enabling Delegated Bypass

**Repository level:**
1. Settings → Advanced Security → Push protection
2. Enable "Restrict who can bypass push protection"
3. Add users, teams, or roles to the bypass list

**Organization level:**
1. Organization Settings → Advanced Security → Global settings
2. Configure delegated bypass in security configuration

### Managing Bypass Requests

Designated reviewers:
1. Navigate to repository Security tab → "Push protection bypass"
2. Review pending requests (includes the secret, commit, and contributor's comment)
3. **Approve** — contributor can push the secret and any future commits with the same secret
4. **Deny** — contributor must remove the secret before pushing

### Bypass Request Flow (Contributor Perspective)

1. Push is blocked; visit the URL from the error message
2. Add a comment explaining why the secret is safe to push
3. Click **Submit request**
4. Wait for email notification of approval/denial
5. If approved: push the commit
6. If denied: remove the secret and push again

## Push Protection Patterns

Push protection supports a subset of secret scanning patterns. Not all detected secret types trigger push protection blocks.

Key considerations:
- Older/legacy token formats may not be supported by push protection
- Some patterns have higher false positive rates and are excluded from push protection
- Custom patterns can have push protection enabled after publishing

For the full list of patterns supported by push protection, see [Supported secret scanning patterns](https://docs.github.com/en/code-security/secret-scanning/introduction/supported-secret-scanning-patterns).

## Configuring Push Protection for Custom Patterns

After publishing a custom pattern:
1. Navigate to the custom pattern in Settings → Advanced Security
2. Click **Enable** next to push protection
3. The pattern will now block pushes containing matching secrets

> Push protection for custom patterns only applies to repos that have push protection enabled. Enabling push protection for commonly found patterns can be disruptive to contributors.
