using FastFoodApp.Models;
using FastFoodApp.Services;

namespace FastFoodApp.Views;

public class OrderHistoryItem
{
    public int Id { get; set; }
    public DateTime Fecha { get; set; }
    public decimal TotalUSD { get; set; }
    public decimal TasaCambioVES { get; set; }
    public decimal TotalVES => TotalUSD * TasaCambioVES;
    public string Estado { get; set; } = string.Empty;
    public string ResumenPlatos { get; set; } = string.Empty;
    public Order PedidoOriginal { get; set; } = null!;
}

public class OrderDetailDisplay
{
    public string NombrePlato { get; set; } = string.Empty;
    public int Cantidad { get; set; }
    public decimal PrecioVentaUSD { get; set; }
    public decimal TasaCambioVES { get; set; }
    public decimal SubtotalUSD => Cantidad * PrecioVentaUSD;
    public decimal SubtotalVES => SubtotalUSD * TasaCambioVES;
    public string ResumenLinea =>
        $"{Cantidad}x {NombrePlato} (${PrecioVentaUSD:F2} c/u) — ${SubtotalUSD:F2} USD ({SubtotalVES:F2} VES)";
}

public class OrderHistoryPage : ContentPage
{
    private readonly DatabaseService _databaseService;
    private readonly DatePicker _fechaPicker;
    private readonly Label _resumenDiaLabel;
    private readonly ListView _ordersListView;
    private readonly ListView _detailsListView;
    private readonly Label _selectedOrderLabel;

    private Dictionary<int, string> _nombresPlatos = new();
    private List<Order> _todosLosPedidos = new();
    private bool _mostrandoTodos;

    public OrderHistoryPage(DatabaseService databaseService)
    {
        _databaseService = databaseService;
        Title = "Historial de Pedidos 📋";

        _fechaPicker = new DatePicker
        {
            Date = DateTime.Today,
            Format = "dd/MM/yyyy"
        };

        var btnVerTodos = new Button
        {
            Text = "Ver todos",
            BackgroundColor = Colors.SlateGray,
            TextColor = Colors.White,
            FontAttributes = FontAttributes.Bold,
            Padding = new Thickness(12, 6)
        };

        var filtroRow = new HorizontalStackLayout
        {
            Spacing = 10,
            VerticalOptions = LayoutOptions.Center,
            Children =
            {
                new Label
                {
                    Text = "Fecha:",
                    FontAttributes = FontAttributes.Bold,
                    VerticalOptions = LayoutOptions.Center
                },
                _fechaPicker,
                btnVerTodos
            }
        };

        _resumenDiaLabel = new Label
        {
            Text = "Ventas del día: $0.00 USD | 0.00 VES (0 pedidos)",
            FontSize = 15,
            FontAttributes = FontAttributes.Bold,
            TextColor = Colors.DarkGreen
        };

        _ordersListView = new ListView
        {
            HeightRequest = 220,
            ItemTemplate = new DataTemplate(() =>
            {
                var cell = new ViewCell();

                var grid = new Grid
                {
                    Padding = new Thickness(10),
                    RowSpacing = 4,
                    RowDefinitions = new RowDefinitionCollection
                    {
                        new RowDefinition { Height = GridLength.Auto },
                        new RowDefinition { Height = GridLength.Auto }
                    },
                    ColumnDefinitions = new ColumnDefinitionCollection
                    {
                        new ColumnDefinition { Width = GridLength.Auto },
                        new ColumnDefinition { Width = GridLength.Star },
                        new ColumnDefinition { Width = GridLength.Auto }
                    }
                };

                var idLabel = new Label
                {
                    FontAttributes = FontAttributes.Bold,
                    VerticalOptions = LayoutOptions.Center
                };
                idLabel.SetBinding(Label.TextProperty, new Binding("Id", stringFormat: "Pedido #{0}"));

                var dateLabel = new Label
                {
                    TextColor = Colors.Gray,
                    VerticalOptions = LayoutOptions.Center,
                    HorizontalOptions = LayoutOptions.Center
                };
                dateLabel.SetBinding(Label.TextProperty, new Binding("Fecha", stringFormat: "{0:dd/MM/yyyy HH:mm}"));

                var totalStack = new VerticalStackLayout
                {
                    HorizontalOptions = LayoutOptions.End,
                    Spacing = 1
                };

                var totalUsdLabel = new Label
                {
                    TextColor = Colors.Green,
                    FontAttributes = FontAttributes.Bold,
                    HorizontalTextAlignment = TextAlignment.End
                };
                totalUsdLabel.SetBinding(Label.TextProperty, new Binding("TotalUSD", stringFormat: "${0:F2} USD"));

                var totalVesLabel = new Label
                {
                    FontSize = 12,
                    TextColor = Colors.DimGray,
                    HorizontalTextAlignment = TextAlignment.End
                };
                totalVesLabel.SetBinding(Label.TextProperty, new Binding("TotalVES", stringFormat: "{0:F2} VES"));

                totalStack.Children.Add(totalUsdLabel);
                totalStack.Children.Add(totalVesLabel);

                var platosLabel = new Label
                {
                    FontSize = 13,
                    TextColor = Colors.DarkSlateGray
                };
                platosLabel.SetBinding(Label.TextProperty, "ResumenPlatos");

                Grid.SetRow(idLabel, 0);
                Grid.SetColumn(idLabel, 0);

                Grid.SetRow(dateLabel, 0);
                Grid.SetColumn(dateLabel, 1);

                Grid.SetRow(totalStack, 0);
                Grid.SetColumn(totalStack, 2);

                Grid.SetRow(platosLabel, 1);
                Grid.SetColumn(platosLabel, 0);
                Grid.SetColumnSpan(platosLabel, 3);

                grid.Children.Add(idLabel);
                grid.Children.Add(dateLabel);
                grid.Children.Add(totalStack);
                grid.Children.Add(platosLabel);

                cell.View = grid;
                return cell;
            })
        };

        _selectedOrderLabel = new Label
        {
            Text = "Selecciona un pedido para ver su detalle",
            FontSize = 16,
            FontAttributes = FontAttributes.Bold
        };

        _detailsListView = new ListView
        {
            ItemTemplate = new DataTemplate(() =>
            {
                var cell = new TextCell();
                cell.SetBinding(TextCell.TextProperty, "ResumenLinea");
                return cell;
            })
        };

        _fechaPicker.DateSelected += async (_, e) =>
        {
            _mostrandoTodos = false;
            DateTime fechaElegida = e.NewDate ?? _fechaPicker.Date ?? DateTime.Today;
            await AplicarFiltroYResumenAsync(fechaElegida);
        };

        btnVerTodos.Clicked += async (_, _) =>
        {
            _mostrandoTodos = true;
            await AplicarFiltroYResumenAsync(_fechaPicker.Date ?? DateTime.Today);
        };

        _ordersListView.ItemSelected += async (_, e) =>
        {
            if (e.SelectedItem is not OrderHistoryItem itemSeleccionado)
                return;

            await CargarDetallePedido(itemSeleccionado.PedidoOriginal);
        };

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = 20,
                Spacing = 12,
                Children =
                {
                    new Label { Text = "Pedidos Registrados", FontSize = 20, FontAttributes = FontAttributes.Bold },
                    filtroRow,
                    _resumenDiaLabel,
                    _ordersListView,
                    new BoxView { HeightRequest = 1, BackgroundColor = Colors.LightGray },
                    _selectedOrderLabel,
                    _detailsListView
                }
            }
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        var platos = await _databaseService.GetPlatosAsync();
        _nombresPlatos = platos.ToDictionary(p => p.Id, p => p.Nombre);

