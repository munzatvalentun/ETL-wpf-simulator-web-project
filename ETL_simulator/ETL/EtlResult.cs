namespace ETL_simulator.ETL
{
    public record BronzeResult(int Inserted);
    public record SilverResult(int Inserted, int Rejected, int Duplicates,
    int NullStore, int NullProduct, int BadQuantity, int BadPrice, int NullDate);
    public record GoldResult(int Inserted);
}
