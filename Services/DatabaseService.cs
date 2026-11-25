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

    public async Task<int> SavePlatoAsync(PlatoMenu plato)
    {
        await Init();
        // Si el plato ya tiene ID, lo actualiza; si es 0, lo inserta nuevo.
        // Asegúrate de que guarde todo el objeto para que no se pierda el CategoriaId.
        if (plato.Id != 0)
        {
            return await _database.UpdateAsync(plato);
        }
        else
        {
            return await _database.InsertAsync(plato);
        }
    }

    public async Task<List<PlatoMenu>> GetPlatosAsync()
    {
        await Init();
        return await _database.Table<PlatoMenu>().ToListAsync();
    }

    // --- MÉTODOS PARA CATEGORÍAS ---

    // 1. Obtener todas las categorías para llenar el Picker
    public async Task<List<Categoria>> GetCategoriasAsync()
    {
        await Init();
        return await _database.Table<Categoria>().ToListAsync();
    }

    // 2. Guardar una nueva categoría (por si quieres crear una pantalla para registrarlas)
    public async Task<int> SaveCategoriaAsync(Categoria categoria)
    {
        await Init();
        return await _database.InsertAsync(categoria);
    }

    // --- MÉTODOS PARA EL CONTROL DE INVENTARIO ---

    // 1. Obtener todos los elementos del inventario
    public async Task<List<InventoryItem>> GetInventoryItemsAsync()
    {
        await Init();
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

    public async Task<int> DeleteInventoryItemAsync(InventoryItem item)
    {
        await Init();
        return await _database.DeleteAsync(item);
    }

    public async Task RegisterPurchaseAsync(int ingredientId, decimal quantity, decimal unitCost)
    {
        await Init();

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
        await Init();
        return await _database.Table<PurchaseItem>().OrderByDescending(p => p.PurchaseDate).ToListAsync();
    }

    public async Task<int> SaveOrderAsync(Order order)
    {
        await Init();
        return await _database.InsertAsync(order);
    }

    public async Task<int> SaveOrderDetailAsync(OrderDetail detail)
    {
        await Init();
        return await _database.InsertAsync(detail);
    }

    public async Task<List<Order>> GetOrdersAsync()
    {
        await Init();
        return await _database.Table<Order>().OrderByDescending(o => o.Fecha).ToListAsync();
    }

    public async Task<List<OrderDetail>> GetOrderDetailsAsync(int orderId)
    {
        await Init();
        return await _database.Table<OrderDetail>()
            .Where(d => d.OrderId == orderId)
            .ToListAsync();
    }

    public async Task<List<Recipe>> GetRecipesByMenuItemIdAsync(int menuItemId)
    {
        await Init();
        return await _database.Table<Recipe>()
            .Where(r => r.MenuItemId == menuItemId)
            .ToListAsync();
    }

    public async Task<InventoryItem?> GetInventoryItemByIdAsync(int id)
    {
        await Init();
        return await _database.Table<InventoryItem>()
            .Where(i => i.Id == id)
            .FirstOrDefaultAsync();
    }

    public async Task<int> UpdateInventoryItemAsync(InventoryItem item)
    {
        await Init();
        return await _database.UpdateAsync(item);
    }
    public async Task<bool> EmailExistsAsync(string correo)
    {
        await Init();
        var correoNormalizado = correo.Trim().ToLowerInvariant();
        var existing = await _database.Table<User>()
                                      .Where(u => u.Correo.ToLower() == correoNormalizado)
                                      .FirstOrDefaultAsync();
        return existing != null;
    }

    public async Task<(bool Success, string Message)> RegisterUserAsync(string nombre, string correo, string password, string rol = "Cajero")
    {
        await Init();

        nombre = nombre?.Trim() ?? string.Empty;
        correo = correo?.Trim().ToLowerInvariant() ?? string.Empty;
        password = password ?? string.Empty;

        if (string.IsNullOrWhiteSpace(nombre) || string.IsNullOrWhiteSpace(correo) || string.IsNullOrWhiteSpace(password))
            return (false, "Completa nombre, correo y contraseña.");

        if (password.Length < 4)
            return (false, "La contraseña debe tener al menos 4 caracteres.");

        if (await EmailExistsAsync(correo))
            return (false, "Ese correo ya está registrado.");

        var newUser = new User
        {
            Nombre = nombre,
            Correo = correo,
            Password = password,
            Rol = rol
        };

        await _database.InsertAsync(newUser);
        return (true, "Usuario registrado correctamente.");
    }

    public async Task<(bool Success, string Message)> ResetPasswordAsync(string correo, string nombre, string newPassword)
    {
        await Init();

        correo = correo?.Trim().ToLowerInvariant() ?? string.Empty;
        nombre = nombre?.Trim() ?? string.Empty;
        newPassword = newPassword ?? string.Empty;

        if (string.IsNullOrWhiteSpace(correo) || string.IsNullOrWhiteSpace(nombre) || string.IsNullOrWhiteSpace(newPassword))
            return (false, "Completa el correo, el nombre registrado y la nueva contraseña.");

        if (newPassword.Length < 4)
            return (false, "La nueva contraseña debe tener al menos 4 caracteres.");

        var user = await _database.Table<User>()
                                  .Where(u => u.Correo.ToLower() == correo)
                                  .FirstOrDefaultAsync();

        if (user == null || !string.Equals(user.Nombre?.Trim(), nombre, StringComparison.OrdinalIgnoreCase))
            return (false, "No se encontró un usuario que coincida con ese correo y nombre.");

        user.Password = newPassword;
        await _database.UpdateAsync(user);
        return (true, "Contraseña actualizada correctamente.");
    }
}