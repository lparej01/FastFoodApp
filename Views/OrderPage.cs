using System.Collections.ObjectModel;
using System.Globalization;
using FastFoodApp.Models;
using FastFoodApp.Services;
using Microsoft.Maui.Storage;

namespace FastFoodApp.Views;

public class OrderItemDraft
{
    public int MenuItemId { get; set; }
    public string NombrePlato { get; set; } = string.Empty;
    public int Cantidad { get; set; }
    public decimal PrecioUnitarioUSD { get; set; }
    public decimal SubtotalUSD => Cantidad * PrecioUnitarioUSD;
    public string Resumen => $"{Cantidad}x {NombrePlato} — ${SubtotalUSD:F2} USD (toca para editar/quitar)";
}

public class OrderPage : ContentPage
{
    private const string PrefUltimaTasa = "FastFoodApp_UltimaTasaVES";
    private const string PrefFechaUltimaTasa = "FastFoodApp_FechaUltimaTasa";

    private readonly DatabaseService _databaseService;
    private readonly ObservableCollection<OrderItemDraft> _carrito = new();
    private List<PlatoMenu> _platosDisponibles = new();

    private readonly Picker _platoPicker;
    private readonly Entry _cantidadEntry;
    private readonly Entry _tasaEntry;
    private readonly Label _estadoTasaLabel;
    private readonly Label _totalLabel;
    private readonly ListView _carritoListView;

