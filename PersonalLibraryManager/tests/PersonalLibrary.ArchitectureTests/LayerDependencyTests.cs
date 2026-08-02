using PersonalLibrary.Application.Identity.Commands;
using PersonalLibrary.Domain.Common;
using PersonalLibrary.Infrastructure.Persistence;

namespace PersonalLibrary.ArchitectureTests;

public sealed class LayerDependencyTests
{
    [Fact]
    public void Domain_does_not_reference_outer_layers()
    {
        var references = ReferencedAssemblies(typeof(AuditableEntity).Assembly);

        Assert.DoesNotContain("PersonalLibrary.Application", references);
        Assert.DoesNotContain("PersonalLibrary.Infrastructure", references);
        Assert.DoesNotContain("PersonalLibrary.Api", references);
        Assert.DoesNotContain(references, name => name.StartsWith("Microsoft.EntityFrameworkCore"));
        Assert.DoesNotContain(references, name => name.StartsWith("Microsoft.AspNetCore.Identity"));
    }

    [Fact]
    public void Application_does_not_reference_infrastructure_or_framework_adapters()
    {
        var references = ReferencedAssemblies(typeof(RegisterUserHandler).Assembly);

        Assert.DoesNotContain("PersonalLibrary.Infrastructure", references);
        Assert.DoesNotContain("PersonalLibrary.Api", references);
        Assert.DoesNotContain(references, name => name.StartsWith("Microsoft.EntityFrameworkCore"));
        Assert.DoesNotContain(references, name => name.StartsWith("Microsoft.AspNetCore.Identity"));
    }

    [Fact]
    public void Infrastructure_depends_inward_on_application_and_domain()
    {
        var references = ReferencedAssemblies(typeof(ApplicationDbContext).Assembly);

        Assert.Contains("PersonalLibrary.Application", references);
        Assert.Contains("PersonalLibrary.Domain", references);
        Assert.DoesNotContain("PersonalLibrary.Api", references);
    }

    [Theory]
    [InlineData("PersonalLibrary.Domain.Catalog", "PersonalLibrary.Domain.Libraries")]
    [InlineData("PersonalLibrary.Domain.Catalog", "PersonalLibrary.Domain.Loans")]
    [InlineData("PersonalLibrary.Domain.Libraries", "PersonalLibrary.Domain.Loans")]
    public void Upstream_domain_module_does_not_reference_downstream_module(
        string sourceNamespace,
        string forbiddenNamespace)
    {
        var violations = typeof(AuditableEntity).Assembly.GetTypes()
            .Where(type => type.Namespace?.StartsWith(sourceNamespace) == true)
            .SelectMany(type => type.GetProperties())
            .Where(property => ReferencesNamespace(property.PropertyType, forbiddenNamespace))
            .Select(property => $"{property.DeclaringType!.FullName}.{property.Name}")
            .ToArray();

        Assert.Empty(violations);
    }

    private static bool ReferencesNamespace(Type type, string targetNamespace)
    {
        if (type.Namespace?.StartsWith(targetNamespace) == true)
            return true;

        return type.IsGenericType && type.GetGenericArguments()
            .Any(argument => ReferencesNamespace(argument, targetNamespace));
    }
    private static HashSet<string> ReferencedAssemblies(System.Reflection.Assembly assembly) =>
        assembly.GetReferencedAssemblies()
            .Select(reference => reference.Name!)
            .ToHashSet(StringComparer.Ordinal);
}
