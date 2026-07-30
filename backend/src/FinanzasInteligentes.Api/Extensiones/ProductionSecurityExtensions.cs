namespace FinanzasInteligentes.Api.Extensiones;

public static class ProductionSecurityExtensions
{
    public static WebApplication UseConfiguredForwardedHeaders(this WebApplication app)
    {
        if (app.Configuration.GetValue<bool>("ReverseProxy:Enabled"))
        {
            app.UseForwardedHeaders();
        }

        return app;
    }
}
