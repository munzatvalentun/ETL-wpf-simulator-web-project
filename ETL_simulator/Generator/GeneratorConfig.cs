namespace ETL_simulator.Generator
{
    public class GeneratorConfig
    {
        public int BatchSize { get; set; } = 10;
        public double NullStoreRate { get; set; } = 0.07;
        public double NullProductRate { get; set; } = 0.07;
        public double NegativeQuantityRate { get; set; } = 0.05;
        public double DuplicateRate { get; set; } = 0.05;
    }
}
