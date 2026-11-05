using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using WeighingMS.Application.Batches;
using WeighingMS.Domain.Common;
using WeighingMS.Infrastructure.Batches;

namespace WeighingMS.Api.IntegrationTests;

/// <summary>The TOL fallback catalogue: lookup, no universal default, reload without restart, start-up validation.</summary>
public sealed class ToleranceCatalogueTests(ApiFixture fixture) : IAsyncLifetime
{
    public async ValueTask InitializeAsync() => await fixture.ResetAsync();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private static (IConfigurationRoot Config, Microsoft.Extensions.Configuration.Memory.MemoryConfigurationProvider Source, IToleranceCatalogue Catalogue) Build()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Tolerances:Entries:0:ChemicalId"] = "AUX-0204",
            ["Tolerances:Entries:0:ScaleClass"] = "precision",
            ["Tolerances:Entries:0:PlusMinus"] = "0.009",
            ["Tolerances:Entries:0:Unit"] = "kg",
        }).Build();
        var services = new ServiceCollection();
        services.AddOptions<ToleranceSettings>().Bind(config.GetSection(ToleranceSettings.SectionName));
        services.AddSingleton<IToleranceCatalogue, ToleranceCatalogue>();
        var provider = services.BuildServiceProvider();
        var source = config.Providers.OfType<Microsoft.Extensions.Configuration.Memory.MemoryConfigurationProvider>().Single();
        return (config, source, provider.GetRequiredService<IToleranceCatalogue>());
    }

    [Fact]
    public void Find_ConfiguredChemicalAndScaleClass_ReturnsTheTolerance()
    {
        var (_, _, catalogue) = Build();

        catalogue.Find("AUX-0204", "precision").ShouldBe(Quantity.Create(0.009m, "kg").Value);
    }

    [Theory]
    [InlineData("AUX-0204", "heavyDuty")]
    [InlineData("DYE-9999", "precision")]
    public void Find_NoEntry_ReturnsNullNotADefault(string chemical, string scaleClass)
    {
        var (_, _, catalogue) = Build();

        catalogue.Find(chemical, scaleClass).ShouldBeNull();
    }

    [Fact]
    public void Find_AfterConfigurationChange_UsesTheNewValueWithoutRestart()
    {
        var (config, source, catalogue) = Build();

        source.Set("Tolerances:Entries:0:PlusMinus", "0.012");
        config.Reload();

        catalogue.Find("AUX-0204", "precision").ShouldBe(Quantity.Create(0.012m, "kg").Value);
    }

    [Fact]
    public void Api_WithAnInvalidToleranceEntry_FailsAtStartUp()
    {
        using var api = fixture.CreateApi(new Dictionary<string, string?>
        {
            ["Tolerances:Entries:0:ChemicalId"] = "DYE-4471",
            ["Tolerances:Entries:0:ScaleClass"] = "precision",
            ["Tolerances:Entries:0:PlusMinus"] = "0",
            ["Tolerances:Entries:0:Unit"] = "g",
        });

        var startUp = Should.Throw<OptionsValidationException>(() => api.CreateClient());
        startUp.Message.ShouldContain("Tolerances");
    }

    [Fact]
    public void Api_WithValidToleranceEntries_Starts()
    {
        using var api = fixture.CreateApi(new Dictionary<string, string?>
        {
            ["Tolerances:Entries:0:ChemicalId"] = "AUX-0101",
            ["Tolerances:Entries:0:ScaleClass"] = "heavyDuty",
            ["Tolerances:Entries:0:PlusMinus"] = "0.120",
            ["Tolerances:Entries:0:Unit"] = "kg",
        });

        using var scope = api.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<IToleranceCatalogue>().Find("AUX-0101", "heavyDuty")!.Value.ShouldBe(0.120m);
    }
}
