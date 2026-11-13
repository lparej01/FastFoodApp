using FastFoodApp.Models;
using FastFoodApp.Services;
using System.Globalization;


public class PurchaseView : ContentPage
{
    private DatabaseService _databaseService;
    private Picker _pickerIngredients;
    private Entry _txtQuantity;
    private Entry _txtUnitCost;
    private Label _lblTotalCost;
    private VerticalStackLayout _historyLayout;

    // Lista para mantener referencia de los ingredientes en el Picker
    private List<InventoryItem> _ingredientsList;

    public PurchaseView(DatabaseService databaseService)
    {
        _databaseService = databaseService;

        Title = "Registrar Compra";
        BackgroundColor = Colors.White;

        var mainStack = new VerticalStackLayout
        {
            Spacing = 20,
            Padding = 20,
            Children =
        {
            CreateHeaderSection(),
            CreateFormSection(),
            CreateHistorySection()
        }
        };

        Content = new ScrollView
        {
            Content = mainStack
        };

        // 🚀 Forzamos la carga de datos de inmediato al instanciar la vista
        _ = LoadDataAsync();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadDataAsync();
    }

    private View CreateHeaderSection()
    {
        return new Label
        {
            Text = "Gestión de Compras y Stock",
            FontSize = 22,
            FontAttributes = FontAttributes.Bold,
            TextColor = Colors.DarkSlateGray
        };
    }

    private View CreateFormSection()
    {
        // Picker de ingredientes
        _pickerIngredients = new Picker
        {
            Title = "Selecciona un ingrediente",
            TextColor = Colors.Black
        };

        // Entry para la cantidad comprada
        _txtQuantity = new Entry
        {
            Placeholder = "Cantidad comprada (ej. 5000)",
            Keyboard = Keyboard.Numeric
        };
        _txtQuantity.TextChanged += CalculateTotal;

        // Entry para el costo unitario
        _txtUnitCost = new Entry
        {
            Placeholder = "Costo unitario en USD (ej. 0.05)",
            Keyboard = Keyboard.Numeric
        };
        _txtUnitCost.TextChanged += CalculateTotal;

        // Etiqueta para mostrar el costo total en tiempo real
        _lblTotalCost = new Label
        {
            Text = "Costo Total: $0.00",
            FontSize = 16,
            FontAttributes = FontAttributes.Bold,
            TextColor = Colors.DarkGreen
        };

        // Botón principal de registro
        var btnRegister = new Button
        {
            Text = "Registrar Compra y Actualizar Stock",
            BackgroundColor = Colors.DarkBlue,
            TextColor = Colors.White,
            CornerRadius = 8
        };
        btnRegister.Clicked += OnRegisterClicked;

        return new VerticalStackLayout
        {
            Spacing = 15,
            Children =
            {
                new Label { Text = "Nuevo Registro de Entrada", FontAttributes = FontAttributes.Bold, TextColor = Colors.Gray },
                _pickerIngredients,
                _txtQuantity,
                _txtUnitCost,
                _lblTotalCost,
                btnRegister
            }
        };
    }

    private View CreateHistorySection()
    {
        _historyLayout = new VerticalStackLayout
        {
            Spacing = 10
        };

        BindableLayout.SetItemsSource(_historyLayout, new List<PurchaseItem>());
        BindableLayout.SetItemTemplate(_historyLayout, new DataTemplate(() =>
        {
            // Creamos las etiquetas
            var lblName = new Label
            {
                FontAttributes = FontAttributes.Bold,
                TextColor = Colors.Black
            };
            lblName.SetBinding(Label.TextProperty, "IngredientName");

            var lblQuantity = new Label
            {
                FontSize = 12,
                TextColor = Colors.DimGray
            };
            // Usamos un stringFormat para mostrar la cantidad claramente
            lblQuantity.SetBinding(Label.TextProperty, new Binding("QuantityPurchased", stringFormat: "Cantidad: {0}"));

            var lblTotal = new Label
            {
                FontSize = 12,
                TextColor = Colors.DarkGreen,
                FontAttributes = FontAttributes.Bold
            };
            lblTotal.SetBinding(Label.TextProperty, new Binding("TotalCost", stringFormat: "Total: ${0:F2}"));

            // Estructura visual de la tarjeta
            var border = new Border
            {
                Stroke = Colors.LightGray,
                StrokeThickness = 1,
                BackgroundColor = Color.FromArgb("#F9F9F9"),
                Padding = 12,
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 8 },
                Content = new VerticalStackLayout
                {
                    Spacing = 4,
                    Children = { lblName, lblQuantity, lblTotal }
                }
            };

            return border;
        }));

        return new VerticalStackLayout
        {
            Spacing = 10,
            Children =
            {
                new Label { Text = "Compras Recientes", FontSize = 18, FontAttributes = FontAttributes.Bold, TextColor = Colors.DarkSlateGray },
                _historyLayout
            }
        };
    }

    private void CalculateTotal(object sender, TextChangedEventArgs e)
    {
        if (decimal.TryParse(_txtQuantity.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal qty) &&
            decimal.TryParse(_txtUnitCost.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal cost))
        {
            decimal total = qty * cost;
            _lblTotalCost.Text = $"Costo Total: ${total.ToString("F2", CultureInfo.InvariantCulture)}";
        }
        else
        {
            _lblTotalCost.Text = "Costo Total: $0.00";
        }
    }

    private async Task LoadDataAsync()
    {
        // 1. Obtenemos los ingredientes desde SQLite
        _ingredientsList = await _databaseService.GetInventoryItemsAsync();

        // 2. Aseguramos que la actualización de la UI ocurra en el hilo principal
        MainThread.BeginInvokeOnMainThread(() =>
        {
            _pickerIngredients.ItemsSource = null; // Limpiamos para forzar el refresco visual
            _pickerIngredients.ItemsSource = _ingredientsList;

            // ⚠️ REVISA AQUÍ: Si en tu modelo InventoryItem la propiedad se llama "Nombre" (en español), 
            // cámbiala en lugar de "Name".
            _pickerIngredients.ItemDisplayBinding = new Binding("Nombre");
        });

        // 3. Cargamos el historial de compras recientes
        var purchases = await _databaseService.GetPurchasesAsync();
        MainThread.BeginInvokeOnMainThread(() =>
        {
            BindableLayout.SetItemsSource(_historyLayout, purchases);
        });
    }
    private async void OnRegisterClicked(object sender, EventArgs e)
    {
        if (_pickerIngredients.SelectedIndex == -1)
        {
            await DisplayAlert("Error", "Por favor selecciona un ingrediente.", "OK");
            return;
        }

        if (!decimal.TryParse(_txtQuantity.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal quantity) ||
            !decimal.TryParse(_txtUnitCost.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal unitCost))
        {
            await DisplayAlert("Error", "Verifica que la cantidad y el costo sean números válidos (usa punto para decimales).", "OK");
            return;
        }

        var selectedIngredient = _ingredientsList[_pickerIngredients.SelectedIndex];

        // Llamamos al método que creamos antes en DatabaseService
        await _databaseService.RegisterPurchaseAsync(selectedIngredient.Id, quantity, unitCost);

        await DisplayAlert("Éxito", "Compra registrada y stock actualizado correctamente.", "OK");

        // Limpiar campos y recargar datos
        _pickerIngredients.SelectedIndex = -1;
        _txtQuantity.Text = string.Empty;
        _txtUnitCost.Text = string.Empty;

        await LoadDataAsync();
    }
}
