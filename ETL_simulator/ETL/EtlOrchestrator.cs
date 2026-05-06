using ETL_web_project.Data.Context;
using ETL_web_project.Data.Entities;
using ETL_web_project.Enums;
using Microsoft.EntityFrameworkCore;

namespace ETL_simulator.ETL
{
    public class EtlOrchestrator
    {
        private readonly ProjectContext _db;

        public EtlOrchestrator(ProjectContext db)
        {
            _db = db;
        }

        public async Task<EtlRun> StartRunAsync(int rowsRead)
        {
            var job = await EnsureJobAsync();
            var run = new EtlRun
            {
                JobId        = job.JobId,
                Status       = EtlStatus.Running,
                StartTime    = DateTime.Now,
                RowsRead     = rowsRead,
                ErrorMessage = string.Empty
            };
            _db.EtlRuns.Add(run);
            await _db.SaveChangesAsync();
            return run;
        }

        public async Task FinishRunAsync(EtlRun run, int rowsInserted, EtlStatus status, string errorMessage = "")
        {
            run.Status       = status;
            run.EndTime      = DateTime.Now;
            run.RowsInserted = rowsInserted;
            run.RowsUpdated  = 0;
            run.ErrorMessage = errorMessage;
            await _db.SaveChangesAsync();
        }

        public async Task LogAsync(long runId, LogLevel level, string message)
        {
            _db.EtlLogs.Add(new EtlLog
            {
                RunId   = runId,
                Level   = level,
                Message = message,
                LogTime = DateTime.Now
            });
            await _db.SaveChangesAsync();
        }

        private async Task<EtlJob> EnsureJobAsync()
        {
            var job = await _db.EtlJobs.FirstOrDefaultAsync(j => j.JobCode == "SIMULATOR");
            if (job != null) return job;

            job = new EtlJob
            {
                JobName     = "ETL Simulator",
                JobCode     = "SIMULATOR",
                Description = "Автоматична симуляція ETL-пайплайну",
                IsActive    = true
            };
            _db.EtlJobs.Add(job);
            await _db.SaveChangesAsync();
            return job;
        }
    }
}
