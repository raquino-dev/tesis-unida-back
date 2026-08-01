using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using FinanzasInteligentes.Aplicacion.Abstracciones;
using Microsoft.Extensions.Options;

namespace FinanzasInteligentes.Infraestructura.Archivos;

public sealed class S3ArchivoStorage : IArchivoStorage, IDisposable
{
    private readonly S3StorageOptions options;
    private readonly AmazonS3Client client;

    public S3ArchivoStorage(IOptions<S3StorageOptions> options)
    {
        this.options = options.Value;
        client = new AmazonS3Client(
            RegionEndpoint.GetBySystemName(this.options.Region));
    }

    public async Task Guardar(
        string clave, Stream contenido, CancellationToken ct)
    {
        await client.PutObjectAsync(new PutObjectRequest
        {
            BucketName = options.Bucket,
            Key = Key(clave),
            InputStream = contenido,
            AutoCloseStream = false,
            ServerSideEncryptionMethod =
                ServerSideEncryptionMethod.AES256
        }, ct);
    }

    public async Task<Stream> Abrir(string clave, CancellationToken ct)
    {
        var response = await client.GetObjectAsync(
            options.Bucket, Key(clave), ct);
        return response.ResponseStream;
    }

    public Task Eliminar(string clave, CancellationToken ct) =>
        client.DeleteObjectAsync(options.Bucket, Key(clave), ct);

    public string CrearUrlTemporal(
        string clave, DateTimeOffset expiraEn) =>
        client.GetPreSignedURL(new GetPreSignedUrlRequest
        {
            BucketName = options.Bucket,
            Key = Key(clave),
            Verb = HttpVerb.GET,
            Expires = expiraEn.UtcDateTime
        });

    public bool ValidarUrl(
        string clave, long expiraUnix, string firma) => false;

    private string Key(string clave)
    {
        var normalized = clave.TrimStart('/');
        var prefix = options.Prefijo.Trim('/');
        return string.IsNullOrEmpty(prefix)
            ? normalized
            : $"{prefix}/{normalized}";
    }

    public void Dispose() => client.Dispose();
}
