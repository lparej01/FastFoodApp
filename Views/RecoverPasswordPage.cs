using FastFoodApp.Services;

namespace FastFoodApp.Views;

public class RecoverPasswordPage : ContentPage
{
    private readonly DatabaseService _databaseService;
    private readonly Entry _correoEntry;
    private readonly Entry _nombreEntry;
    private readonly Entry _nuevaPasswordEntry;
    private readonly Entry _confirmarPasswordEntry;
    private readonly Button _guardarButton;

    public RecoverPasswordPage(DatabaseService databaseService)
    {
        _databaseService = databaseService;
        Title = "Recuperar Contraseña";

        _correoEntry = new Entry
        {
            Placeholder = "Correo registrado",
            Keyboard = Keyboard.Email,
            ClearButtonVisibility = ClearButtonVisibility.WhileEditing
        };

        _nombreEntry = new Entry
        {
            Placeholder = "Nombre registrado",
            ClearButtonVisibility = ClearButtonVisibility.WhileEditing
        };

        _nuevaPasswordEntry = new Entry
        {
            Placeholder = "Nueva contraseña (mín. 4 caracteres)",
            IsPassword = true,
            ClearButtonVisibility = ClearButtonVisibility.WhileEditing
        };

        _confirmarPasswordEntry = new Entry
        {
            Placeholder = "Confirmar nueva contraseña",
            IsPassword = true,
            ClearButtonVisibility = ClearButtonVisibility.WhileEditing
        };

        _guardarButton = new Button
        {
            Text = "Actualizar contraseña",
            BackgroundColor = Colors.DarkBlue,
            TextColor = Colors.White,
            CornerRadius = 8,
            HeightRequest = 48
        };
        _guardarButton.Clicked += OnGuardarClicked;

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
                        Text = "Restablecer contraseña",
                        FontSize = 24,
                        FontAttributes = FontAttributes.Bold
                    },
                    new Label { Text = "Correo" },
                    _correoEntry,
                    new Label { Text = "Nombre de verificación" },
                    _nombreEntry,
                    new Label { Text = "Nueva contraseña" },
                    _nuevaPasswordEntry,
                    new Label { Text = "Confirmar contraseña" },
                    _confirmarPasswordEntry,
                    _guardarButton,
                    volverButton
                }
            }
        };
    }

    private async void OnGuardarClicked(object? sender, EventArgs e)
    {
        string nuevaPassword = _nuevaPasswordEntry.Text ?? string.Empty;
        string confirmarPassword = _confirmarPasswordEntry.Text ?? string.Empty;

        if (nuevaPassword != confirmarPassword)
        {
            await DisplayAlert("Aviso", "La nueva contraseña y su confirmación no coinciden.", "Aceptar");
            return;
        }

        _guardarButton.IsEnabled = false;

        try
        {
            var (success, message) = await _databaseService.ResetPasswordAsync(
                _correoEntry.Text ?? string.Empty,
                _nombreEntry.Text ?? string.Empty,
                nuevaPassword
            );

            await DisplayAlert(success ? "Éxito" : "Aviso", message, "Aceptar");

            if (success)
            {
                await Navigation.PopAsync();
            }
        }
        finally
        {
            _guardarButton.IsEnabled = true;
        }
    }
}