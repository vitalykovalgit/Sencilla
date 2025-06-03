
namespace Sencilla.Core;

/// <summary>
/// Base exception for all sencilla exceptions 
/// </summary>
public class SencillaException(string? message = null) : Exception(message);
