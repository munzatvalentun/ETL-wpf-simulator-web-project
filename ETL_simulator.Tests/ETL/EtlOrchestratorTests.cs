using ETL_simulator.ETL;
using ETL_web_project.Data.Context;
using Xunit;
using ETL_web_project.Data.Entities;
using ETL_web_project.Enums;
using Microsoft.EntityFrameworkCore;
using LogLevel = ETL_web_project.Enums.LogLevel;

namespace ETL_simulator.Tests.ETL;

public class EtlOrchestratorTests
{
    private static ProjectContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<ProjectContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new ProjectContext(options);
    }

    [Fact]
    public async Task StartRunAsync_CreatesRunWithRunningStatus()
    {
        using var db = CreateContext(nameof(StartRunAsync_CreatesRunWithRunningStatus));
        var orch = new EtlOrchestrator(db);

        var run = await orch.StartRunAsync(100);

        Assert.NotNull(run);
        Assert.Equal(EtlStatus.Running, run.Status);
        Assert.Equal(100, run.RowsRead);
        Assert.True(run.StartTime <= DateTime.Now);
        Assert.Equal(string.Empty, run.ErrorMessage);
    }

    [Fact]
    public async Task StartRunAsync_RunIsPersisted()
    {
        using var db = CreateContext(nameof(StartRunAsync_RunIsPersisted));
        await new EtlOrchestrator(db).StartRunAsync(50);

        Assert.Equal(1, await db.EtlRuns.CountAsync());
    }

    [Fact]
    public async Task StartRunAsync_CreatesSimulatorJobWhenNoneExists()
    {
        using var db = CreateContext(nameof(StartRunAsync_CreatesSimulatorJobWhenNoneExists));
        await new EtlOrchestrator(db).StartRunAsync(0);

        var job = await db.EtlJobs.SingleOrDefaultAsync(j => j.JobCode == "SIMULATOR");
        Assert.NotNull(job);
        Assert.Equal("ETL Simulator", job.JobName);
        Assert.True(job.IsActive);
    }

    [Fact]
    public async Task StartRunAsync_ReusesExistingSimulatorJob()
    {
        using var db = CreateContext(nameof(StartRunAsync_ReusesExistingSimulatorJob));
        db.EtlJobs.Add(new EtlJob
        {
            JobCode     = "SIMULATOR",
            JobName     = "ETL Simulator",
            Description = "existing",
            IsActive    = true
        });
        await db.SaveChangesAsync();
        var orch = new EtlOrchestrator(db);

        await orch.StartRunAsync(0);
        await orch.StartRunAsync(0);

        Assert.Equal(1, await db.EtlJobs.CountAsync(j => j.JobCode == "SIMULATOR"));
    }

    [Fact]
    public async Task FinishRunAsync_UpdatesStatusAndRowsInserted()
    {
        using var db = CreateContext(nameof(FinishRunAsync_UpdatesStatusAndRowsInserted));
        var orch = new EtlOrchestrator(db);
        var run  = await orch.StartRunAsync(50);

        await orch.FinishRunAsync(run, 45, EtlStatus.Success);

        Assert.Equal(EtlStatus.Success, run.Status);
        Assert.Equal(45, run.RowsInserted);
        Assert.NotNull(run.EndTime);
        Assert.Equal(string.Empty, run.ErrorMessage);
    }

    [Fact]
    public async Task FinishRunAsync_FailedStatus_StoresErrorMessage()
    {
        using var db = CreateContext(nameof(FinishRunAsync_FailedStatus_StoresErrorMessage));
        var orch = new EtlOrchestrator(db);
        var run  = await orch.StartRunAsync(10);

        await orch.FinishRunAsync(run, 0, EtlStatus.Failed, "Connection timeout");

        Assert.Equal(EtlStatus.Failed, run.Status);
        Assert.Equal("Connection timeout", run.ErrorMessage);
        Assert.NotNull(run.EndTime);
    }

    [Fact]
    public async Task FinishRunAsync_RowsUpdatedIsZero()
    {
        using var db = CreateContext(nameof(FinishRunAsync_RowsUpdatedIsZero));
        var orch = new EtlOrchestrator(db);
        var run  = await orch.StartRunAsync(10);

        await orch.FinishRunAsync(run, 10, EtlStatus.Success);

        Assert.Equal(0, run.RowsUpdated);
    }

    [Fact]
    public async Task LogAsync_AddsLogEntry()
    {
        using var db = CreateContext(nameof(LogAsync_AddsLogEntry));
        var orch = new EtlOrchestrator(db);
        var run  = await orch.StartRunAsync(0);

        await orch.LogAsync(run.RunId, LogLevel.Info, "Pipeline started");

        var log = await db.EtlLogs.SingleOrDefaultAsync();
        Assert.NotNull(log);
        Assert.Equal(run.RunId, log.RunId);
        Assert.Equal("Pipeline started", log.Message);
        Assert.Equal(LogLevel.Info, log.Level);
    }

    [Fact]
    public async Task LogAsync_MultipleEntries_AllPersisted()
    {
        using var db = CreateContext(nameof(LogAsync_MultipleEntries_AllPersisted));
        var orch = new EtlOrchestrator(db);
        var run  = await orch.StartRunAsync(0);

        await orch.LogAsync(run.RunId, LogLevel.Info,  "Step 1 done");
        await orch.LogAsync(run.RunId, LogLevel.Warn,  "Step 2 warning");
        await orch.LogAsync(run.RunId, LogLevel.Error, "Step 3 error");

        Assert.Equal(3, await db.EtlLogs.CountAsync());
    }

    [Fact]
    public async Task LogAsync_LogTimeIsSet()
    {
        using var db = CreateContext(nameof(LogAsync_LogTimeIsSet));
        var orch   = new EtlOrchestrator(db);
        var run    = await orch.StartRunAsync(0);
        var before = DateTime.Now.AddSeconds(-1);

        await orch.LogAsync(run.RunId, LogLevel.Info, "msg");

        var log = await db.EtlLogs.FirstAsync();
        Assert.True(log.LogTime >= before);
    }
}
