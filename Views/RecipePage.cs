using System.Globalization;
using FastFoodApp.Models;
using FastFoodApp.Services;

namespace FastFoodApp.Views;

public class RecipePage : ContentPage
{
    private readonly DatabaseService _databaseService;
    private readonly Picker _platoPicker;
    private readonly Picker _ingredientePicker;
    private readonly Entry _cantidadEntry;
    private readonly Button _btnGuardar;
    private readonly Button _btnEliminar;
    private readonly Button _btnCancelar;
    private readonly ListView _listView;

    private List<PlatoMenu> _platos = new();
    private List<InventoryItem> _ingredientes = new();
    private Recipe? _recetaSeleccionada = null;

    public RecipePage(DatabaseService databaseService)
    {
        _databaseService = databaseService;
        Title = "Recetas por Plato 📋";

        _platoPicker = new Picker
        {
            Title = "Selecciona un plato del menú",
            ItemDisplayBinding = new Binding("Nombre")
        };
        _platoPicker.SelectedIndexChanged += async (_, _) =>
        {
            LimpiarEdicionIngrediente(mantenerPlato: true);
            await CargarIngredientesDeRecetaAsync();
        };

        _ingredientePicker = new Picker
        {
            Title = "Selecciona un ingrediente del almacén",
            ItemDisplayBinding = new Binding("Nombre")
        };

        _cantidadEntry = new Entry
        {
            Placeholder = "Cantidad requerida por plato (ej. 0.15 o 2)",
            Keyboard = Keyboard.Numeric
        };

        _btnGuardar = new Button
        {
            Text = "Guardar en Receta",
            BackgroundColor = Colors.ForestGreen,
            TextColor = Colors.White,
            FontAttributes = FontAttributes.Bold
        };
        _btnGuardar.Clicked += OnGuardarClicked;

        _btnEliminar = new Button
        {
            Text = "Quitar de Receta",
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
        _btnCancelar.Clicked += (_, _) => LimpiarEdicionIngrediente(mantenerPlato: true);

        _listView = new ListView();
        _listView.ItemTemplate = new DataTemplate(() =>
        {
            var textCell = new TextCell();
            textCell.SetBinding(TextCell.TextProperty, "Titulo");
            textCell.SetBinding(TextCell.DetailProperty, "Detalle");
            return textCell;
        });

        _listView.ItemTapped += (_, e) =>
        {
            if (e.Item is null)
                return;

            var propReceta = e.Item.GetType().GetProperty("Receta");
            if (propReceta?.GetValue(e.Item) is not Recipe receta)
                return;

            _recetaSeleccionada = receta;
            _ingredientePicker.SelectedItem = _ingredientes.FirstOrDefault(i => i.Id == receta.InventoryItemId);
            _cantidadEntry.Text = receta.CantidadRequerida.ToString("0.##", CultureInfo.InvariantCulture);

            _btnGuardar.Text = "Actualizar Cantidad";
            _btnGuardar.BackgroundColor = Colors.DarkOrange;
            _btnEliminar.IsVisible = true;
            _btnCancelar.IsVisible = true;

            _listView.SelectedItem = null;
        };

        Content = new VerticalStackLayout
        {
            Padding = 20,
            Spacing = 12,
            Children =
            {
                new Label { Text = "Plato del Menú", FontAttributes = FontAttributes.Bold, FontSize = 18 },
                _platoPicker,
                new BoxView { HeightRequest = 1, BackgroundColor = Colors.LightGray },
                new Label { Text = "Ingrediente y Cantidad por Plato", FontAttributes = FontAttributes.Bold, FontSize = 18 },
                _ingredientePicker,
                _cantidadEntry,
                _btnGuardar,
                _btnEliminar,
                _btnCancelar,
                new BoxView { HeightRequest = 1, BackgroundColor = Colors.LightGray },
                new Label { Text = "Ingredientes de la Receta", FontAttributes = FontAttributes.Bold, FontSize = 18 },
                _listView
            }
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await CargarCatalogosAsync();
        await CargarIngredientesDeRecetaAsync();
    }

    private async Task CargarCatalogosAsync()
    {
        var idPlatoActual = (_platoPicker.SelectedItem as PlatoMenu)?.Id;
        var idIngredienteActual = (_ingredientePicker.SelectedItem as InventoryItem)?.Id;

        _platos = await _databaseService.GetPlatosAsync();
        _ingredientes = await _databaseService.GetInventoryItemsAsync();

        _platoPicker.ItemsSource = _platos;
        _ingredientePicker.ItemsSource = _ingredientes;

        if (idPlatoActual.HasValue)
            _platoPicker.SelectedItem = _platos.FirstOrDefault(p => p.Id == idPlatoActual.Value);

        if (idIngredienteActual.HasValue)
            _ingredientePicker.SelectedItem = _ingredientes.FirstOrDefault(i => i.Id == idIngredienteActual.Value);
    }

    private async Task CargarIngredientesDeRecetaAsync()
    {
        if (_platoPicker.SelectedItem is not PlatoMenu plato)
        {
            _listView.ItemsSource = null;
            return;
        }

        var recetas = await _databaseService.GetRecipesByMenuItemIdAsync(plato.Id);
        var porId = _ingredientes.ToDictionary(i => i.Id);

        var filas = recetas.Select(r =>
        {
            porId.TryGetValue(r.InventoryItemId, out var insumo);
            string nombre = insumo?.Nombre ?? $"Insumo #{r.InventoryItemId}";
            string unidad = insumo?.UnidadMedida ?? string.Empty;

            return new
            {
                Receta = r,
                Titulo = nombre,
                Detalle = $"Cantidad por plato: {r.CantidadRequerida:0.##} {unidad} • Toca para editar o quitar".Trim()
            };
        }).ToList();

        _listView.ItemsSource = filas;
    }

    private async void OnGuardarClicked(object? sender, EventArgs e)
    {
        if (_platoPicker.SelectedItem is not PlatoMenu plato)
        {
            await DisplayAlert("Error", "Selecciona primero un plato del menú.", "OK");
            return;
        }

        if (_ingredientePicker.SelectedItem is not InventoryItem ingrediente)
        {
            await DisplayAlert("Error", "Selecciona un ingrediente del almacén.", "OK");
            return;
        }

        string textoCantidad = (_cantidadEntry.Text ?? string.Empty).Trim().Replace(',', '.');
        if (!decimal.TryParse(textoCantidad, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal cantidad) || cantidad <= 0)
        {
            await DisplayAlert("Error", "Ingresa una cantidad válida mayor que 0.", "OK");
            return;
        }

        var recetasExistentes = await _databaseService.GetRecipesByMenuItemIdAsync(plato.Id);
        var duplicado = recetasExistentes.FirstOrDefault(r => r.InventoryItemId == ingrediente.Id);

        int idFinal = _recetaSeleccionada?.Id ?? duplicado?.Id ?? 0;

        var receta = new Recipe
        {
            Id = idFinal,
            MenuItemId = plato.Id,
            InventoryItemId = ingrediente.Id,
            CantidadRequerida = cantidad
        };

        await _databaseService.SaveRecipeAsync(receta);

        LimpiarEdicionIngrediente(mantenerPlato: true);
        await CargarIngredientesDeRecetaAsync();

        await DisplayAlert("Éxito", $"Ingrediente guardado en la receta de '{plato.Nombre}'.", "OK");
    }

    private async void OnEliminarClicked(object? sender, EventArgs e)
    {
        if (_recetaSeleccionada == null)
            return;

        bool confirmar = await DisplayAlert(
            "Confirmar",
            "¿Deseas quitar este ingrediente de la receta?",
            "Sí, quitar",
            "Cancelar");

        if (!confirmar)
            return;

        await _databaseService.DeleteRecipeAsync(_recetaSeleccionada);

        LimpiarEdicionIngrediente(mantenerPlato: true);
        await CargarIngredientesDeRecetaAsync();

        await DisplayAlert("Eliminado", "Ingrediente retirado de la receta.", "OK");
    }

    private void LimpiarEdicionIngrediente(bool mantenerPlato)
    {
        _recetaSeleccionada = null;
        _ingredientePicker.SelectedItem = null;
        _cantidadEntry.Text = string.Empty;

        _btnGuardar.Text = "Guardar en Receta";
        _btnGuardar.BackgroundColor = Colors.ForestGreen;
        _btnEliminar.IsVisible = false;
        _btnCancelar.IsVisible = false;

        if (!mantenerPlato)
            _platoPicker.SelectedItem = null;
    }
}