using SQLite;
using FastFoodApp.Models;
using System.Globalization;

namespace FastFoodApp.Services;

public class DatabaseService
{
    private SQLiteAsyncConnection _database;
    private bool _isInitialized = false;

    private async Task Init()
    {
        if (_isInitialized && _database != null)
            return;

        try
        {
            var databasePath = Path.Combine(FileSystem.AppDataDirectory, "FastFoodBusiness.db3");
            _database = new SQLiteAsyncConnection(databasePath);

            // Creación automática de todas las tablas
            await _database.CreateTableAsync<User>();
            await _database.CreateTableAsync<PlatoMenu>();
            await _database.CreateTableAsync<Categoria>();
            await _database.CreateTableAsync<InventoryItem>();            
            await _database.CreateTableAsync<Recipe>();
            await _database.CreateTableAsync<Purchase>();
            await _database.CreateTableAsync<PurchaseDetail>();
            await _database.CreateTableAsync<Order>();
            await _database.CreateTableAsync<OrderDetail>();
            await _database.CreateTableAsync<PurchaseItem>();

            // Crear usuario Administrador por defecto si la tabla está vacía
            var adminCheck = await _database.Table<User>().CountAsync();
            if (adminCheck == 0)
            {
                await _database.InsertAsync(new User
                {
                    Nombre = "Administrador",
                    Correo = "admin@foodapp.com",
                    Password = "123",
                    Rol = "Admin"
                });
            }

            _isInitialized = true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error inicializando la BD: {ex.Message}");
        }
    }

    public async Task<User> LoginAsync(string correo, string password)
    {
        await Init();
        return await _database.Table<User>()
                              .Where(u => u.Correo == correo && u.Password == password)
                              .FirstOrDefaultAsync();
    }

    public Task<int> SavePlatoAsync(PlatoMenu plato)
    {
        // Si el plato ya tiene ID, lo actualiza; si es 0, lo inserta nuevo.
        // Asegúrate de que guarde todo el objeto para que no se pierda el CategoriaId.
        if (plato.Id != 0)
        {
            return _database.UpdateAsync(plato);
        }
        else
        {
            return _database.InsertAsync(plato);
        }
    }
    public async Task<List<PlatoMenu>> GetPlatosAsync()
    {
        await Init();
        return await _database.Table<PlatoMenu>().ToListAsync();
    }
    // --- MÉTODOS PARA CATEGORÍAS ---

    // 1. Obtener todas las categorías para llenar el Picker
    public Task<List<Categoria>> GetCategoriasAsync()
    {
        // Asegúrate de usar _database (o la variable que uses para tu conexión SQLiteAsyncConnection)
        return _database.Table<Categoria>().ToListAsync();
    }

    // 2. Guardar una nueva categoría (por si quieres crear una pantalla para registrarlas)
    public Task<int> SaveCategoriaAsync(Categoria categoria)
    {
        return _database.InsertAsync(categoria);
    }
    // --- MÉTODOS PARA EL CONTROL DE INVENTARIO ---

    // 1. Obtener todos los elementos del inventario
   
    public async Task<List<InventoryItem>> GetInventoryItemsAsync()
    {
        return await _database.Table<InventoryItem>().ToListAsync();
    }


    // 2. Guardar o actualizar un elemento del inventario
    public async Task<int> SaveInventoryItemAsync(InventoryItem item)
    {
        await Init();
        if (item.Id != 0)
        {
            return await _database.UpdateAsync(item);
        }
        else
        {
            return await _database.InsertAsync(item);
        }
    }

    public Task<int> DeleteInventoryItemAsync(InventoryItem item)
    {
        return _database.DeleteAsync(item);
    }

   

    public async Task RegisterPurchaseAsync(int ingredientId, decimal quantity, decimal unitCost)
    {
    // 1. Buscar el ingrediente actual en el inventario
    var inventoryItem = await _database.Table<InventoryItem>()
                                       .Where(i => i.Id == ingredientId)
                                       .FirstOrDefaultAsync();

    if (inventoryItem != null)
    {
        // 2. Calcular costos
        decimal totalCost = quantity * unitCost;

        // 3. Crear el registro de la compra
        var purchase = new PurchaseItem
        {
            IngredientId = ingredientId,
            IngredientName = inventoryItem.Nombre,
            QuantityPurchased = quantity,
            UnitCost = unitCost,
            TotalCost = totalCost,
            PurchaseDate = DateTime.Now
        };

        await _database.InsertAsync(purchase);

            // 4. Actualizar el stock del inventario (sumando lo comprado)
         inventoryItem.StockActual += (float)quantity; 
        await _database.UpdateAsync(inventoryItem);
    }
  }
   

    // 2. Obtener el historial de compras recientes
    public async Task<List<PurchaseItem>> GetPurchasesAsync()
    {
        return await _database.Table<PurchaseItem>().OrderByDescending(p => p.PurchaseDate).ToListAsync();
    }
}