        _todosLosPedidos = await _databaseService.GetOrdersAsync();
        await AplicarFiltroYResumenAsync(_fechaPicker.Date ?? DateTime.Today);
    }

    private async Task AplicarFiltroYResumenAsync(DateTime fechaSeleccionada)
    {
        DateTime diaObjetivo = fechaSeleccionada.Date;
        var pedidosDelDia = _todosLosPedidos
         .Where(p => p.Fecha.Date == diaObjetivo)
         .ToList();

        decimal totalUsdDia = pedidosDelDia.Sum(p => p.TotalUSD);
        decimal totalVesDia = pedidosDelDia.Sum(p => p.TotalUSD * p.TasaCambioVES);
        int cantidadPedidosDia = pedidosDelDia.Count;

        _resumenDiaLabel.Text =
            $"Total {diaObjetivo:dd/MM/yyyy}: ${totalUsdDia:F2} USD | {totalVesDia:F2} VES ({cantidadPedidosDia} {(cantidadPedidosDia == 1 ? "pedido" : "pedidos")})";

        var pedidosAMostrar = _mostrandoTodos ? _todosLosPedidos : pedidosDelDia;
        var listaHistorial = new List<OrderHistoryItem>();

        foreach (var pedido in pedidosAMostrar)
        {
            var detalles = await _databaseService.GetOrderDetailsAsync(pedido.Id);
            var resumen = string.Join(", ", detalles.Select(d =>
            {
                string nombrePlato = _nombresPlatos.TryGetValue(d.MenuItemId, out var nombre)
                    ? nombre
                    : $"Plato #{d.MenuItemId}";
                return $"{d.Cantidad}x {nombrePlato}";
            }));

            listaHistorial.Add(new OrderHistoryItem
            {
                Id = pedido.Id,
                Fecha = pedido.Fecha,
                TotalUSD = pedido.TotalUSD,
                TasaCambioVES = pedido.TasaCambioVES,
                Estado = pedido.Estado,
                ResumenPlatos = string.IsNullOrWhiteSpace(resumen) ? "Sin platos" : resumen,
                PedidoOriginal = pedido
            });
        }

        _ordersListView.ItemsSource = listaHistorial;
        _ordersListView.SelectedItem = null;
        _selectedOrderLabel.Text = "Selecciona un pedido para ver su detalle";
        _detailsListView.ItemsSource = null;
    }

    private async Task CargarDetallePedido(Order pedido)
    {
        var detalles = await _databaseService.GetOrderDetailsAsync(pedido.Id);
        decimal totalVes = pedido.TotalUSD * pedido.TasaCambioVES;
        string fechaTexto = pedido.Fecha.ToString("dd/MM/yyyy HH:mm");

        var filasVisuales = detalles.Select(d => new OrderDetailDisplay
        {
            NombrePlato = _nombresPlatos.TryGetValue(d.MenuItemId, out var nombre)
                ? nombre
                : $"Plato #{d.MenuItemId}",
            Cantidad = d.Cantidad,
            PrecioVentaUSD = d.PrecioVentaUSD,
            TasaCambioVES = pedido.TasaCambioVES
        }).ToList();

        _selectedOrderLabel.Text =
            $"Pedido #{pedido.Id} | {fechaTexto} | Total: ${pedido.TotalUSD:F2} USD ({totalVes:F2} VES) | Tasa: {pedido.TasaCambioVES:F2} | {pedido.Estado}";

        _detailsListView.ItemsSource = filasVisuales;
    }
}
