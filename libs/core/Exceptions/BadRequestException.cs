namespace Sencilla.Core;

public class BadRequestException(string? message = null) : SencillaException(message);
