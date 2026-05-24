using ETL_simulator.ETL;
using ETL_web_project.Data.Context;
using Xunit;
using ETL_web_project.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace ETL_simulator.Tests.ETL;

public class SilverProcessorTests
{
    private static ProjectContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<ProjectContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new ProjectContext(options);
    }

    private static SalesRaw ValidRaw(int id = 1) => new()
    {
        Id          = id,
        SalesTime   = new DateTime(2024, 6, 15, 10, 0, 0),
        StoreCode   = "S001",
        ProductCode = "P001",
        CustomerCode = "C0001",
        Quantity    = 5,
        UnitPrice   = 99.99m,
        IsProcessedToSilver = false
    };

    [Fact]
    public async Task ProcessAsync_EmptyRaw_ReturnsAllZeros()
    {
        using var db = CreateContext(nameof(ProcessAsync_EmptyRaw_ReturnsAllZeros));
        var result = await new SilverProcessor(db).ProcessAsync();

        Assert.Equal(0, result.Inserted);
        Assert.Equal(0, result.Rejected);
        Assert.Equal(0, result.Duplicates);
    }

    [Fact]
    public async Task ProcessAsync_ValidRecord_InsertsToSilver()
    {
        using var db = CreateContext(nameof(ProcessAsync_ValidRecord_InsertsToSilver));
        db.SalesRaws.Add(ValidRaw());
        await db.SaveChangesAsync();

        var result = await new SilverProcessor(db).ProcessAsync();

        Assert.Equal(1, result.Inserted);
        Assert.Equal(0, result.Rejected);
        Assert.Equal(1, await db.SalesCleans.CountAsync());
    }

    [Fact]
    public async Task ProcessAsync_ValidRecord_SetsIsProcessedToSilver()
    {
        using var db = CreateContext(nameof(ProcessAsync_ValidRecord_SetsIsProcessedToSilver));
        var raw = ValidRaw();
        db.SalesRaws.Add(raw);
        await db.SaveChangesAsync();

        await new SilverProcessor(db).ProcessAsync();

        var updated = await db.SalesRaws.FindAsync(raw.Id);
        Assert.True(updated!.IsProcessedToSilver);
    }

    [Fact]
    public async Task ProcessAsync_ValidRecord_TotalAmountIsCalculatedCorrectly()
    {
        using var db = CreateContext(nameof(ProcessAsync_ValidRecord_TotalAmountIsCalculatedCorrectly));
        var raw = ValidRaw();
        raw.Quantity  = 3;
        raw.UnitPrice = 10.00m;
        db.SalesRaws.Add(raw);
        await db.SaveChangesAsync();

        await new SilverProcessor(db).ProcessAsync();

        var clean = await db.SalesCleans.FirstAsync();
        Assert.Equal(30.00m, clean.TotalAmount);
    }

    [Fact]
    public async Task ProcessAsync_AlreadyProcessedRaw_IsIgnored()
    {
        using var db = CreateContext(nameof(ProcessAsync_AlreadyProcessedRaw_IsIgnored));
        var raw = ValidRaw();
        raw.IsProcessedToSilver = true;
        db.SalesRaws.Add(raw);
        await db.SaveChangesAsync();

        var result = await new SilverProcessor(db).ProcessAsync();

        Assert.Equal(0, result.Inserted);
        Assert.Equal(0, result.Rejected);
    }

    [Fact]
    public async Task ProcessAsync_NullDate_RejectsWithCorrectReason()
    {
        using var db = CreateContext(nameof(ProcessAsync_NullDate_RejectsWithCorrectReason));
        var raw = ValidRaw();
        raw.SalesTime = null;
        db.SalesRaws.Add(raw);
        await db.SaveChangesAsync();

        var result = await new SilverProcessor(db).ProcessAsync();

        Assert.Equal(0, result.Inserted);
        Assert.Equal(1, result.Rejected);
        Assert.Equal(1, result.NullDate);
        Assert.Equal("Відсутня дата", result.RejectedReasons[raw.Id]);
    }

    [Fact]
    public async Task ProcessAsync_NullStoreCode_RejectsWithCorrectReason()
    {
        using var db = CreateContext(nameof(ProcessAsync_NullStoreCode_RejectsWithCorrectReason));
        var raw = ValidRaw();
        raw.StoreCode = null;
        db.SalesRaws.Add(raw);
        await db.SaveChangesAsync();

        var result = await new SilverProcessor(db).ProcessAsync();

        Assert.Equal(1, result.Rejected);
        Assert.Equal(1, result.NullStore);
        Assert.Equal("Null StoreCode", result.RejectedReasons[raw.Id]);
    }

    [Fact]
    public async Task ProcessAsync_NullProductCode_RejectsWithCorrectReason()
    {
        using var db = CreateContext(nameof(ProcessAsync_NullProductCode_RejectsWithCorrectReason));
        var raw = ValidRaw();
        raw.ProductCode = null;
        db.SalesRaws.Add(raw);
        await db.SaveChangesAsync();

        var result = await new SilverProcessor(db).ProcessAsync();

        Assert.Equal(1, result.Rejected);
        Assert.Equal(1, result.NullProduct);
        Assert.Equal("Null ProductCode", result.RejectedReasons[raw.Id]);
    }

    [Fact]
    public async Task ProcessAsync_ZeroQuantity_Rejects()
    {
        using var db = CreateContext(nameof(ProcessAsync_ZeroQuantity_Rejects));
        var raw = ValidRaw();
        raw.Quantity = 0;
        db.SalesRaws.Add(raw);
        await db.SaveChangesAsync();

        var result = await new SilverProcessor(db).ProcessAsync();

        Assert.Equal(1, result.Rejected);
        Assert.Equal(1, result.BadQuantity);
        Assert.Equal("Кількість ≤ 0", result.RejectedReasons[raw.Id]);
    }

    [Fact]
    public async Task ProcessAsync_NegativeQuantity_Rejects()
    {
        using var db = CreateContext(nameof(ProcessAsync_NegativeQuantity_Rejects));
        var raw = ValidRaw();
        raw.Quantity = -3;
        db.SalesRaws.Add(raw);
        await db.SaveChangesAsync();

        var result = await new SilverProcessor(db).ProcessAsync();

        Assert.Equal(1, result.Rejected);
        Assert.Equal(1, result.BadQuantity);
    }

    [Fact]
    public async Task ProcessAsync_NullQuantity_Rejects()
    {
        using var db = CreateContext(nameof(ProcessAsync_NullQuantity_Rejects));
        var raw = ValidRaw();
        raw.Quantity = null;
        db.SalesRaws.Add(raw);
        await db.SaveChangesAsync();

        var result = await new SilverProcessor(db).ProcessAsync();

        Assert.Equal(1, result.Rejected);
        Assert.Equal(1, result.BadQuantity);
    }

    [Fact]
    public async Task ProcessAsync_ZeroPrice_Rejects()
    {
        using var db = CreateContext(nameof(ProcessAsync_ZeroPrice_Rejects));
        var raw = ValidRaw();
        raw.UnitPrice = 0m;
        db.SalesRaws.Add(raw);
        await db.SaveChangesAsync();

        var result = await new SilverProcessor(db).ProcessAsync();

        Assert.Equal(1, result.Rejected);
        Assert.Equal(1, result.BadPrice);
        Assert.Equal("Ціна ≤ 0", result.RejectedReasons[raw.Id]);
    }

    [Fact]
    public async Task ProcessAsync_NegativePrice_Rejects()
    {
        using var db = CreateContext(nameof(ProcessAsync_NegativePrice_Rejects));
        var raw = ValidRaw();
        raw.UnitPrice = -5m;
        db.SalesRaws.Add(raw);
        await db.SaveChangesAsync();

        var result = await new SilverProcessor(db).ProcessAsync();

        Assert.Equal(1, result.Rejected);
        Assert.Equal(1, result.BadPrice);
    }

    [Fact]
    public async Task ProcessAsync_NullPrice_Rejects()
    {
        using var db = CreateContext(nameof(ProcessAsync_NullPrice_Rejects));
        var raw = ValidRaw();
        raw.UnitPrice = null;
        db.SalesRaws.Add(raw);
        await db.SaveChangesAsync();

        var result = await new SilverProcessor(db).ProcessAsync();

        Assert.Equal(1, result.Rejected);
        Assert.Equal(1, result.BadPrice);
    }

    [Fact]
    public async Task ProcessAsync_InBatchDuplicate_OnlyFirstInserted()
    {
        using var db = CreateContext(nameof(ProcessAsync_InBatchDuplicate_OnlyFirstInserted));
        var r1 = ValidRaw(1);
        var r2 = ValidRaw(2);
        r2.SalesTime    = r1.SalesTime;
        r2.StoreCode    = r1.StoreCode;
        r2.ProductCode  = r1.ProductCode;
        db.SalesRaws.AddRange(r1, r2);
        await db.SaveChangesAsync();

        var result = await new SilverProcessor(db).ProcessAsync();

        Assert.Equal(1, result.Inserted);
        Assert.Equal(1, result.Duplicates);
        Assert.Equal(1, result.Rejected);
        Assert.Equal("Дублікат (батч)", result.RejectedReasons[2]);
    }

    [Fact]
    public async Task ProcessAsync_DbDuplicate_RejectsWithDbReason()
    {
        using var db = CreateContext(nameof(ProcessAsync_DbDuplicate_RejectsWithDbReason));
        var salesTime = new DateTime(2024, 6, 15, 10, 0, 0);

        db.SalesCleans.Add(new SalesClean
        {
            SalesTime   = salesTime,
            StoreCode   = "S001",
            ProductCode = "P001",
            Quantity    = 5,
            UnitPrice   = 99.99m,
            TotalAmount = 499.95m,
            CleanedAt   = DateTime.Now
        });
        var raw = ValidRaw();
        raw.SalesTime = salesTime;
        db.SalesRaws.Add(raw);
        await db.SaveChangesAsync();

        var result = await new SilverProcessor(db).ProcessAsync();

        Assert.Equal(0, result.Inserted);
        Assert.Equal(1, result.Duplicates);
        Assert.Equal("Дублікат (БД)", result.RejectedReasons[raw.Id]);
    }

    [Fact]
    public async Task ProcessAsync_MultipleErrors_CountsEachSeparately()
    {
        using var db = CreateContext(nameof(ProcessAsync_MultipleErrors_CountsEachSeparately));
        db.SalesRaws.AddRange(
            new SalesRaw { Id = 1, SalesTime = null,   StoreCode = "S001", ProductCode = "P001", Quantity = 1, UnitPrice = 10m },
            new SalesRaw { Id = 2, SalesTime = DateTime.Now, StoreCode = null,   ProductCode = "P001", Quantity = 1, UnitPrice = 10m },
            new SalesRaw { Id = 3, SalesTime = DateTime.Now, StoreCode = "S001", ProductCode = null,   Quantity = 1, UnitPrice = 10m },
            new SalesRaw { Id = 4, SalesTime = DateTime.Now, StoreCode = "S002", ProductCode = "P002", Quantity = 0, UnitPrice = 10m },
            new SalesRaw { Id = 5, SalesTime = DateTime.Now, StoreCode = "S003", ProductCode = "P003", Quantity = 1, UnitPrice = 0m }
        );
        await db.SaveChangesAsync();

        var result = await new SilverProcessor(db).ProcessAsync();

        Assert.Equal(0, result.Inserted);
        Assert.Equal(5, result.Rejected);
        Assert.Equal(1, result.NullDate);
        Assert.Equal(1, result.NullStore);
        Assert.Equal(1, result.NullProduct);
        Assert.Equal(1, result.BadQuantity);
        Assert.Equal(1, result.BadPrice);
    }
}
