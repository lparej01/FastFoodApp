using System.Globalization;
using FastFoodApp.Models;
using FastFoodApp.Services;

namespace FastFoodApp.Views;

public class MenuCatalogPage : ContentPage
{
    private readonly DatabaseService _databaseService;
    private readonly ListView _listView;
    private readonly Picker _categoriaPicker;
    private readonly Entry _nombreEntry;
    private readonly Entry _precioEntry;
    private readonly Button _btnGuardar;
    private readonly Button _btnEliminar;
    private readonly Button _btnCancelar;

    private List<Categoria> _categorias = new();
    private PlatoMenu? _platoEnEdicion = null;

    public MenuCatalogPage(DatabaseService databaseService)
    {
        _databaseService = databaseService;
        Title = "Catálogo del Menú 🍽️";

        _nombreEntry = new Entry { Placeholder = "Nombre del plato (ej. Salchipapa)" };

        _categoriaPicker = new Picker
        {
            Title = "Selecciona una categoría",
            ItemDisplayBinding = new Binding("Nombre")
        };

        _precioEntry = new Entry
        {
            Placeholder = "Precio USD (ej. 8.50)",
            Keyboard = Keyboard.Numeric
        };

        _btnGuardar = new Button
        {
            Text = "Guardar Plato",
            BackgroundColor = Colors.ForestGreen,
            TextColor = Colors.White,
            FontAttributes = FontAttributes.Bold
        };

        _btnEliminar = new Button
        {
            Text = "Eliminar Plato",
            BackgroundColor = Colors.IndianRed,
            TextColor = Colors.White,
            FontAttributes = FontAttributes.Bold,
            IsVisible = false
        };
        _btnEliminar.Clicked += OnEliminarClicked;

        _btnCancelar = new Button
        {
            Text = "Cancelar Edición",
            BackgroundColor = Colors.SlateGray,
            TextColor = Colors.White,
            IsVisible = false
        };
        _btnCancelar.Clicked += (_, _) => LimpiarFormulario();

        _listView = new ListView();
        _listView.ItemTemplate = new DataTemplate(() =>
        {
            var textCell = new TextCell();
            textCell.SetBinding(TextCell.TextProperty, "Nombre");
            textCell.SetBinding(TextCell.DetailProperty, new Binding("PrecioUSD", stringFormat: "Precio: ${0:F2} • Toca para editar o eliminar"));
            return textCell;
        });

        // Tocar un elemento de la lista carga sus datos para modificarlo o eliminarlo
        _listView.ItemTapped += (_, e) =>
        {
            if (e.Item is not PlatoMenu plato)
                return;

            _platoEnEdicion = plato;
            _nombreEntry.Text = plato.Nombre;
            _precioEntry.Text = plato.PrecioUSD.ToString("0.##", CultureInfo.InvariantCulture);
            _categoriaPicker.SelectedItem = _categorias.FirstOrDefault(c => c.Id == plato.CategoriaId);

            _btnGuardar.Text = "Actualizar Plato";
            _btnGuardar.BackgroundColor = Colors.DarkOrange;
            _btnEliminar.IsVisible = true;
            _btnCancelar.IsVisible = true;

            _listView.SelectedItem = null;
        };

        _btnGuardar.Clicked += async (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(_nombreEntry.Text) || string.IsNullOrWhiteSpace(_precioEntry.Text))
            {
                await DisplayAlert("Error", "Por favor completa el nombre y el precio en USD.", "OK");
                return;
            }

            if (_categoriaPicker.SelectedItem is not Categoria categoriaSeleccionada)
            {
                await DisplayAlert("Error", "Por favor selecciona una categoría válida.", "OK");
                return;
            }

            string precioTexto = _precioEntry.Text.Trim().Replace(',', '.');
            if (!decimal.TryParse(precioTexto, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal precio) || precio <= 0)
            {
                await DisplayAlert("Error", "El precio ingresado no es válido.", "OK");
                return;
            }

            bool esEdicion = _platoEnEdicion != null;

            if (esEdicion && _platoEnEdicion != null)
            {
                _platoEnEdicion.Nombre = _nombreEntry.Text.Trim();
                _platoEnEdicion.CategoriaId = categoriaSeleccionada.Id;
                _platoEnEdicion.PrecioUSD = precio;

                await _databaseService.SavePlatoAsync(_platoEnEdicion);
                await DisplayAlert("Éxito", "Plato actualizado correctamente.", "OK");
            }
            else
            {
                var nuevoPlato = new PlatoMenu
                {
                    Nombre = _nombreEntry.Text.Trim(),
                    CategoriaId = categoriaSeleccionada.Id,
                    PrecioUSD = precio
                };

                await _databaseService.SavePlatoAsync(nuevoPlato);
                await DisplayAlert("Éxito", "Plato agregado correctamente.", "OK");
            }

            LimpiarFormulario();
            await CargarPlatos();
        };

        Content = new VerticalStackLayout
        {
            Padding = 20,
            Spacing = 15,
            Children =
            {
                new Label { Text = "Registrar o Modificar Plato", FontAttributes = FontAttributes.Bold, FontSize = 18 },
                _nombreEntry,
                _categoriaPicker,
                _precioEntry,
                _btnGuardar,
                _btnEliminar,
                _btnCancelar,
                new BoxView { HeightRequest = 1, BackgroundColor = Colors.LightGray },
                new Label { Text = "Lista de Platos Disponibles", FontAttributes = FontAttributes.Bold, FontSize = 18 },
                _listView
            }
        };
    }

    private async void OnEliminarClicked(object? sender, EventArgs e)
    {
        if (_platoEnEdicion == null)
            return;

        bool confirmar = await DisplayAlert(
            "Confirmar eliminación",
            $"¿Seguro que deseas eliminar '{_platoEnEdicion.Nombre}' del menú?",
            "Sí, eliminar",
            "Cancelar");

        if (!confirmar)
            return;

        await _databaseService.DeletePlatoAsync(_platoEnEdicion);

        LimpiarFormulario();
        await CargarPlatos();

        await DisplayAlert("Eliminado", "El plato fue eliminado del menú.", "OK");
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await CargarCategoriasAsync();
        await CargarPlatos();
    }

    private async Task CargarCategoriasAsync()
    {
        _categorias = await _databaseService.GetCategoriasAsync();
        _categoriaPicker.ItemsSource = _categorias;
    }

    private async Task CargarPlatos()
    {
        var platos = await _databaseService.GetPlatosAsync();
        _listView.ItemsSource = platos;
    }

    private void LimpiarFormulario()
    {
        _platoEnEdicion = null;
        _nombreEntry.Text = string.Empty;
        _categoriaPicker.SelectedItem = null;
        _precioEntry.Text = string.Empty;

        _btnGuardar.Text = "Guardar Plato";
        _btnGuardar.BackgroundColor = Colors.ForestGreen;
        _btnEliminar.IsVisible = false;
        _btnCancelar.IsVisible = false;
    }
}