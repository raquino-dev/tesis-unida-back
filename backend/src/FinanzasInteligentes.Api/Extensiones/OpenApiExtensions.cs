namespace FinanzasInteligentes.Api.Extensiones;

public static class OpenApiExtensions
{
    public static IApplicationBuilder UseOpenApiDocumentation(this IApplicationBuilder app)
    {
        app.UseOpenApi(settings =>
        {
            settings.Path = "/swagger/{documentName}/swagger.json";
        });
        app.UseSwaggerUi(settings =>
        {
            settings.Path = "/swagger";
            settings.DocumentPath = "/swagger/{documentName}/swagger.json";
        });

        return app;
    }
}