using System.IO;
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
            BackupCorrupt(path);
            return new T();
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

    public static void WriteAtomic(string path, string text)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var tmp = path + ".tmp";
        File.WriteAllText(tmp, text);
        File.Move(tmp, path, overwrite: true);
    }

    public static void WriteAtomic(string path, byte[] bytes)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var tmp = path + ".tmp";
        File.WriteAllBytes(tmp, bytes);
        File.Move(tmp, path, overwrite: true);
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
