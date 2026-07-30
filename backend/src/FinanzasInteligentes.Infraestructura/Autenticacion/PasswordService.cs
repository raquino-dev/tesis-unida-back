using FinanzasInteligentes.Aplicacion.Abstracciones;
using Microsoft.AspNetCore.Identity;

namespace FinanzasInteligentes.Infraestructura.Autenticacion;

public sealed class PasswordService : IPasswordService
{
    private static readonly object Marker = new();
    private readonly PasswordHasher<object> _hasher = new();

    public string Hash(string password) => _hasher.HashPassword(Marker, password);

    public bool Verificar(string hash, string password) =>
        _hasher.VerifyHashedPassword(Marker, hash, password) != PasswordVerificationResult.Failed;
}