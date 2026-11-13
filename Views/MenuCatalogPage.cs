using FastFoodApp.Models;
using FastFoodApp.Services;

namespace FastFoodApp.Views;

public class MenuCatalogPage : ContentPage
{
    private readonly DatabaseService _databaseService;
    private ListView _listView;
    private Picker _categoriaPicker; // Cambiamos el Entry por un Picker

    public MenuCatalogPage(DatabaseService databaseService)
    {
        _databaseService = databaseService;
        Title = "Catálogo del Menú 🍽️";

        // Campos de entrada
        var nombreEntry = new Entry { Placeholder = "Nombre del plato (ej. Salchipapa)" };

        // Configuramos el Picker para seleccionar categorías de la base de datos
        _categoriaPicker = new Picker
        {
            Title = "Selecciona una categoría",
            ItemDisplayBinding = new Binding("Nombre") // Muestra la propiedad Nombre de la clase Categoria
        };

        var precioEntry = new Entry { Placeholder = "Precio USD (ej. 8.50)", Keyboard = Keyboard.Numeric };

        var btnGuardar = new Button
        {
            Text = "Guardar Plato",
            BackgroundColor = Colors.ForestGreen,
            TextColor = Colors.White,
            FontAttributes = FontAttributes.Bold
        };

        // Lista para mostrar los platos
        _listView = new ListView();
        _listView.ItemTemplate = new DataTemplate(() =>
        {
            var textCell = new TextCell();
            textCell.SetBinding(TextCell.TextProperty, "Nombre");
            textCell.SetBinding(TextCell.DetailProperty, new Binding("PrecioUSD", stringFormat: "Precio: ${0:F2}"));
            return textCell;
        });

        // Acción al hacer clic en Guardar
        btnGuardar.Clicked += async (s, e) =>
        {
            if (string.IsNullOrWhiteSpace(nombreEntry.Text) || string.IsNullOrWhiteSpace(precioEntry.Text))
            {
                await DisplayAlert("Error", "Por favor completa el nombre y el precio en USD.", "OK");
                return;
            }

            // Validar que se haya seleccionado una categoría del Picker
            if (_categoriaPicker.SelectedItem is not Categoria categoriaSeleccionada)
            {
                await DisplayAlert("Error", "Por favor selecciona una categoría válida.", "OK");
                return;
            }

            if (decimal.TryParse(precioEntry.Text, out decimal precio))
            {
                var nuevoPlato = new PlatoMenu
                {
                    Nombre = nombreEntry.Text.Trim(),
                    CategoriaId = categoriaSeleccionada.Id, // <-- Guardamos la relación por ID
                    PrecioUSD = precio
                };

                await _databaseService.SavePlatoAsync(nuevoPlato);

                await DisplayAlert("Éxito", "Plato agregado correctamente.", "OK");

                // Limpiar campos
                nombreEntry.Text = string.Empty;
                _categoriaPicker.SelectedItem = null;
                precioEntry.Text = string.Empty;

                // Recargar lista
                await CargarPlatos();
            }
            else
            {
                await DisplayAlert("Error", "El precio ingresado no es válido.", "OK");
            }
        };

        Content = new VerticalStackLayout
        {
            Padding = 20,
            Spacing = 15,
            Children =
            {
                new Label { Text = "Registrar Nuevo Plato", FontAttributes = FontAttributes.Bold, FontSize = 18 },
                nombreEntry,
                _categoriaPicker, // Reemplazamos categoriaEntry por el Picker
                precioEntry,
                btnGuardar,
                new BoxView { HeightRequest = 1, BackgroundColor = Colors.LightGray },
                new Label { Text = "Lista de Platos Disponibles", FontAttributes = FontAttributes.Bold, FontSize = 18 },
                _listView
            }
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await CargarCategoriasAsync(); // Cargamos las categorías del Picker primero
        await CargarPlatos();
    }

    private async Task CargarCategoriasAsync()
    {
        // Asegúrate de tener un método en tu DatabaseService que devuelva List<Categoria>
        var categorias = await _databaseService.GetCategoriasAsync();
        _categoriaPicker.ItemsSource = categorias;
    }

    private async Task CargarPlatos()
    {
        var platos = await _databaseService.GetPlatosAsync();
        _listView.ItemsSource = platos;
    }
}