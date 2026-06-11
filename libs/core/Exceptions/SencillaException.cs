
namespace Sencilla.Core;

/// <summary>
/// Base exception for all sencilla exceptions.
/// Passes "" instead of null to the base so Message stays empty (not the
/// "Exception of type ... was thrown." default) — SencillaExceptionHandler
/// relies on that to omit the ProblemDetails detail for messageless throws.
/// </summary>
public class SencillaException(string? message = null) : Exception(message ?? "");
