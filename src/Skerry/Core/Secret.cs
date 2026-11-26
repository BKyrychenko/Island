using System.Security.Cryptography;
using System.Text;

namespace Skerry.Core;

/// <summary>DPAPI (current user) for secrets in settings and the clipboard store. Stored as "dpapi:" + base64.</summary>
public static class Secret
{
    const string Prefix = "dpapi:";
    // Kept from the app's former name (Isle): changing it would make every stored secret unreadable.
    static readonly byte[] Entropy = "Isle.v1"u8.ToArray();

    public static string Protect(string plain)
    {
        if (plain.Length == 0) return "";
        return Prefix + Convert.ToBase64String(ProtectBytes(Encoding.UTF8.GetBytes(plain)));
    }

    /// <summary>Plain text, or "" if the value is empty or cannot be decrypted (another user/machine). Non-prefixed values pass through.</summary>
    public static string Unprotect(string stored)
    {
        if (string.IsNullOrEmpty(stored)) return "";
        if (!stored.StartsWith(Prefix, StringComparison.Ordinal)) return stored;
        try { return Encoding.UTF8.GetString(UnprotectBytes(Convert.FromBase64String(stored[Prefix.Length..]))); }
        catch (Exception ex) when (ex is CryptographicException or FormatException) { return ""; }
    }

    public static byte[] ProtectBytes(byte[] data) => ProtectedData.Protect(data, Entropy, DataProtectionScope.CurrentUser);
    public static byte[] UnprotectBytes(byte[] data) => ProtectedData.Unprotect(data, Entropy, DataProtectionScope.CurrentUser);
}
