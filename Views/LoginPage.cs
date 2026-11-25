using FastFoodApp.Services;
using FastFoodApp.Models;

namespace FastFoodApp.Views;

public class LoginPage : ContentPage
{
    private readonly DatabaseService _databaseService;

    public LoginPage(DatabaseService databaseService)
    {
        _databaseService = databaseService;
        Title = "Iniciar Sesión - FastFoodApp";

        var emailEntry = new Entry
        {
            Placeholder = "Correo electronico",
            Keyboard = Keyboard.Email
        };

        var passwordEntry = new Entry
        {
            Placeholder = "Contraseña",
            IsPassword = true
        };

        var loginButton = new Button
        {
            Text = "Ingresar",
            BackgroundColor = Colors.DarkOrange,
            TextColor = Colors.White,
            FontAttributes = FontAttributes.Bold
        };

        loginButton.Clicked += async (sender, e) =>
        {
            string email = emailEntry.Text?.Trim() ?? string.Empty;
            string password = passwordEntry.Text?.Trim() ?? string.Empty;

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                await DisplayAlert("Advertencia", "Por favor, ingresa el correo y la contraseña.", "OK");
                return;
            }

            // Validar contra nuestra base de datos SQLite
            User usuarioEncontrado = await _databaseService.LoginAsync(email, password);

            if (usuarioEncontrado != null)
            {
                await DisplayAlert("Éxito", $"Bienvenido, {usuarioEncontrado.Nombre}", "OK");


                // Redirigimos al Menú Principal pasando el servicio de base de datos
                Application.Current.MainPage = new NavigationPage(new MainPage(_databaseService));
            }
            else
            {
                await DisplayAlert("Error", "Correo o contraseña incorrectos.", "OK");
            }
        };

        var registerButton = new Button
        {
            Text = "Crear cuenta",
            BackgroundColor = Colors.Transparent,
            TextColor = Colors.ForestGreen,
            FontAttributes = FontAttributes.Bold
        };
        registerButton.Clicked += async (_, _) =>
        {
            await Navigation.PushAsync(new RegisterPage(_databaseService));
        };

        var recoverButton = new Button
        {
            Text = "¿Olvidaste tu contraseña?",
            BackgroundColor = Colors.Transparent,
            TextColor = Colors.DarkBlue
        };
        recoverButton.Clicked += async (_, _) =>
        {
            await Navigation.PushAsync(new RecoverPasswordPage(_databaseService));
        };

        Content = new VerticalStackLayout
        {
            Padding = 40,
            Spacing = 20,
            VerticalOptions = LayoutOptions.Center,
            Children =
            {
                new Label
                {
                    Text = "FastFoodApp 🍔",
                    FontSize = 28,
                    FontAttributes = FontAttributes.Bold,
                    HorizontalOptions = LayoutOptions.Center
                },
                new Label
                {
                    Text = "Control de Ventas e Inventario",
                    FontSize = 14,
                    TextColor = Colors.Gray,
                    HorizontalOptions = LayoutOptions.Center
                },
               new BoxView { HeightRequest = 20 }, // Espaciador
                emailEntry,
                passwordEntry,
                loginButton,
                registerButton,
                recoverButton
            }
        };
    }
}
