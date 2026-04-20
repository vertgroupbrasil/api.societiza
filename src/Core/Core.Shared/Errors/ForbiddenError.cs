namespace Core.Shared.Errors;

public sealed class ForbiddenError(Error error) : Error(error.Code, error.Message) { }
