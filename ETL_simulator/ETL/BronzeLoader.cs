using ETL_web_project.Data.Context;
using ETL_web_project.Data.Entities;

namespace ETL_simulator.ETL
{
    public class BronzeLoader
    {
        private readonly ProjectContext _db;

        public BronzeLoader(ProjectContext db)
        {
            _db = db;
        }

        public async Task<BronzeResult> LoadAsync(IEnumerable<SalesRaw> records)
        {
            var list = records.ToList();
            _db.SalesRaws.AddRange(list);
            await _db.SaveChangesAsync();
            return new BronzeResult(list.Count);
        }
    }
}
