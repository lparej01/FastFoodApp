using SQLite;

namespace FastFoodApp.Models;

public class InventoryItem
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [NotNull, MaxLength(100)]
    public string Nombre { get; set; }

    [NotNull]
    public float StockActual { get; set; }

    [NotNull, MaxLength(50)]
    public string UnidadMedida { get; set; }

    [NotNull]
    public float StockMinimo { get; set; }
}