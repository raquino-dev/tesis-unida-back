namespace FinanzasInteligentes.Aplicacion.Abstracciones;

public interface IPasswordService
{
    string Hash(string password);

    bool Verificar(string hash, string password);
}