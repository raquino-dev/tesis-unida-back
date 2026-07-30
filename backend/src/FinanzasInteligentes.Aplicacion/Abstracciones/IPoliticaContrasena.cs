namespace FinanzasInteligentes.Aplicacion.Abstracciones;

public interface IPoliticaContrasena
{
    void Validar(string contrasena, string? correo = null, string? nombre = null);
}
