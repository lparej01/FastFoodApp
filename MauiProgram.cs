using Microsoft.Extensions.Logging;
using FastFoodApp.Services;

namespace FastFoodApp
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            try
            {
                var builder = MauiApp.CreateBuilder();
                builder
                    .UseMauiApp<App>()
                    .ConfigureFonts(fonts =>
                    {
                        fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                        fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                    });

                // Registramos nuestro servicio de base de datos
                builder.Services.AddSingleton<DatabaseService>();

#if DEBUG
                builder.Logging.AddDebug();
#endif

                return builder.Build();
            }
            catch (Exception ex)
            {
                // Esto imprimirá el error exacto en la consola de depuración de Visual Studio
                System.Diagnostics.Debug.WriteLine($"ERROR CRITICO AL INICIAR: {ex.Message} --- {ex.InnerException?.Message}");
                throw;
            }
        }
    }
}