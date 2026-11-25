using FastFoodApp.Services;
using FastFoodApp.Views;

namespace FastFoodApp.Views;

public class MainPage : ContentPage
{
    private readonly DatabaseService _databaseService;

    public MainPage(DatabaseService databaseService)
    {
        _databaseService = databaseService;
        Title = "Menú Principal - FastFoodApp";

        // Botones de navegación para las funciones del sistema
        var btnCatalogo = new Button
        {
            Text = "🍽️ Catálogo del Menú",
            BackgroundColor = Colors.OrangeRed,
            TextColor = Colors.White,
            FontAttributes = FontAttributes.Bold,
            HeightRequest = 50
        };

        var btnInventario = new Button
        {
            Text = "📦 Control de Inventario",
            BackgroundColor = Colors.DarkSlateGray,
            TextColor = Colors.White,
            FontAttributes = FontAttributes.Bold,
            HeightRequest = 50
        };

        var btnCompras = new Button
        {
            Text = "🛒 Registrar Compras",
            BackgroundColor = Colors.DarkBlue,
            TextColor = Colors.White,
            FontAttributes = FontAttributes.Bold,
            HeightRequest = 50
        };
        var btnCerrarSesion = new Button
        {
            Text = "🚪 Cerrar sesión",
            BackgroundColor = Colors.Crimson,
            TextColor = Colors.White,
            FontAttributes = FontAttributes.Bold,
            HeightRequest = 50
        };

        btnCerrarSesion.Clicked += async (_, _) =>
        {
            bool confirmar = await DisplayAlert(
                "Cerrar sesión",
                "¿Deseas salir de tu sesión actual?",
                "Sí",
                "No"
            );

            if (!confirmar) return;

            Application.Current.MainPage = new NavigationPage(new LoginPage(_databaseService));
        };
        var btnPedidos = new Button
        {
            Text = "📝 Tomar Pedidos",
            BackgroundColor = Colors.ForestGreen,
            TextColor = Colors.White,
            FontAttributes = FontAttributes.Bold,
            HeightRequest = 50
        };

        // Catalogo)
        btnCatalogo.Clicked += async (s, e) =>
        {
            await Navigation.PushAsync(new MenuCatalogPage(_databaseService));
        };
        // <-- Iventario -->
        btnInventario.Clicked += async (s, e) =>
        {
            await Navigation.PushAsync(new InventoryView(_databaseService));
        };
        // <-- Compras -->
        btnCompras.Clicked += async (s, e) =>
        {
            await Navigation.PushAsync(new PurchaseView(_databaseService));
        };

        btnPedidos.Clicked += async (s, e) =>
        {
            await Navigation.PushAsync(new OrderPage(_databaseService));
        };


        Content = new VerticalStackLayout
        {
            Padding = 30,
            Spacing = 20,
            VerticalOptions = LayoutOptions.Center,
            Children =
            {
                new Label
                {
                    Text = "Panel de Control 🍔",
                    FontSize = 26,
                    FontAttributes = FontAttributes.Bold,
                    HorizontalOptions = LayoutOptions.Center
                },
                new Label
                {
                    Text = "Selecciona una opción para continuar:",
                    FontSize = 14,
                    TextColor = Colors.Gray,
                    HorizontalOptions = LayoutOptions.Center
                },
                new BoxView { HeightRequest = 10 },
                btnCatalogo,
                btnInventario,
                btnCompras,
                btnPedidos,
                btnCerrarSesion
            }
        };
       
    }
   
   
}