    public OrderPage(DatabaseService databaseService)
    {
        _databaseService = databaseService;
        Title = "Tomar Pedidos 📝";

        _platoPicker = new Picker
        {
            Title = "Selecciona un plato del menú",
            ItemDisplayBinding = new Binding("Nombre")
        };

        _cantidadEntry = new Entry
        {
            Placeholder = "Cantidad (ej. 2)",
            Keyboard = Keyboard.Numeric,
            Text = "1"
        };

        _tasaEntry = new Entry
        {
            Placeholder = "Precio del dólar en VES (ej. 45.00)",
            Keyboard = Keyboard.Numeric
        };

        _estadoTasaLabel = new Label
        {
            FontSize = 12,
            TextColor = Colors.DimGray
        };

        var btnGuardarTasa = new Button
        {
            Text = "Guardar tasa del día",
            BackgroundColor = Colors.DarkSlateBlue,
            TextColor = Colors.White,
            FontAttributes = FontAttributes.Bold
        };

        _tasaEntry.TextChanged += (_, _) =>
        {
            if (TryParseTasa(_tasaEntry.Text, out decimal tasaValida))
            {
                GuardarTasaDelDia(tasaValida);
            }

            ActualizarTotal();
        };

        btnGuardarTasa.Clicked += async (_, _) =>
        {
            if (!TryParseTasa(_tasaEntry.Text, out decimal tasaValida))
            {
                await DisplayAlert("Aviso", "Ingresa una tasa de cambio válida mayor que 0.", "OK");
                return;
            }

            GuardarTasaDelDia(tasaValida);
            ActualizarTotal();
            await DisplayAlert("Tasa Guardada", $"Tasa del día establecida en {tasaValida:F2} VES por USD.", "OK");
        };

        var btnAgregarItem = new Button
        {
            Text = "Agregar al Pedido",
            BackgroundColor = Colors.DodgerBlue,
            TextColor = Colors.White,
            FontAttributes = FontAttributes.Bold
        };

        var btnVaciarCarrito = new Button
        {
            Text = "Vaciar carrito",
            BackgroundColor = Colors.IndianRed,
            TextColor = Colors.White,
            FontAttributes = FontAttributes.Bold
        };

        _carritoListView = new ListView
        {
            HeightRequest = 160,
            ItemsSource = _carrito,
            ItemTemplate = new DataTemplate(() =>
            {
                var cell = new TextCell();
                cell.SetBinding(TextCell.TextProperty, "Resumen");
                return cell;
            })
        };

        _carritoListView.ItemSelected += async (_, e) =>
        {
            if (e.SelectedItem is not OrderItemDraft itemSeleccionado)
                return;

            _carritoListView.SelectedItem = null;

            string accion = await DisplayActionSheet(
                $"{itemSeleccionado.NombrePlato} ({itemSeleccionado.Cantidad}x)",
                "Cancelar",
                "Quitar plato",
                "Reducir en 1");

            int index = _carrito.IndexOf(itemSeleccionado);
            if (index < 0)
                return;

            if (accion == "Reducir en 1")
            {
                int nuevaCantidad = itemSeleccionado.Cantidad - 1;
                if (nuevaCantidad <= 0)
                {
                    _carrito.RemoveAt(index);
                }
                else
                {
                    _carrito[index] = new OrderItemDraft
                    {
                        MenuItemId = itemSeleccionado.MenuItemId,
                        NombrePlato = itemSeleccionado.NombrePlato,
                        Cantidad = nuevaCantidad,
                        PrecioUnitarioUSD = itemSeleccionado.PrecioUnitarioUSD
                    };
                }

                ActualizarTotal();
            }
            else if (accion == "Quitar plato")
            {
                _carrito.RemoveAt(index);
                ActualizarTotal();
            }
        };

        btnVaciarCarrito.Clicked += async (_, _) =>
        {
            if (_carrito.Count == 0)
                return;

            bool confirmar = await DisplayAlert(
                "Vaciar carrito",
                "¿Deseas quitar todos los platos del pedido actual?",
                "Sí, vaciar",
                "Cancelar");

            if (!confirmar)
                return;

            _carrito.Clear();
            ActualizarTotal();
        };

        _totalLabel = new Label
        {
            Text = "Total: $0.00 USD (0.00 VES)",
            FontSize = 18,
            FontAttributes = FontAttributes.Bold
        };

        var btnConfirmarPedido = new Button
        {
            Text = "Confirmar y Guardar Pedido",
            BackgroundColor = Colors.ForestGreen,
            TextColor = Colors.White,
            FontAttributes = FontAttributes.Bold,
            HeightRequest = 48
        };

        var btnVerHistorial = new Button
        {
            Text = "Ver Historial de Pedidos",
            BackgroundColor = Colors.SlateGray,
            TextColor = Colors.White,
            FontAttributes = FontAttributes.Bold,
            HeightRequest = 45
        };

        btnAgregarItem.Clicked += async (_, _) =>
        {
            if (_platoPicker.SelectedItem is not PlatoMenu platoSeleccionado)
            {
                await DisplayAlert("Aviso", "Selecciona un plato de la lista.", "OK");
                return;
            }

            if (!int.TryParse(_cantidadEntry.Text, out int cantidad) || cantidad <= 0)
            {
                await DisplayAlert("Aviso", "Ingresa una cantidad válida mayor que 0.", "OK");
                return;
            }

            var existente = _carrito.FirstOrDefault(x => x.MenuItemId == platoSeleccionado.Id);
            if (existente != null)
            {
                existente.Cantidad += cantidad;
                int index = _carrito.IndexOf(existente);
                _carrito[index] = new OrderItemDraft
                {
                    MenuItemId = existente.MenuItemId,
                    NombrePlato = existente.NombrePlato,
                    Cantidad = existente.Cantidad,
                    PrecioUnitarioUSD = existente.PrecioUnitarioUSD
                };
            }
            else
            {
                _carrito.Add(new OrderItemDraft
                {
                    MenuItemId = platoSeleccionado.Id,
                    NombrePlato = platoSeleccionado.Nombre,
                    Cantidad = cantidad,
                    PrecioUnitarioUSD = platoSeleccionado.PrecioUSD
                });
            }

            ActualizarTotal();
            _cantidadEntry.Text = "1";
        };

        btnConfirmarPedido.Clicked += async (_, _) =>
        {
            if (_carrito.Count == 0)
            {
                await DisplayAlert("Aviso", "Agrega al menos un plato antes de guardar el pedido.", "OK");
                return;
            }

            // 1. Calcular requerimientos totales por insumo antes de modificar la base de datos
            var requerimientosPorInsumo = new Dictionary<int, float>();
            var recetasPorItem = new Dictionary<int, List<Recipe>>();

            foreach (var item in _carrito)
            {
                var recetas = await _databaseService.GetRecipesByMenuItemIdAsync(item.MenuItemId);
                recetasPorItem[item.MenuItemId] = recetas;

                foreach (var receta in recetas)
                {
                    float cantidadRequerida = (float)(receta.CantidadRequerida * item.Cantidad);
                    if (requerimientosPorInsumo.ContainsKey(receta.InventoryItemId))
                        requerimientosPorInsumo[receta.InventoryItemId] += cantidadRequerida;
                    else
                        requerimientosPorInsumo[receta.InventoryItemId] = cantidadRequerida;
                }
            }

            // 2. Verificar si existe stock suficiente para cada insumo requerido
            var faltantes = new List<string>();
            var insumosCargados = new Dictionary<int, InventoryItem>();

            foreach (var kvp in requerimientosPorInsumo)
            {
                int insumoId = kvp.Key;
                float requerido = kvp.Value;
                var insumo = await _databaseService.GetInventoryItemByIdAsync(insumoId);

                if (insumo == null)
                {
                    faltantes.Add($"- Insumo #{insumoId}: no encontrado en inventario (requerido: {requerido:0.##})");
                }
                else
                {
                    insumosCargados[insumoId] = insumo;
                    if (insumo.StockActual < requerido)
                    {
                        faltantes.Add(
                            $"- {insumo.Nombre}: disponible {insumo.StockActual:0.##} {insumo.UnidadMedida}, requerido {requerido:0.##} {insumo.UnidadMedida}");
                    }
                }
            }

            if (faltantes.Count > 0)
            {
                string mensajeFaltantes = "No hay stock suficiente para confirmar el pedido:\n\n" +
                                         string.Join("\n", faltantes);
                await DisplayAlert("Stock Insuficiente", mensajeFaltantes, "OK");
                return;
            }

            // 3. Guardar pedido, descontar inventario y registrar detalle
            decimal tasa = ObtenerTasaActual();
            GuardarTasaDelDia(tasa);

            decimal totalUsd = _carrito.Sum(x => x.SubtotalUSD);

            var order = new Order
            {
                UserId = 1,
                Fecha = DateTime.Now,
                TotalUSD = totalUsd,
                TasaCambioVES = tasa,
                Estado = "Completado"
            };

            await _databaseService.SaveOrderAsync(order);

            var insumosBajoMinimo = new Dictionary<int, InventoryItem>();

            foreach (var item in _carrito)
            {
                var recetas = recetasPorItem[item.MenuItemId];

                foreach (var receta in recetas)
                {
                    if (insumosCargados.TryGetValue(receta.InventoryItemId, out var insumo))
                    {
                        float cantidadADescontar = (float)(receta.CantidadRequerida * item.Cantidad);
                        insumo.StockActual = Math.Max(0f, insumo.StockActual - cantidadADescontar);
                        await _databaseService.UpdateInventoryItemAsync(insumo);

                        if (insumo.StockActual <= insumo.StockMinimo)
                        {
                            insumosBajoMinimo[insumo.Id] = insumo;
                        }
                    }
                }

                var detail = new OrderDetail
                {
                    OrderId = order.Id,
                    MenuItemId = item.MenuItemId,
                    Cantidad = item.Cantidad,
                    PrecioVentaUSD = item.PrecioUnitarioUSD,
                    CostoEstimadoUSD = 0m
                };

                await _databaseService.SaveOrderDetailAsync(detail);
            }

            decimal totalVes = totalUsd * tasa;
            await DisplayAlert(
                "Pedido Guardado",
                $"Pedido #{order.Id} registrado por ${totalUsd:F2} USD ({totalVes:F2} VES) e inventario actualizado.",
                "OK");

            if (insumosBajoMinimo.Count > 0)
            {
                var lineasAlerta = insumosBajoMinimo.Values.Select(i =>
                    $"- {i.Nombre}: actual {i.StockActual:0.##} {i.UnidadMedida} (mínimo: {i.StockMinimo:0.##} {i.UnidadMedida})");

                await DisplayAlert(
                    "Alerta de Stock Mínimo",
                    "Los siguientes insumos quedaron iguales o por debajo de su stock mínimo:\n\n" +
                    string.Join("\n", lineasAlerta),
                    "OK");
            }

            _carrito.Clear();
            ActualizarTotal();
        };

        btnVerHistorial.Clicked += async (_, _) =>
        {
            await Navigation.PushAsync(new OrderHistoryPage(_databaseService));
        };

        CargarTasaGuardada();

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = 20,
                Spacing = 12,
                Children =
                {
                    new Label { Text = "Nuevo Pedido", FontSize = 20, FontAttributes = FontAttributes.Bold },
                    _platoPicker,
                    _cantidadEntry,
                    new Label { Text = "Tasa del día (VES por USD)", FontSize = 14, FontAttributes = FontAttributes.Bold },
                    _tasaEntry,
                    _estadoTasaLabel,
                    btnGuardarTasa,
                    btnAgregarItem,
                    new BoxView { HeightRequest = 1, BackgroundColor = Colors.LightGray },
                    new Label { Text = "Detalle del Pedido", FontSize = 16, FontAttributes = FontAttributes.Bold },
                    _carritoListView,
                    btnVaciarCarrito,
                    _totalLabel,
                    btnConfirmarPedido,
                    btnVerHistorial
                }
            }
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        CargarTasaGuardada();
        _platosDisponibles = await _databaseService.GetPlatosAsync();
        _platoPicker.ItemsSource = _platosDisponibles;
        ActualizarTotal();
    }

    private void CargarTasaGuardada()
    {
        string hoy = DateTime.Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        string fechaGuardada = Preferences.Default.Get(PrefFechaUltimaTasa, string.Empty);
        string tasaGuardada = Preferences.Default.Get(PrefUltimaTasa, "1.00");

        if (string.IsNullOrWhiteSpace(_tasaEntry.Text))
        {
            _tasaEntry.Text = tasaGuardada;
        }

        if (fechaGuardada == hoy)
        {
            _estadoTasaLabel.Text = $"Tasa activa de hoy ({DateTime.Today:dd/MM/yyyy})";
        }
        else if (!string.IsNullOrWhiteSpace(fechaGuardada))
        {
            _estadoTasaLabel.Text = $"Última tasa guardada ({fechaGuardada}). Revísala o actualízala para hoy.";
        }
        else
        {
            _estadoTasaLabel.Text = "Coloca el precio del dólar en bolívares para la jornada.";
        }
    }

    private void GuardarTasaDelDia(decimal tasa)
    {
        string hoy = DateTime.Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        Preferences.Default.Set(PrefUltimaTasa, tasa.ToString("F2", CultureInfo.InvariantCulture));
        Preferences.Default.Set(PrefFechaUltimaTasa, hoy);
        _estadoTasaLabel.Text = $"Tasa activa de hoy ({DateTime.Today:dd/MM/yyyy}): {tasa:F2} VES";
    }

    private static bool TryParseTasa(string? texto, out decimal tasa)
    {
        tasa = 0m;
        if (string.IsNullOrWhiteSpace(texto))
            return false;

        string normalizado = texto.Trim().Replace(',', '.');
        return decimal.TryParse(normalizado, NumberStyles.Any, CultureInfo.InvariantCulture, out tasa) && tasa > 0;
    }

    private decimal ObtenerTasaActual()
    {
        return TryParseTasa(_tasaEntry.Text, out decimal tasa) ? tasa : 1m;
    }

    private void ActualizarTotal()
    {
        decimal totalUsd = _carrito.Sum(x => x.SubtotalUSD);
        decimal tasa = ObtenerTasaActual();
        decimal totalVes = totalUsd * tasa;
        _totalLabel.Text = $"Total: ${totalUsd:F2} USD ({totalVes:F2} VES)";
    }
}