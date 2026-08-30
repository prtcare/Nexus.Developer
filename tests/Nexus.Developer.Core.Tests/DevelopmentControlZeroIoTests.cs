using Nexus.Developer.Core.DevelopmentControl;
using Xunit;

namespace Nexus.Developer.Core.Tests;

// Proves the WI-07-0.2.1 zero-I/O requirement by inspection rather than assertion-by-
// hand: the DevelopmentControl contract layer must compile with no reference to any
// Excel/OpenXML/EF/SQL assembly, because WI-07-0.2.3's Excel adapter and any later SQL
// store must be the ONLY places that pull in persistence/IO libraries. A plain xunit
// fact -- no NetArchTest or other architecture-test package is added just for this.
public class DevelopmentControlZeroIoTests
{
    private static readonly string[] ForbiddenAssemblyPrefixes =
    {
        "DocumentFormat.OpenXml",
        "EPPlus",
        "ClosedXML",
        "NPOI",
        "Microsoft.EntityFrameworkCore",
        "System.Data.SqlClient",
        "Microsoft.Data.SqlClient"
    };

    [Fact]
    public void DevelopmentControlTypes_LiveInTheZeroIoCoreAssembly()
    {
        var storeAssembly = typeof(IDevelopmentControlStore).Assembly;

        var controlTypes = storeAssembly
            .GetTypes()
            .Where(type => type.Namespace == typeof(IDevelopmentControlStore).Namespace)
            .ToArray();

        Assert.NotEmpty(controlTypes);
        Assert.All(controlTypes, type => Assert.Equal(storeAssembly, type.Assembly));
    }

    [Fact]
    public void CoreAssembly_ReferencesNoExcelOpenXmlEfOrSqlAssembly()
    {
        var referenced = typeof(IDevelopmentControlStore).Assembly
            .GetReferencedAssemblies()
            .Where(assembly => assembly.Name is not null)
            .Select(assembly => assembly.Name!)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        var violations = referenced
            .Where(name => ForbiddenAssemblyPrefixes.Any(
                prefix => name.StartsWith(prefix, StringComparison.Ordinal)))
            .ToArray();

        Assert.Empty(violations);
    }
}
