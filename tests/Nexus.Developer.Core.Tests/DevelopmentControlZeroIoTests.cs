using Nexus.Developer.Core.DevelopmentControl;
using Xunit;

namespace Nexus.Developer.Core.Tests;

// Proves the WI-07-0.2.1 zero-I/O requirement by inspection rather than assertion-by-hand:
// the DevelopmentControl contract layer must compile with no reference to any
// Excel/OpenXML/EF/SQL assembly, because WI-07-0.2.3's Excel adapter and any later SQL
// store must be the ONLY places that pull in persistence/IO libraries.
//
// SP1-WAVE-04 Lane A moved the pure zero-IO DevelopmentControl value contracts into the
// bootstrap-safe shared assembly Nexus.DevelopmentControl.Contracts (physical extraction,
// namespaces deliberately UNCHANGED -- no broad namespace rename). These tests now assert
// the two-assembly boundary: the shared contract types live in Nexus.DevelopmentControl.Contracts,
// the behavioral runtime implementations stay in Nexus.Developer.Core, and neither assembly
// references any IO/persistence library. A plain xunit fact -- no NetArchTest or other
// architecture-test package is added just for this.
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
    public void SharedDevelopmentControlContracts_AllLiveInTheSharedAssembly()
    {
        var sharedAssembly = typeof(IDevelopmentControlStore).Assembly;

        // Every pure zero-IO contract in the (unchanged) DevelopmentControl namespace that
        // is a value/vocabulary/contract type must resolve to the shared assembly.
        var contractTypes = new[]
        {
            typeof(Node), typeof(NodeId), typeof(NodeType), typeof(Status),
            typeof(NodePhase), typeof(NodePhaseCategory), typeof(NodePhaseClassification),
            typeof(ControlState), typeof(ActiveChange), typeof(PreflightDeclaration),
            typeof(PreflightResult), typeof(PreflightVerdict), typeof(ActorRef),
            typeof(ActorType), typeof(MutationEnvelope), typeof(MutationResult<>),
            typeof(ActivityLogEntry), typeof(AuditFinding), typeof(NodeSearchCriteria),
            typeof(ValidationResult), typeof(AtomicWriteResult<>), typeof(AtomicWriteRequest<>),
            typeof(DevelopmentControlConcurrencyOutcome), typeof(DevelopmentControlMutexIdentity),
            typeof(IDevelopmentControlStore), typeof(IConcurrencyGuardedDevelopmentControlStore),
            typeof(IDevelopmentControlWriteLockFactory), typeof(IDevelopmentControlWriteLock),
            typeof(DevelopmentControlLockAttempt), typeof(DevelopmentControlLockOutcome),
            typeof(IDevelopmentControlAtomicWorkUnitRunner),
            typeof(IDevelopmentControlAtomicWriteCoordinator),
        };

        Assert.All(contractTypes, type => Assert.Equal(sharedAssembly, type.Assembly));
        Assert.Equal("Nexus.DevelopmentControl.Contracts", sharedAssembly.GetName().Name);
    }

    [Fact]
    public void SharedDevelopmentControlContracts_KeepTheHistoricalNamespace()
    {
        // The physical extraction to a shared assembly must not be a namespace rename:
        // consumers (Application/Infrastructure/Api/tests/Forge) keep resolving the same
        // namespace they already imported.
        Assert.Equal("Nexus.Developer.Core.DevelopmentControl", typeof(Node).Namespace);
        Assert.Equal("Nexus.Developer.Core.DevelopmentControl", typeof(IDevelopmentControlStore).Namespace);
    }

    [Fact]
    public void BehavioralRuntimeImplementations_StayInDeveloperCore()
    {
        var coreAssembly = typeof(ConcurrencyGuardedDevelopmentControlStore).Assembly;
        Assert.Equal("Nexus.Developer.Core", coreAssembly.GetName().Name);

        // The shared assembly holds contracts only -- no concrete runtime implementation.
        // A C# `record` compiles to a class carrying an EqualityContract property, and the
        // static NodePhase classifier is `abstract`; so the only non-record, non-abstract
        // classes in the namespace would be the behavioral runtime implementations, which
        // must NOT be present in the shared assembly.
        var shared = typeof(IDevelopmentControlStore).Assembly;
        var concreteNonRecordClasses = shared.GetTypes()
            .Where(type => type.Namespace == typeof(Node).Namespace)
            .Where(type => type.IsClass && !type.IsAbstract)
            .Where(type => type.GetProperty("EqualityContract",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic) is null)
            .Select(type => type.FullName)
            .ToArray();
        Assert.Equal(Array.Empty<string>(), concreteNonRecordClasses);

        // And the concrete runtime decorators remain in Developer.Core.
        foreach (var runtimeType in new[]
        {
            typeof(ConcurrencyGuardedDevelopmentControlStore),
            typeof(DevelopmentControlAtomicWriteCoordinator),
        })
        {
            Assert.Equal(coreAssembly, runtimeType.Assembly);
        }
    }

    [Fact]
    public void SharedAssembly_ReferencesNoExcelOpenXmlEfOrSqlAssembly()
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

    [Fact]
    public void DeveloperCoreAssembly_ReferencesNoExcelOpenXmlEfOrSqlAssembly()
    {
        var referenced = typeof(ConcurrencyGuardedDevelopmentControlStore).Assembly
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
