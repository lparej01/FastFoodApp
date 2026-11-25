using FastFoodApp.Services;

namespace FastFoodApp.Views;

public class RegisterPage : ContentPage
{
    private readonly DatabaseService _databaseService;
    private readonly Entry _nombreEntry;
    private readonly Entry _correoEntry;
    private readonly Entry _passwordEntry;
    private readonly Entry _confirmarPasswordEntry;
    private readonly Button _registrarButton;

    public RegisterPage(DatabaseService databaseService)
    {
        _databaseService = databaseService;
        Title = "Registro de Usuario";

        _nombreEntry = new Entry
        {
            Placeholder = "Nombre completo",
            ClearButtonVisibility = ClearButtonVisibility.WhileEditing
        };

        _correoEntry = new Entry
        {
            Placeholder = "Correo electrónico",
            Keyboard = Keyboard.Email,
            ClearButtonVisibility = ClearButtonVisibility.WhileEditing
        };

        _passwordEntry = new Entry
        {
            Placeholder = "Contraseña (mín. 4 caracteres)",
            IsPassword = true,
            ClearButtonVisibility = ClearButtonVisibility.WhileEditing
        };

        _confirmarPasswordEntry = new Entry
        {
            Placeholder = "Confirmar contraseña",
            IsPassword = true,
            ClearButtonVisibility = ClearButtonVisibility.WhileEditing
        };

        _registrarButton = new Button
        {
            Text = "Registrar usuario",
            BackgroundColor = Colors.DarkGreen,
            TextColor = Colors.White,
            CornerRadius = 8,
            HeightRequest = 48
        };
        _registrarButton.Clicked += OnRegistrarClicked;

        var volverButton = new Button
        {
            Text = "Volver al inicio de sesión",
            BackgroundColor = Colors.Transparent,
            TextColor = Colors.Gray
        };
        volverButton.Clicked += async (_, _) => await Navigation.PopAsync();

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = 24,
                Spacing = 14,
                Children =
                {
                    new Label
                    {
                        Text = "Crear cuenta",
                        FontSize = 24,
                        FontAttributes = FontAttributes.Bold
                    },
                    new Label { Text = "Nombre" },
                    _nombreEntry,
                    new Label { Text = "Correo" },
                    _correoEntry,
                    new Label { Text = "Contraseña" },
                    _passwordEntry,
                    new Label { Text = "Confirmar contraseña" },
                    _confirmarPasswordEntry,
                    _registrarButton,
                    volverButton
                }
            }
        };
    }

    private async void OnRegistrarClicked(object? sender, EventArgs e)
    {
        string password = _passwordEntry.Text ?? string.Empty;
        string confirmarPassword = _confirmarPasswordEntry.Text ?? string.Empty;

        if (password != confirmarPassword)
        {
            await DisplayAlert("Aviso", "La contraseña y su confirmación no coinciden.", "Aceptar");
            return;
        }

        _registrarButton.IsEnabled = false;

        try
        {
            var (success, message) = await _databaseService.RegisterUserAsync(
                _nombreEntry.Text ?? string.Empty,
                _correoEntry.Text ?? string.Empty,
                password,
                rol: "Cajero"
            );

            await DisplayAlert(success ? "Éxito" : "Aviso", message, "Aceptar");

            if (success)
            {
                await Navigation.PopAsync();
            }
        }
        finally
        {
            _registrarButton.IsEnabled = true;
        }
    }
}