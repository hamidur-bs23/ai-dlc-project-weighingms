using System.Reflection;
using NetArchTest.Rules;
using WeighingMS.Application;
using WeighingMS.Domain;
using WeighingMS.Infrastructure;

namespace WeighingMS.Architecture.Tests;

/// <summary>Clean-architecture dependency rule (system-architecture: Api → Application → Domain).</summary>
public sealed class LayerDependencyTests
{
    private static readonly Assembly _domain = typeof(IDomainAssemblyMarker).Assembly;
    private static readonly Assembly _application = typeof(IApplicationAssemblyMarker).Assembly;
    private static readonly Assembly _infrastructure = typeof(IInfrastructureAssemblyMarker).Assembly;
    private static readonly Assembly _api = typeof(Program).Assembly;

    [Theory]
    [InlineData("WeighingMS.Application")]
    [InlineData("WeighingMS.Infrastructure")]
    [InlineData("WeighingMS.Api")]
    [InlineData("Microsoft.AspNetCore")]
    [InlineData("Microsoft.EntityFrameworkCore")]
    public void Domain_DoesNotDependOn(string forbidden)
    {
        Types.InAssembly(_domain).ShouldNot().HaveDependencyOn(forbidden).GetResult().IsSuccessful.ShouldBeTrue();
        _domain.GetReferencedAssemblies().Select(a => a.Name).ShouldNotContain(n => n != null && n.StartsWith(forbidden, StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("WeighingMS.Infrastructure")]
    [InlineData("WeighingMS.Api")]
    public void Application_DoesNotDependOn(string forbidden)
    {
        Types.InAssembly(_application).ShouldNot().HaveDependencyOn(forbidden).GetResult().IsSuccessful.ShouldBeTrue();
        _application.GetReferencedAssemblies().Select(a => a.Name).ShouldNotContain(forbidden);
    }

    [Fact]
    public void Infrastructure_DoesNotDependOnApi()
    {
        _infrastructure.GetReferencedAssemblies().Select(a => a.Name).ShouldNotContain("WeighingMS.Api");
    }

    [Fact]
    public void BusinessAssemblies_DoNotReferenceTheSimulatorHost()
    {
        foreach (var assembly in new[] { _domain, _application, _infrastructure, _api })
        {
            assembly.GetReferencedAssemblies().Select(a => a.Name).ShouldNotContain("WeighingMS.Simulators.Host");
        }
    }
}
