namespace FinanzasInteligentes.Aplicacion.Excepciones;

public abstract class ApplicationExceptionBase(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}

public sealed class ConflictException(string code, string message) : ApplicationExceptionBase(code, message);

public sealed class NotFoundException(string code, string message) : ApplicationExceptionBase(code, message);

public sealed class AuthenticationException(string code, string message) : ApplicationExceptionBase(code, message);

public sealed class ForbiddenException(string code, string message) : ApplicationExceptionBase(code, message);

public sealed class PreconditionFailedException(string code, string message) : ApplicationExceptionBase(code, message);