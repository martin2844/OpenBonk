using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using UnityEngine;

// Installs the first version of the Lua mod format without replacing an
// existing package. The caller owns archive selection and UI feedback.
public static class AndroidModPackageImporter
{
    private const int FileLimit = 65536;
    private const int ZipLimit = 1024 * 1024;
    private static readonly UTF8Encoding StrictUtf8 = new UTF8Encoding(false, true);

    [Serializable] private sealed class Manifest
    {
        public string id;
        public int api;
        public bool enabled;
        public string hook;
        public string[] hooks;
    }

    public static string Install(Stream input, string modsRoot)
    {
        if (input == null || !input.CanRead || !input.CanSeek || input.Length > ZipLimit)
            throw new InvalidDataException("ZIP must be readable and no larger than 1 MiB");
        byte[] manifestBytes = null, scriptBytes = null;
        string prefix = null, directoryPrefix = null;
        input.Position = 0;
        using (var zip = new ZipArchive(input, ZipArchiveMode.Read, true))
        {
            if (zip.Entries.Count > 3) throw new InvalidDataException("Unexpected files in package");
            foreach (var entry in zip.Entries)
            {
                string name = entry.FullName;
                if (string.IsNullOrEmpty(name) || name.IndexOf('\\') >= 0 || name.StartsWith("/", StringComparison.Ordinal))
                    throw new InvalidDataException("Invalid archive path");
                if (name.EndsWith("/", StringComparison.Ordinal))
                {
                    string directory = name.Substring(0, name.Length - 1);
                    if (!ValidId(directory) || directoryPrefix != null) throw new InvalidDataException("Invalid package directory");
                    directoryPrefix = directory;
                    continue;
                }
                string[] parts = name.Split('/');
                if (parts.Length < 1 || parts.Length > 2 || (parts.Length == 2 && !ValidId(parts[0])))
                    throw new InvalidDataException("Invalid archive path");
                string currentPrefix = parts.Length == 2 ? parts[0] : "";
                if (prefix == null) prefix = currentPrefix;
                else if (prefix != currentPrefix) throw new InvalidDataException("Files must share one package directory");
                string leaf = parts[parts.Length - 1];
                if (leaf == "manifest.json" && manifestBytes == null) manifestBytes = Read(entry);
                else if (leaf == "main.lua" && scriptBytes == null) scriptBytes = Read(entry);
                else throw new InvalidDataException("Unexpected or duplicate file in package");
            }
        }
        if (manifestBytes == null || scriptBytes == null) throw new InvalidDataException("Package requires manifest.json and main.lua");
        if (directoryPrefix != null && directoryPrefix != prefix) throw new InvalidDataException("Unexpected package directory");
        Manifest manifest;
        try { manifest = JsonUtility.FromJson<Manifest>(StrictUtf8.GetString(manifestBytes)); }
        catch (Exception e) { throw new InvalidDataException("Invalid manifest", e); }
        if (manifest == null || !ValidId(manifest.id) || !AndroidModHooks.TryGetStats(manifest.api, manifest.hook, manifest.hooks, out _) ||
            (!string.IsNullOrEmpty(prefix) && prefix != manifest.id))
            throw new InvalidDataException("Unsupported manifest or package ID");
        string script = StrictUtf8.GetString(scriptBytes);
        if (script.IndexOf('\0') >= 0) throw new InvalidDataException("NUL in script");
        Directory.CreateDirectory(modsRoot);
        string destination = Path.Combine(modsRoot, manifest.id);
        if (Directory.Exists(destination) || File.Exists(destination)) throw new IOException("A mod with this ID is already installed");
        string staging = Path.Combine(modsRoot, ".import-" + Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(staging);
            File.WriteAllBytes(Path.Combine(staging, "manifest.json"), manifestBytes);
            File.WriteAllBytes(Path.Combine(staging, "main.lua"), scriptBytes);
            Directory.Move(staging, destination);
        }
        finally { if (Directory.Exists(staging)) Directory.Delete(staging, true); }
        return manifest.id;
    }

    private static byte[] Read(ZipArchiveEntry entry)
    {
        if (entry.Length > FileLimit) throw new InvalidDataException("Package file is too large");
        using (Stream stream = entry.Open())
        using (var data = new MemoryStream())
        {
            byte[] buffer = new byte[4096];
            int read;
            while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
            {
                if (data.Length + read > FileLimit) throw new InvalidDataException("Package file is too large");
                data.Write(buffer, 0, read);
            }
            return data.ToArray();
        }
    }

    private static bool ValidId(string id)
    {
        if (string.IsNullOrEmpty(id) || id.Length > 64 || !AlphaNumeric(id[0])) return false;
        foreach (char ch in id) if (!AlphaNumeric(ch) && ch != '-' && ch != '_' && ch != '.') return false;
        return true;
    }

    private static bool AlphaNumeric(char ch) => (ch >= 'a' && ch <= 'z') || (ch >= 'A' && ch <= 'Z') || (ch >= '0' && ch <= '9');
}
