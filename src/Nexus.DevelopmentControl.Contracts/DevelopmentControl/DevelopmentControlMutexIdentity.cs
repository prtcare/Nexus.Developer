using System.Security.Cryptography;
using System.Text;

namespace Nexus.Developer.Core.DevelopmentControl;

// WI-07-0.2.4: deterministic identity for the named cross-process writer lock protecting a
// Development Control store/workbook. The identity is derived from a caller-supplied store
// identity string (for the Excel adapter that is the workbook's full path, exposed
// publicly as ExcelDevelopmentControlStore.WorkbookPath); Core never embeds one
// machine-specific absolute path as the only valid key. Normalization canonicalizes the
// path form so the same file reached through different casing/separator spellings yields
// the same lock, then maps to a single stable named-object name via SHA-256 -- so no
// machine-specific path ever appears in the kernel object name and two processes over the
// same store contend on the same object. Case folding is applied on Windows to match the
// filesystem's own case-insensitivity.
public sealed record DevelopmentControlMutexIdentity
{
    private const string MutexNamePrefix = "NexusDevelopmentControl_";

    private DevelopmentControlMutexIdentity(string identity, string mutexName)
    {
        Identity = identity;
        MutexName = mutexName;
    }

    // The normalized store identity (a canonical full path for the workbook store).
    public string Identity { get; }

    // The deterministic, cross-process named-object name. Stable for the same governed
    // store identity; no machine-specific absolute path is embedded.
    public string MutexName { get; }

    public static DevelopmentControlMutexIdentity FromStoreIdentity(string identity)
    {
        if (string.IsNullOrWhiteSpace(identity))
            throw new ArgumentException("A store identity is required to derive a mutex identity.", nameof(identity));
        var normalized = NormalizeIdentity(identity);
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalized)));
        return new DevelopmentControlMutexIdentity(normalized, MutexNamePrefix + hash);
    }

    public static DevelopmentControlMutexIdentity FromWorkbookPath(string workbookPath)
    {
        if (workbookPath is null) throw new ArgumentNullException(nameof(workbookPath));
        return FromStoreIdentity(Path.GetFullPath(workbookPath));
    }

    // Canonicalize a store identity: for a filesystem path, resolve it to a full path and
    // normalize separators so "C:\A\b.xlsx" and "c:/a/b.xlsx" are the same store; non-path
    // identities are used verbatim (trimmed). The derivation is case-insensitive on Windows
    // to match the filesystem.
    private static string NormalizeIdentity(string identity)
    {
        var value = identity.Trim();
        var looksLikePath = value.Contains('\\') || value.Contains('/') || Path.HasExtension(value);
        if (!looksLikePath) return value;
        var full = Path.GetFullPath(value).Replace('/', '\\').TrimEnd('\\');
        return OperatingSystem.IsWindows() ? full.ToLowerInvariant() : full;
    }
}
