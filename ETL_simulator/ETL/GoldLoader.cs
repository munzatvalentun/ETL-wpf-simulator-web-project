using ETL_simulator.Generator;
using ETL_web_project.Data.Context;
using ETL_web_project.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace ETL_simulator.ETL
{
    public class GoldLoader
    {
        private readonly ProjectContext _db;

        public GoldLoader(ProjectContext db)
        {
            _db = db;
        }

        public async Task SeedDimensionsAsync()
        {
            var existingStores    = await _db.DimStores.Select(x => x.StoreCode).ToListAsync();
            var existingProducts  = await _db.DimProducts.Select(x => x.ProductCode).ToListAsync();
            var existingCustomers = await _db.DimCustomers.Select(x => x.CustomerCode).ToListAsync();

            foreach (var s in SalesGenerator.Stores.Where(s => !existingStores.Contains(s.Code)))
            {
                _db.DimStores.Add(new DimStore
                {
                    StoreCode = s.Code, StoreName = s.Name,
                    City = s.City, Country = s.Country, IsActive = true
                });
            }

            foreach (var p in SalesGenerator.Products.Where(p => !existingProducts.Contains(p.Code)))
            {
                _db.DimProducts.Add(new DimProduct
                {
                    ProductCode = p.Code, ProductName = p.Name,
                    Category = p.Category, UnitPrice = p.Price, IsActive = true
                });
            }

            foreach (var c in SalesGenerator.Customers.Where(c => !existingCustomers.Contains(c.Code)))
            {
                _db.DimCustomers.Add(new DimCustomer
                {
                    CustomerCode = c.Code, FullName = c.FullName,
                    Gender = c.Gender, BirthDate = c.BirthDate,
                    City = c.City
                });
            }

            await _db.SaveChangesAsync();
        }

        public async Task<GoldResult> LoadAsync()
        {
            var silver = await _db.SalesCleans
                .Where(s => !s.IsProcessedToGold)
                .ToListAsync();

            if (silver.Count == 0)
                return new GoldResult(0);

            int inserted = 0;

            foreach (var s in silver)
            {
                var store   = await _db.DimStores.FirstOrDefaultAsync(x => x.StoreCode == s.StoreCode);
                var product = await _db.DimProducts.FirstOrDefaultAsync(x => x.ProductCode == s.ProductCode);

                if (store == null || product == null)
                {
                    s.IsProcessedToGold = true;
                    continue;
                }

                var dateKey = await GetOrCreateDateKeyAsync(s.SalesTime.Date);

                int? customerKey = null;
                if (s.CustomerCode != null)
                {
                    var customer = await _db.DimCustomers
                        .FirstOrDefaultAsync(x => x.CustomerCode == s.CustomerCode);
                    customerKey = customer?.CustomerKey;
                }

                _db.FactSales.Add(new FactSales
                {
                    DateKey     = dateKey,
                    StoreKey    = store.StoreKey,
                    ProductKey  = product.ProductKey,
                    CustomerKey = customerKey,
                    Quantity    = s.Quantity,
                    TotalAmount = s.TotalAmount,
                    CreatedAt   = DateTime.Now
                });

                s.IsProcessedToGold = true;
                inserted++;
            }

            await _db.SaveChangesAsync();
            return new GoldResult(inserted);
        }

        private async Task<int> GetOrCreateDateKeyAsync(DateTime date)
        {
            var existing = await _db.DimDates.FirstOrDefaultAsync(d => d.Date == date);
            if (existing != null)
                return existing.DateKey;

            var dimDate = new DimDate
            {
                Date      = date,
                Year      = date.Year,
                Month     = (byte)date.Month,
                Day       = (byte)date.Day,
                MonthName = date.ToString("MMMM"),
                DayOfWeek = (byte)date.DayOfWeek,
                DayName   = date.DayOfWeek.ToString()
            };

            _db.DimDates.Add(dimDate);
            await _db.SaveChangesAsync();
            return dimDate.DateKey;
        }
    }
}
