using Nexus.Developer.Core.DevelopmentControl;
using Xunit;

namespace Nexus.Developer.Core.Tests;

// WI-07-0.2.4 stabilization (SP1-M00): deterministic, cross-process writer-lock identity.
// The identity is derived from a store identity (for the Excel adapter, the workbook's full
// path) and mapped to a single stable named-object name via SHA-256. These tests lock the
// determinism and normalization guarantees the concurrency layer depends on: the same store
// reached through different path spellings must yield the same kernel-object name on
// Windows, and a non-path identity must be used verbatim rather than path-normalized.
public class DevelopmentControlMutexIdentityTests
{
    private const string Prefix = "NexusDevelopmentControl_";

    [Fact]
    public void FromStoreIdentity_IsDeterministic()
    {
        var first = DevelopmentControlMutexIdentity.FromStoreIdentity("C:\\Nexus\\book.xlsx");
        var second = DevelopmentControlMutexIdentity.FromStoreIdentity("C:\\Nexus\\book.xlsx");

        Assert.Equal(first.Identity, second.Identity);
        Assert.Equal(first.MutexName, second.MutexName);
    }

    [Fact]
    public void MutexName_IsPrefixedWithTheStableObjectPrefix()
    {
        var identity = DevelopmentControlMutexIdentity.FromStoreIdentity("C:\\book.xlsx");

        Assert.StartsWith(Prefix, identity.MutexName);
        Assert.True(identity.MutexName.Length > Prefix.Length);
    }

    // On Windows the filesystem is case-insensitive and '/' and '\' are interchangeable, so
    // different spellings of the same file must contend on the SAME kernel object. Off
    // Windows the case-folding is intentionally not applied, so the assertion is guarded.
    [Fact]
    public void PathSpellings_ResolveToTheSameIdentity_OnWindows()
    {
        var upper = DevelopmentControlMutexIdentity.FromStoreIdentity(@"C:\Nexus\Book.XLSX");
        var lower = DevelopmentControlMutexIdentity.FromStoreIdentity(@"c:/nexus/book.xlsx");

        if (OperatingSystem.IsWindows())
        {
            Assert.Equal(upper.MutexName, lower.MutexName);
        }
    }

    [Fact]
    public void FromWorkbookPath_ResolvesRelativeToAFullPath()
    {
        var identity = DevelopmentControlMutexIdentity.FromWorkbookPath("some-dir\\w.xlsx");

        Assert.EndsWith("some-dir\\w.xlsx", identity.Identity, System.StringComparison.OrdinalIgnoreCase);
        Assert.False(string.IsNullOrWhiteSpace(identity.MutexName));
    }

    [Fact]
    public void NonPathIdentity_IsUsedVerbatim_NotPathNormalized()
    {
        var identity = DevelopmentControlMutexIdentity.FromStoreIdentity("NexusWorkbookStore");

        Assert.Equal("NexusWorkbookStore", identity.Identity);
    }

    [Fact]
    public void EmptyStoreIdentity_Throws()
    {
        Assert.Throws<ArgumentException>(() => DevelopmentControlMutexIdentity.FromStoreIdentity(""));
        Assert.Throws<ArgumentException>(() => DevelopmentControlMutexIdentity.FromStoreIdentity("   "));
    }

    [Fact]
    public void NullWorkbookPath_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => DevelopmentControlMutexIdentity.FromWorkbookPath(null!));
    }
}
