using FastFoodApp.Services;
using FastFoodApp.Views;
using Microsoft.Extensions.DependencyInjection;

namespace FastFoodApp
{
    public partial class App : Application
    {
        public App(DatabaseService databaseService)
        {
            InitializeComponent();
            // Indicamos que la pantalla de inicio sea el Login pasándole el servicio
            // Cargamos la pantalla de inicio de sesión conectada a la base de datos
            MainPage = new NavigationPage(new LoginPage(databaseService));
        
        }

    
    }
}