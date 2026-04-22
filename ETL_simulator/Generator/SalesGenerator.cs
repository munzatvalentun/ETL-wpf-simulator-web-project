using Bogus;
using ETL_web_project.Data.Entities;

namespace ETL_simulator.Generator
{
    public class SalesGenerator
    {
        public static readonly List<StoreEntry>    Stores;
        public static readonly List<ProductEntry>  Products;
        public static readonly List<CustomerEntry> Customers;

        static SalesGenerator()
        {
            var f = new Faker("en");

            Stores = Enumerable.Range(1, 10).Select(i => new StoreEntry(
                Code:    $"S{i:D3}",
                Name:    f.Company.CompanyName() + " Store",
                City:    f.Address.City(),
                Country: f.Address.Country()
            )).ToList();

            var categories = new[]
            {
                "Electronics", "Furniture", "Clothing", "Food", "Appliances",
                "Stationery", "Sports", "Beauty", "Toys", "Automotive",
                "Garden", "Health", "Books", "Music", "Pets"
            };

            Products = Enumerable.Range(1, 100).Select(i => new ProductEntry(
                Code:     $"P{i:D3}",
                Name:     f.Commerce.ProductName(),
                Category: f.PickRandom(categories),
                Price:    Math.Round(f.Random.Decimal(5, 2000), 2)
            )).ToList();

            Customers = Enumerable.Range(1, 250).Select(i =>
            {
                var person = new Faker().Person;
                return new CustomerEntry(
                    Code:      $"C{i:D4}",
                    FullName:  person.FullName,
                    Gender:    person.Gender.ToString(),
                    BirthDate: person.DateOfBirth,
                    City:      new Faker().Address.City()
                );
            }).ToList();
        }

        private readonly Faker _faker = new("en");
        private readonly GeneratorConfig _config;
        private SalesRaw? _lastGenerated;

        public SalesGenerator(GeneratorConfig config)
        {
            _config = config;
        }

        public SalesRaw Generate()
        {
            if (_lastGenerated != null && _faker.Random.Double() < _config.DuplicateRate)
            {
                return new SalesRaw
                {
                    SalesTime    = _lastGenerated.SalesTime,
                    StoreCode    = _lastGenerated.StoreCode,
                    ProductCode  = _lastGenerated.ProductCode,
                    CustomerCode = _lastGenerated.CustomerCode,
                    Quantity     = _lastGenerated.Quantity,
                    UnitPrice    = _lastGenerated.UnitPrice,
                    LoadedAt     = DateTime.Now
                };
            }

            var store    = _faker.PickRandom(Stores);
            var product  = _faker.PickRandom(Products);
            var customer = _faker.PickRandom(Customers);

            var record = new SalesRaw
            {
                SalesTime   = _faker.Date.Recent(60),
                LoadedAt    = DateTime.Now,

                StoreCode   = _faker.Random.Double() < _config.NullStoreRate
                    ? null : store.Code,

                ProductCode = _faker.Random.Double() < _config.NullProductRate
                    ? null : product.Code,

                CustomerCode = _faker.Random.Double() < 0.15
                    ? null : customer.Code,

                Quantity = _faker.Random.Double() < _config.NegativeQuantityRate
                    ? _faker.Random.Int(-10, -1)
                    : _faker.Random.Int(1, 100),

                UnitPrice = Math.Round(product.Price * _faker.Random.Decimal(0.8m, 1.2m), 2)
            };

            _lastGenerated = record;
            return record;
        }

        public List<SalesRaw> GenerateBatch(int count) =>
            Enumerable.Range(0, count).Select(_ => Generate()).ToList();
    }
}
