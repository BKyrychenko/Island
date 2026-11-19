using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Isle.Core;

/// <summary>JSON persistence shared by every store: atomic writes, corrupt file → *.bak-timestamp + fresh value.</summary>
public static class JsonStore
{
    public static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    public static T Load<T>(string path) where T : class, new()
    {
        try
        {
            if (!File.Exists(path)) return new T();
            var text = File.ReadAllText(path);
            return JsonSerializer.Deserialize<T>(text, Options) ?? throw new JsonException("null document");
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException)
        {
            var recovered = Recover<T>(path);
            BackupCorrupt(path);
            return recovered ?? new T();
        }
        catch (Exception ex)
        {
            Logger.Error($"load {Path.GetFileName(path)}", ex);
            return new T();
        }
    }

    public static bool Save<T>(string path, T value)
    {
        try
        {
            WriteAtomic(path, JsonSerializer.Serialize(value, Options));
            return true;
        }
        catch (Exception ex)
        {
            Logger.Error($"save {Path.GetFileName(path)}", ex);
            return false;
        }
    }

    public static void WriteAtomic(string path, string text) =>
        WriteAtomic(path, Encoding.UTF8.GetBytes(text));

    public static void WriteAtomic(string path, byte[] bytes)
    {
        path = Path.GetFullPath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var tmp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            using (var stream = new FileStream(tmp, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                stream.Write(bytes);
                stream.Flush(flushToDisk: true);
            }
            for (var attempt = 0; ; attempt++)
            {
                try { File.Move(tmp, path, overwrite: true); break; }
                catch (IOException) when (attempt < 3) { Thread.Sleep(30 * (attempt + 1)); }
            }
        }
        finally { if (File.Exists(tmp)) File.Delete(tmp); }
    }

    static T? Recover<T>(string path) where T : class, new()
    {
        var full = Path.GetFullPath(path);
        foreach (var backup in Directory.EnumerateFiles(Path.GetDirectoryName(full)!, Path.GetFileName(full) + ".bak-*")
            .OrderByDescending(File.GetLastWriteTimeUtc))
        {
            try
            {
                var value = JsonSerializer.Deserialize<T>(File.ReadAllText(backup), Options);
                if (value is null) continue;
                Logger.Warn($"recovered {Path.GetFileName(path)} from backup");
                return value;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or NotSupportedException) { }
        }
        return null;
    }

    public static string? BackupCorrupt(string path)
    {
        try
        {
            var bak = $"{path}.bak-{DateTime.Now:yyyyMMdd-HHmmss}";
            File.Move(path, bak, overwrite: true);
            Logger.Warn($"{Path.GetFileName(path)} was corrupt; moved to {Path.GetFileName(bak)}");
            return bak;
        }
        catch (Exception ex)
        {
            Logger.Error("backup corrupt file", ex);
            return null;
        }
    }
}
