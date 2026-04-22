namespace ETL_simulator.Generator
{
    public record StoreEntry(string Code, string Name, string City, string Country);
    public record ProductEntry(string Code, string Name, string Category, decimal Price);
    public record CustomerEntry(string Code, string FullName, string Gender, DateTime? BirthDate, string City);
}
