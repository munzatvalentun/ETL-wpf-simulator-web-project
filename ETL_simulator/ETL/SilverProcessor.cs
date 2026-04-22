using ETL_web_project.Data.Context;
using ETL_web_project.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace ETL_simulator.ETL
{
    public class SilverProcessor
    {
        private readonly ProjectContext _db;

        public SilverProcessor(ProjectContext db)
        {
            _db = db;
        }

        public async Task<SilverResult> ProcessAsync()
        {
            var raw = await _db.SalesRaws
                .Where(r => !r.IsProcessedToSilver)
                .ToListAsync();

            if (raw.Count == 0)
                return new SilverResult(0, 0, 0, 0, 0, 0, 0, 0);

            int rejected = 0, duplicates = 0;
            int nullStore = 0, nullProduct = 0, badQty = 0, badPrice = 0, nullDate = 0;

            var toInsert = new List<SalesClean>();
            var seen = new HashSet<(DateTime, string, string)>();

            foreach (var r in raw)
            {
                r.IsProcessedToSilver = true;

                // Validation з підрахунком причин
                if (!r.SalesTime.HasValue)         { nullDate++;    rejected++; continue; }
                if (r.StoreCode == null)            { nullStore++;   rejected++; continue; }
                if (r.ProductCode == null)          { nullProduct++; rejected++; continue; }
                if (!r.Quantity.HasValue || r.Quantity <= 0)
                                                    { badQty++;      rejected++; continue; }
                if (!r.UnitPrice.HasValue || r.UnitPrice <= 0)
                                                    { badPrice++;    rejected++; continue; }

                // Дедублікація в межах батчу
                var key = (r.SalesTime.Value, r.StoreCode, r.ProductCode);
                if (seen.Contains(key))
                {
                    duplicates++; rejected++; continue;
                }

                // Дедублікація проти існуючих silver-записів
                var storeCode   = r.StoreCode;
                var productCode = r.ProductCode;
                var salesTime   = r.SalesTime.Value;
                var exists = await _db.SalesCleans.AnyAsync(s =>
                    s.SalesTime == salesTime &&
                    s.StoreCode == storeCode &&
                    s.ProductCode == productCode);

                if (exists)
                {
                    duplicates++; rejected++; continue;
                }

                seen.Add(key);
                toInsert.Add(new SalesClean
                {
                    SourceId     = r.Id,
                    SalesTime    = r.SalesTime.Value,
                    StoreCode    = r.StoreCode,
                    ProductCode  = r.ProductCode,
                    CustomerCode = r.CustomerCode,
                    Quantity     = r.Quantity.Value,
                    UnitPrice    = r.UnitPrice.Value,
                    TotalAmount  = r.Quantity.Value * r.UnitPrice.Value,
                    CleanedAt    = DateTime.Now
                });
            }

            if (toInsert.Count > 0)
                _db.SalesCleans.AddRange(toInsert);

            await _db.SaveChangesAsync();

            return new SilverResult(toInsert.Count, rejected, duplicates,
                nullStore, nullProduct, badQty, badPrice, nullDate);
        }
    }
}
