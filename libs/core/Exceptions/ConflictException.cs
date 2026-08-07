namespace Sencilla.Core;

/// <summary>
/// The request cannot be applied to the current state of the row — it was changed or removed since
/// the caller read it. Maps to 409. The caller's remedy is always the same: re-read and retry.
/// </summary>
public class ConflictException(string? message = null) : SencillaException(message);
