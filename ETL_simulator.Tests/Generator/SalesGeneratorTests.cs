using ETL_simulator.Generator;
using Xunit;

namespace ETL_simulator.Tests.Generator;

public class SalesGeneratorTests
{
    // --- Static collections ---

    [Fact]
    public void Stores_ContainsExactlyTenEntries()
    {
        Assert.Equal(10, SalesGenerator.Stores.Count);
    }

    [Fact]
    public void Products_ContainsExactlyOneHundredEntries()
    {
        Assert.Equal(100, SalesGenerator.Products.Count);
    }

    [Fact]
    public void Customers_ContainsExactlyTwoHundredFiftyEntries()
    {
        Assert.Equal(250, SalesGenerator.Customers.Count);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    public void Stores_CodesFollowSNNNPattern(int n)
    {
        Assert.Contains(SalesGenerator.Stores, s => s.Code == $"S{n:D3}");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(50)]
    [InlineData(100)]
    public void Products_CodesFollowPNNNPattern(int n)
    {
        Assert.Contains(SalesGenerator.Products, p => p.Code == $"P{n:D3}");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(125)]
    [InlineData(250)]
    public void Customers_CodesFollowCNNNNPattern(int n)
    {
        Assert.Contains(SalesGenerator.Customers, c => c.Code == $"C{n:D4}");
    }

    [Fact]
    public void Stores_AllCodesAreUnique()
    {
        var codes = SalesGenerator.Stores.Select(s => s.Code).ToList();
        Assert.Equal(codes.Count, codes.Distinct().Count());
    }

    [Fact]
    public void Products_AllCodesAreUnique()
    {
        var codes = SalesGenerator.Products.Select(p => p.Code).ToList();
        Assert.Equal(codes.Count, codes.Distinct().Count());
    }

    // --- Generate() ---

    [Fact]
    public void Generate_ReturnsNonNullRecord()
    {
        var gen = new SalesGenerator(new GeneratorConfig());
        Assert.NotNull(gen.Generate());
    }

    [Fact]
    public void Generate_LoadedAtIsSetToCurrentTime()
    {
        var before = DateTime.Now.AddSeconds(-1);
        var gen    = new SalesGenerator(new GeneratorConfig());

        var record = gen.Generate();

        Assert.True(record.LoadedAt >= before);
        Assert.True(record.LoadedAt <= DateTime.Now.AddSeconds(1));
    }

    [Fact]
    public void Generate_NullStoreRateOne_AlwaysProducesNullStoreCode()
    {
        var gen = new SalesGenerator(new GeneratorConfig { NullStoreRate = 1.0 });

        for (int i = 0; i < 30; i++)
            Assert.Null(gen.Generate().StoreCode);
    }

    [Fact]
    public void Generate_NullStoreRateZero_NeverProducesNullStoreCode()
    {
        var gen = new SalesGenerator(new GeneratorConfig
        {
            NullStoreRate   = 0.0,
            NullProductRate = 0.0,
            DuplicateRate   = 0.0
        });

        for (int i = 0; i < 50; i++)
            Assert.NotNull(gen.Generate().StoreCode);
    }

    [Fact]
    public void Generate_NullProductRateOne_AlwaysProducesNullProductCode()
    {
        var gen = new SalesGenerator(new GeneratorConfig { NullProductRate = 1.0 });

        for (int i = 0; i < 30; i++)
            Assert.Null(gen.Generate().ProductCode);
    }

    [Fact]
    public void Generate_NullProductRateZero_NeverProducesNullProductCode()
    {
        var gen = new SalesGenerator(new GeneratorConfig
        {
            NullStoreRate   = 0.0,
            NullProductRate = 0.0,
            DuplicateRate   = 0.0
        });

        for (int i = 0; i < 50; i++)
            Assert.NotNull(gen.Generate().ProductCode);
    }

    [Fact]
    public void Generate_NegativeQuantityRateOne_AlwaysProducesNegativeQuantity()
    {
        var gen = new SalesGenerator(new GeneratorConfig { NegativeQuantityRate = 1.0 });

        for (int i = 0; i < 30; i++)
            Assert.True(gen.Generate().Quantity < 0);
    }

    [Fact]
    public void Generate_NegativeQuantityRateZero_AlwaysProducesPositiveQuantity()
    {
        var gen = new SalesGenerator(new GeneratorConfig
        {
            NegativeQuantityRate = 0.0,
            DuplicateRate        = 0.0
        });

        for (int i = 0; i < 50; i++)
            Assert.True(gen.Generate().Quantity > 0);
    }

    [Fact]
    public void Generate_DuplicateRateOne_SecondRecordMatchesFirst()
    {
        var gen = new SalesGenerator(new GeneratorConfig
        {
            DuplicateRate   = 1.0,
            NullStoreRate   = 0.0,
            NullProductRate = 0.0
        });

        var first  = gen.Generate();
        var second = gen.Generate();

        Assert.Equal(first.SalesTime,    second.SalesTime);
        Assert.Equal(first.StoreCode,    second.StoreCode);
        Assert.Equal(first.ProductCode,  second.ProductCode);
        Assert.Equal(first.Quantity,     second.Quantity);
        Assert.Equal(first.UnitPrice,    second.UnitPrice);
        Assert.Equal(first.CustomerCode, second.CustomerCode);
    }

    [Fact]
    public void Generate_DuplicateRateZero_FirstCallNeverDuplicates()
    {
        var gen = new SalesGenerator(new GeneratorConfig { DuplicateRate = 0.0 });

        // first call: _lastGenerated is null, so no duplicate can be generated
        var record = gen.Generate();
        Assert.NotNull(record);
    }

    [Fact]
    public void Generate_UnitPrice_IsPositiveWhenProductCodeNotNull()
    {
        var gen = new SalesGenerator(new GeneratorConfig
        {
            NullProductRate = 0.0,
            DuplicateRate   = 0.0
        });

        for (int i = 0; i < 30; i++)
        {
            var r = gen.Generate();
            if (r.UnitPrice.HasValue)
                Assert.True(r.UnitPrice > 0);
        }
    }

    // --- GenerateBatch() ---

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(100)]
    public void GenerateBatch_ReturnsExactRequestedCount(int count)
    {
        var gen   = new SalesGenerator(new GeneratorConfig());
        var batch = gen.GenerateBatch(count);

        Assert.Equal(count, batch.Count);
    }

    [Fact]
    public void GenerateBatch_ZeroCount_ReturnsEmptyList()
    {
        var gen   = new SalesGenerator(new GeneratorConfig());
        var batch = gen.GenerateBatch(0);

        Assert.Empty(batch);
    }

    [Fact]
    public void GenerateBatch_AllRecordsAreNonNull()
    {
        var gen   = new SalesGenerator(new GeneratorConfig());
        var batch = gen.GenerateBatch(20);

        Assert.All(batch, r => Assert.NotNull(r));
    }
}
