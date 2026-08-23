namespace Shop.Catalog.Api;

// EF entity: Id is assigned by the database on insert.
public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
}
