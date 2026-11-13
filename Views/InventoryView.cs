using FastFoodApp.Models;
using FastFoodApp.Services;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Input;

namespace FastFoodApp.Views;

public class InventoryView : ContentPage
{
    private readonly DatabaseService _databaseService;
    private VerticalStackLayout _listaInventarioLayout;
    private ObservableCollection<InventoryItem> _inventoryItems;

    private Entry _nombreEntry;
    private Entry _stockEntry;
    private Picker _unidadPicker;
    private Entry _minimoEntry;
    private Button _btnGuardar;

    private int? _idItemEnEdicion = null;

    public ICommand EditCommand { get; }
    public ICommand DeleteCommand { get; }

    public InventoryView(DatabaseService databaseService)
    {
        _databaseService = databaseService;
        Title = "Control de Inventario 📦";

        _inventoryItems = new ObservableCollection<InventoryItem>();

        EditCommand = new Command<InventoryItem>(OnEditItem);
        DeleteCommand = new Command<InventoryItem>(OnDeleteItem);

        // Inicialización de campos de entrada
        _nombreEntry = new Entry { Placeholder = "Nombre del ingrediente (ej. Pollo, Arroz)" };
        _stockEntry = new Entry { Placeholder = "Cantidad actual (ej. 15.5)", Keyboard = Keyboard.Numeric };

        _unidadPicker = new Picker
        {
            Title = "Selecciona la unidad de medida",
            ItemsSource = new List<string>
            {
                "kilos",
                "gramos",
                "litros",
                "mililitros",
                "unidades"
            }
        };

        _minimoEntry = new Entry { Placeholder = "Stock mínimo de alerta (ej. 5)", Keyboard = Keyboard.Numeric };

        _btnGuardar = new Button
        {
            Text = "Guardar en Inventario",
            BackgroundColor = Colors.ForestGreen,
            TextColor = Colors.White,
            FontAttributes = FontAttributes.Bold
        };

        // Layout para la lista de inventario adaptado a ScrollView
        _listaInventarioLayout = new VerticalStackLayout
        {
            Spacing = 8
        };

        BindableLayout.SetItemsSource(_listaInventarioLayout, _inventoryItems);
        BindableLayout.SetItemTemplate(_listaInventarioLayout, new DataTemplate(() =>
        {
            var mainGrid = new Grid
            {
                Padding = new Thickness(10),
                BackgroundColor = Colors.WhiteSmoke,
                ColumnDefinitions = new ColumnDefinitionCollection
                {
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = GridLength.Auto },
                    new ColumnDefinition { Width = GridLength.Auto }
                },
                ColumnSpacing = 8
            };

            // Columna 0: Información del producto
            var stackIzq = new VerticalStackLayout { Spacing = 2, VerticalOptions = LayoutOptions.Center };

            var lblNombre = new Label { FontAttributes = FontAttributes.Bold, FontSize = 16 };
            lblNombre.SetBinding(Label.TextProperty, "Nombre");

            var lblMinimo = new Label { FontSize = 12, TextColor = Colors.Gray };
            var multiBindingMin = new MultiBinding { StringFormat = "Mínimo: {0} {1}" };
            multiBindingMin.Bindings.Add(new Binding("StockMinimo"));
            multiBindingMin.Bindings.Add(new Binding("UnidadMedida"));
            lblMinimo.SetBinding(Label.TextProperty, multiBindingMin);

            var lblStockYUnidad = new Label
            {
                FontAttributes = FontAttributes.Bold,
                FontSize = 13,
                TextColor = Colors.DarkSlateGray
            };
            var multiBinding = new MultiBinding { StringFormat = "Stock Actual: {0} {1}" };
            multiBinding.Bindings.Add(new Binding("StockActual"));
            multiBinding.Bindings.Add(new Binding("UnidadMedida"));
            lblStockYUnidad.SetBinding(Label.TextProperty, multiBinding);

            stackIzq.Children.Add(lblNombre);
            stackIzq.Children.Add(lblMinimo);
            stackIzq.Children.Add(lblStockYUnidad);

            // Columna 1: Botón Editar
            var btnEditar = new Button
            {
                Text = "✏️",
                BackgroundColor = Colors.Orange,
                TextColor = Colors.White,
                FontSize = 14,
                WidthRequest = 45,
                HeightRequest = 40,
                VerticalOptions = LayoutOptions.Center
            };
            btnEditar.Command = EditCommand;
            btnEditar.SetBinding(Button.CommandParameterProperty, new Binding("."));

            // Columna 2: Botón Eliminar
            var btnEliminar = new Button
            {
                Text = "🗑️",
                BackgroundColor = Colors.IndianRed,
                TextColor = Colors.White,
                FontSize = 14,
                WidthRequest = 45,
                HeightRequest = 40,
                VerticalOptions = LayoutOptions.Center
            };
            btnEliminar.Command = DeleteCommand;
            btnEliminar.SetBinding(Button.CommandParameterProperty, new Binding("."));

            mainGrid.Add(stackIzq, 0, 0);
            mainGrid.Add(btnEditar, 1, 0);
            mainGrid.Add(btnEliminar, 2, 0);

            return mainGrid;
        }));

