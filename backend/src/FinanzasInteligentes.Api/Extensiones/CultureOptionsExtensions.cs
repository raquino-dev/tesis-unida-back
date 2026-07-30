namespace FinanzasInteligentes.Api.Extensiones
{
    internal static class CultureOptionsExtensions
    {
        public static void SetDefaultCulture(this WebApplicationBuilder _)
        {
            var defaultCulture = new System.Globalization.CultureInfo("es-ES");
            System.Globalization.CultureInfo.DefaultThreadCurrentCulture = defaultCulture;
            System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = defaultCulture;
        }
    }
}