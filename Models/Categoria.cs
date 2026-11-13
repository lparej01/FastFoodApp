using SQLite;

public class Categoria
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [NotNull, Unique]
    public string Nombre { get; set; }
}