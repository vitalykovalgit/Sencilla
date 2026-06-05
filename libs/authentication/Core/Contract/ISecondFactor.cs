namespace Sencilla.Authentication;

/// <summary>
/// Multi-factor seam. Defined now so flows and claims (<c>amr</c>/<c>acr</c>) are MFA-ready;
/// a TOTP implementation (then WebAuthn/passkeys) is added later without reshaping tokens.
/// </summary>
public interface ISecondFactor
{
    Task<bool> IsEnrolled(Guid userId, CancellationToken token = default);

    Task Challenge(Guid userId, CancellationToken token = default);

    Task<bool> Verify(Guid userId, string code, CancellationToken token = default);
}
