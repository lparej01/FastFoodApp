using SQLite;

namespace FastFoodApp.Models;

public class User
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public string Nombre { get; set; }
    public string Correo { get; set; }
    public string Password { get; set; }
    public string Rol { get; set; } // "Admin", "Cajero"
}

public class PlatoMenu
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public string Nombre { get; set; }
    // Llave foránea hacia la tabla Categoria
    public int CategoriaId { get; set; } 
    public decimal PrecioUSD { get; set; }
}



public class Recipe
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public int MenuItemId { get; set; }
    public int InventoryItemId { get; set; }
    public decimal CantidadRequerida { get; set; }
}

public class Purchase
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public DateTime Fecha { get; set; }
    public decimal TotalUSD { get; set; }
    public decimal TasaCambioVES { get; set; }
}

public class PurchaseDetail
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public int PurchaseId { get; set; }
    public int InventoryItemId { get; set; }
    public decimal CantidadComprada { get; set; }
    public decimal CostoUnitarioUSD { get; set; }
}

public class Order
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public int UserId { get; set; }
    public DateTime Fecha { get; set; }
    public decimal TotalUSD { get; set; }
    public decimal TasaCambioVES { get; set; }
    public string Estado { get; set; } // "Completado", "Cancelado"
}

public class OrderDetail
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int MenuItemId { get; set; }
    public int Cantidad { get; set; }
    public decimal PrecioVentaUSD { get; set; }
    public decimal CostoEstimadoUSD { get; set; }
}

public class PurchaseItem
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public int IngredientId { get; set; }           // ID del ingrediente en la tabla de Inventario
    public string IngredientName { get; set; }      // Nombre del ingrediente (para mostrarlo fácilmente en listas)

    public decimal QuantityPurchased { get; set; }  // Cantidad que ingresa (ej. 5000 gramos, 10 litros)
    public decimal UnitCost { get; set; }           // Costo por unidad en USD
    public decimal TotalCost { get; set; }          // QuantityPurchased * UnitCost

    public DateTime PurchaseDate { get; set; }      // Fecha y hora en que se realizó la compra
}