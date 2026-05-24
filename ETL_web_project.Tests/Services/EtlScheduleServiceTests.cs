using ETL_web_project.Data.Context;
using ETL_web_project.Data.Entities;
using Xunit;
using ETL_web_project.Enums;
using ETL_web_project.Services;
using Microsoft.EntityFrameworkCore;

namespace ETL_web_project.Tests.Services;

public class EtlScheduleServiceTests
{
    private static ProjectContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<ProjectContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new ProjectContext(options);
    }

    private static EtlJob SeedJob(ProjectContext db, string code = "TEST")
    {
        var job = new EtlJob { JobName = "Test Job", JobCode = code, Description = "desc", IsActive = true };
        db.EtlJobs.Add(job);
        db.SaveChanges();
        return job;
    }

    private static EtlSchedule MakeSchedule(int jobId, string freq = "Daily", bool active = true)
        => new() { JobId = jobId, FrequencyText = freq, IsActive = active };

    // --- CreateAsync ---

    [Fact]
    public async Task CreateAsync_AddsScheduleToDatabase()
    {
        using var db = CreateContext(nameof(CreateAsync_AddsScheduleToDatabase));
        var job     = SeedJob(db);
        var service = new EtlScheduleService(db);

        await service.CreateAsync(MakeSchedule(job.JobId, "Daily at 08:00"));

        Assert.Equal(1, await db.EtlSchedules.CountAsync());
    }

    [Fact]
    public async Task CreateAsync_ReturnsCreatedScheduleWithId()
    {
        using var db = CreateContext(nameof(CreateAsync_ReturnsCreatedScheduleWithId));
        var job     = SeedJob(db);
        var service = new EtlScheduleService(db);

        var result = await service.CreateAsync(MakeSchedule(job.JobId, "Weekly"));

        Assert.NotNull(result);
        Assert.Equal("Weekly", result.FrequencyText);
        Assert.True(result.ScheduleId > 0);
    }

    // --- GetByIdAsync ---

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsSchedule()
    {
        using var db = CreateContext(nameof(GetByIdAsync_ExistingId_ReturnsSchedule));
        var job      = SeedJob(db);
        var schedule = MakeSchedule(job.JobId, "Monthly");
        db.EtlSchedules.Add(schedule);
        await db.SaveChangesAsync();
        var service = new EtlScheduleService(db);

        var result = await service.GetByIdAsync(schedule.ScheduleId);

        Assert.NotNull(result);
        Assert.Equal("Monthly", result.FrequencyText);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ReturnsNull()
    {
        using var db = CreateContext(nameof(GetByIdAsync_NonExistingId_ReturnsNull));
        var service  = new EtlScheduleService(db);

        var result = await service.GetByIdAsync(9999);

        Assert.Null(result);
    }

    // --- GetAllAsync ---

    [Fact]
    public async Task GetAllAsync_ReturnsAllSchedules()
    {
        using var db = CreateContext(nameof(GetAllAsync_ReturnsAllSchedules));
        var job = SeedJob(db);
        db.EtlSchedules.AddRange(
            MakeSchedule(job.JobId, "Daily"),
            MakeSchedule(job.JobId, "Weekly"),
            MakeSchedule(job.JobId, "Monthly", false)
        );
        await db.SaveChangesAsync();
        var service = new EtlScheduleService(db);

        var result = await service.GetAllAsync();

        Assert.Equal(3, result.Count);
    }

    [Fact]
    public async Task GetAllAsync_EmptyDb_ReturnsEmptyList()
    {
        using var db = CreateContext(nameof(GetAllAsync_EmptyDb_ReturnsEmptyList));
        var service  = new EtlScheduleService(db);

        var result = await service.GetAllAsync();

        Assert.Empty(result);
    }

    // --- UpdateAsync ---

    [Fact]
    public async Task UpdateAsync_ChangesFrequencyText()
    {
        using var db = CreateContext(nameof(UpdateAsync_ChangesFrequencyText));
        var job      = SeedJob(db);
        var schedule = MakeSchedule(job.JobId, "Daily");
        db.EtlSchedules.Add(schedule);
        await db.SaveChangesAsync();
        var service = new EtlScheduleService(db);

        schedule.FrequencyText = "Hourly";
        var result = await service.UpdateAsync(schedule);

        Assert.True(result);
        var updated = await db.EtlSchedules.FindAsync(schedule.ScheduleId);
        Assert.Equal("Hourly", updated!.FrequencyText);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsTrue_WhenRecordSaved()
    {
        using var db = CreateContext(nameof(UpdateAsync_ReturnsTrue_WhenRecordSaved));
        var job      = SeedJob(db);
        var schedule = MakeSchedule(job.JobId);
        db.EtlSchedules.Add(schedule);
        await db.SaveChangesAsync();
        var service = new EtlScheduleService(db);

        schedule.IsActive = false;
        var result = await service.UpdateAsync(schedule);

        Assert.True(result);
    }

    // --- DeleteAsync ---

    [Fact]
    public async Task DeleteAsync_ExistingId_RemovesSchedule()
    {
        using var db = CreateContext(nameof(DeleteAsync_ExistingId_RemovesSchedule));
        var job      = SeedJob(db);
        var schedule = MakeSchedule(job.JobId);
        db.EtlSchedules.Add(schedule);
        await db.SaveChangesAsync();
        var service = new EtlScheduleService(db);

        var result = await service.DeleteAsync(schedule.ScheduleId);

        Assert.True(result);
        Assert.Equal(0, await db.EtlSchedules.CountAsync());
    }

    [Fact]
    public async Task DeleteAsync_NonExistingId_ReturnsFalse()
    {
        using var db = CreateContext(nameof(DeleteAsync_NonExistingId_ReturnsFalse));
        var service  = new EtlScheduleService(db);

        var result = await service.DeleteAsync(9999);

        Assert.False(result);
    }

    // --- ToggleActiveAsync ---

    [Fact]
    public async Task ToggleActiveAsync_ActiveSchedule_SetsIsActiveFalse()
    {
        using var db = CreateContext(nameof(ToggleActiveAsync_ActiveSchedule_SetsIsActiveFalse));
        var job      = SeedJob(db);
        var schedule = MakeSchedule(job.JobId, active: true);
        db.EtlSchedules.Add(schedule);
        await db.SaveChangesAsync();
        var service = new EtlScheduleService(db);

        var result = await service.ToggleActiveAsync(schedule.ScheduleId);

        Assert.True(result);
        var updated = await db.EtlSchedules.FindAsync(schedule.ScheduleId);
        Assert.False(updated!.IsActive);
    }

    [Fact]
    public async Task ToggleActiveAsync_InactiveSchedule_SetsIsActiveTrue()
    {
        using var db = CreateContext(nameof(ToggleActiveAsync_InactiveSchedule_SetsIsActiveTrue));
        var job      = SeedJob(db);
        var schedule = MakeSchedule(job.JobId, active: false);
        db.EtlSchedules.Add(schedule);
        await db.SaveChangesAsync();
        var service = new EtlScheduleService(db);

        await service.ToggleActiveAsync(schedule.ScheduleId);

        var updated = await db.EtlSchedules.FindAsync(schedule.ScheduleId);
        Assert.True(updated!.IsActive);
    }

    [Fact]
    public async Task ToggleActiveAsync_DoubleToggle_RestoresOriginalState()
    {
        using var db = CreateContext(nameof(ToggleActiveAsync_DoubleToggle_RestoresOriginalState));
        var job      = SeedJob(db);
        var schedule = MakeSchedule(job.JobId, active: true);
        db.EtlSchedules.Add(schedule);
        await db.SaveChangesAsync();
        var service = new EtlScheduleService(db);

        await service.ToggleActiveAsync(schedule.ScheduleId);
        await service.ToggleActiveAsync(schedule.ScheduleId);

        var updated = await db.EtlSchedules.FindAsync(schedule.ScheduleId);
        Assert.True(updated!.IsActive);
    }

    [Fact]
    public async Task ToggleActiveAsync_NonExistingId_ReturnsFalse()
    {
        using var db = CreateContext(nameof(ToggleActiveAsync_NonExistingId_ReturnsFalse));
        var service  = new EtlScheduleService(db);

        var result = await service.ToggleActiveAsync(9999);

        Assert.False(result);
    }

    // --- RunNowAsync ---

    [Fact]
    public async Task RunNowAsync_ExistingSchedule_CreatesRunWithSuccessStatus()
    {
        using var db = CreateContext(nameof(RunNowAsync_ExistingSchedule_CreatesRunWithSuccessStatus));
        var job      = SeedJob(db);
        var schedule = MakeSchedule(job.JobId);
        db.EtlSchedules.Add(schedule);
        await db.SaveChangesAsync();
        var service = new EtlScheduleService(db);

        var run = await service.RunNowAsync(schedule.ScheduleId);

        Assert.NotNull(run);
        Assert.Equal(EtlStatus.Success, run.Status);
        Assert.NotNull(run.EndTime);
        Assert.Equal(job.JobId, run.JobId);
    }

    [Fact]
    public async Task RunNowAsync_ExistingSchedule_RunIsPersisted()
    {
        using var db = CreateContext(nameof(RunNowAsync_ExistingSchedule_RunIsPersisted));
        var job      = SeedJob(db);
        var schedule = MakeSchedule(job.JobId);
        db.EtlSchedules.Add(schedule);
        await db.SaveChangesAsync();
        var service = new EtlScheduleService(db);

        await service.RunNowAsync(schedule.ScheduleId);

        Assert.Equal(1, await db.EtlRuns.CountAsync());
    }

    [Fact]
    public async Task RunNowAsync_NonExistingSchedule_ThrowsException()
    {
        using var db = CreateContext(nameof(RunNowAsync_NonExistingSchedule_ThrowsException));
        var service  = new EtlScheduleService(db);

        await Assert.ThrowsAsync<Exception>(() => service.RunNowAsync(9999));
    }
}