        // Lógica del botón Guardar / Actualizar
        _btnGuardar.Clicked += async (s, e) =>
        {
            if (string.IsNullOrWhiteSpace(_nombreEntry.Text) ||
                string.IsNullOrWhiteSpace(_stockEntry.Text) ||
                _unidadPicker.SelectedItem == null)
            {
                await DisplayAlert("Error", "Por favor completa el nombre, la cantidad y selecciona una unidad.", "OK");
                return;
            }

            string stockTexto = _stockEntry.Text.Replace(',', '.');
            string minimoTexto = string.IsNullOrWhiteSpace(_minimoEntry.Text) ? "0" : _minimoEntry.Text.Replace(',', '.');

            if (float.TryParse(stockTexto, NumberStyles.Any, CultureInfo.InvariantCulture, out float stock) &&
                float.TryParse(minimoTexto, NumberStyles.Any, CultureInfo.InvariantCulture, out float minStock))
            {
                var item = new InventoryItem
                {
                    Id = _idItemEnEdicion ?? 0,
                    Nombre = _nombreEntry.Text.Trim(),
                    StockActual = stock,
                    UnidadMedida = _unidadPicker.SelectedItem.ToString(),
                    StockMinimo = minStock
                };

                await _databaseService.SaveInventoryItemAsync(item);

                string mensaje = _idItemEnEdicion.HasValue ? "Elemento actualizado correctamente." : "Elemento de inventario registrado correctamente.";

                LimpiarFormulario();
                await CargarInventario();

                await DisplayAlert("Éxito", mensaje, "OK");
            }
            else
            {
                await DisplayAlert("Error", "Los valores numéricos de stock no son válidos.", "OK");
            }
        };

        // Diseño general de la página envuelto en ScrollView
        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = 20,
                Spacing = 12,
                Children =
                {
                    new Label { Text = "Registrar Elemento de Inventario", FontAttributes = FontAttributes.Bold, FontSize = 18 },
                    _nombreEntry,
                    _stockEntry,
                    _unidadPicker,
                    _minimoEntry,
                    _btnGuardar,
                    new BoxView { HeightRequest = 1, BackgroundColor = Colors.LightGray, Margin = new Thickness(0, 10) },
                    new Label { Text = "Stock Actual en Almacén", FontAttributes = FontAttributes.Bold, FontSize = 18 },
                    _listaInventarioLayout
                }
            }
        };
    }

    private void OnEditItem(InventoryItem item)
    {
        if (item == null) return;

        _idItemEnEdicion = item.Id;
        _nombreEntry.Text = item.Nombre;
        _stockEntry.Text = item.StockActual.ToString(CultureInfo.InvariantCulture);
        _unidadPicker.SelectedItem = item.UnidadMedida;
        _minimoEntry.Text = item.StockMinimo.ToString(CultureInfo.InvariantCulture);

        _btnGuardar.Text = "Actualizar Elemento";
        _btnGuardar.BackgroundColor = Colors.DarkOrange;
    }

    private async void OnDeleteItem(InventoryItem item)
    {
        if (item == null) return;

        bool confirmar = await DisplayAlert("Eliminar", $"¿Deseas eliminar '{item.Nombre}'?", "Sí", "No");
        if (confirmar)
        {
            await _databaseService.DeleteInventoryItemAsync(item);

            if (_idItemEnEdicion == item.Id)
            {
                LimpiarFormulario();
            }

            await CargarInventario();
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await CargarInventario();
    }

    private async Task CargarInventario()
    {
        var items = await _databaseService.GetInventoryItemsAsync();

        _inventoryItems.Clear();
        foreach (var item in items)
        {
            _inventoryItems.Add(item);
        }
    }

    private void LimpiarFormulario()
    {
        _idItemEnEdicion = null;
        _nombreEntry.Text = string.Empty;
        _stockEntry.Text = string.Empty;
        _unidadPicker.SelectedItem = null;
        _minimoEntry.Text = string.Empty;

        _btnGuardar.Text = "Guardar en Inventario";
        _btnGuardar.BackgroundColor = Colors.ForestGreen;
    }
}