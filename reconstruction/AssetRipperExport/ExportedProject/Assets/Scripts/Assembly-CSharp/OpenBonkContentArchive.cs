using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

// Experimental API 3 content archive. Kept separate from the shipped Lua-only
// importer until a content runtime can actually register its definitions.
public static class OpenBonkContentArchive
{
    private const long MaxArchive = 128L * 1024 * 1024;
    private const long MaxFile = 64L * 1024 * 1024;
    private const long MaxTotal = 128L * 1024 * 1024;
    private const int MaxManifest = 65536, MaxEntries = 128, MaxFiles = 64;
    private static readonly UTF8Encoding Utf8 = new UTF8Encoding(false, true);
    [Serializable] private sealed class Manifest { public int api; public string kind; public string id; public FileSpec[] files; }
    [Serializable] private sealed class FileSpec { public string path; public long size; public string sha256; }

    public static string Install(Stream input, string root)
    {
        if (input == null || !input.CanRead || !input.CanSeek || input.Length <= 0 || input.Length > MaxArchive) throw new InvalidDataException("Invalid archive size");
        input.Position = 0;
        using var zip = new ZipArchive(input, ZipArchiveMode.Read, true);
        if (zip.Entries.Count > MaxEntries) throw new InvalidDataException("Too many archive entries");
        var entries = new Dictionary<string, ZipArchiveEntry>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in zip.Entries)
        {
            string path = entry.FullName;
            if (entry.FullName.EndsWith("/", StringComparison.Ordinal))
            {
                if (!ValidPath(path.Substring(0, path.Length - 1))) throw new InvalidDataException("Invalid directory");
                continue;
            }
            if (!ValidPath(path) || entries.ContainsKey(path)) throw new InvalidDataException("Invalid or duplicate path");
            entries.Add(path, entry);
        }
        if (!entries.TryGetValue("manifest.json", out var manifestEntry) || manifestEntry.FullName != "manifest.json") throw new InvalidDataException("Missing manifest");
        byte[] manifestBytes = ReadSmall(manifestEntry, MaxManifest);
        Manifest manifest;
        try { manifest = JsonUtility.FromJson<Manifest>(Utf8.GetString(manifestBytes)); }
        catch (Exception e) { throw new InvalidDataException("Invalid manifest", e); }
        if (manifest == null || manifest.api != 3 || manifest.kind != "content" || !ValidId(manifest.id) || manifest.files == null || manifest.files.Length == 0 || manifest.files.Length > MaxFiles)
            throw new InvalidDataException("Invalid content manifest");
        var declared = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        long total = 0;
        foreach (var file in manifest.files)
        {
            if (file == null || !ValidPath(file.path) || file.path.Equals("manifest.json", StringComparison.OrdinalIgnoreCase) || !declared.Add(file.path) || file.size <= 0 || file.size > MaxFile || !ValidHash(file.sha256)) throw new InvalidDataException("Invalid file declaration");
            if (!entries.TryGetValue(file.path, out var entry) || entry.FullName != file.path || entry.Length != file.size) throw new InvalidDataException("Missing file or size mismatch");
            total += file.size;
            if (total > MaxTotal) throw new InvalidDataException("Content too large");
        }
        if (!entries.TryGetValue("main.lua", out var scriptEntry) || scriptEntry.FullName != "main.lua" || !declared.Contains("main.lua") || entries.Count != declared.Count + 1) throw new InvalidDataException("Missing script or unexpected file");
        string destination = Path.Combine(root, manifest.id);
        Directory.CreateDirectory(root);
        if (File.Exists(destination) || Directory.Exists(destination)) throw new IOException("Mod ID already installed");
        string staging = Path.Combine(root, ".import-" + Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(staging);
            File.WriteAllBytes(Path.Combine(staging, "manifest.json"), manifestBytes);
            foreach (var file in manifest.files)
            {
                string target = Path.Combine(staging, file.path.Replace('/', Path.DirectorySeparatorChar));
                Directory.CreateDirectory(Path.GetDirectoryName(target));
                using var source = entries[file.path].Open();
                using var output = new FileStream(target, FileMode.CreateNew, FileAccess.Write, FileShare.None);
                using var sha = SHA256.Create();
                var buffer = new byte[65536]; long length = 0; int count;
                while ((count = source.Read(buffer, 0, buffer.Length)) > 0)
                {
                    length += count;
                    if (length > file.size) throw new InvalidDataException("File exceeds declared size");
                    sha.TransformBlock(buffer, 0, count, null, 0); output.Write(buffer, 0, count);
                }
                sha.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                if (length != file.size || !string.Equals(BitConverter.ToString(sha.Hash).Replace("-", ""), file.sha256, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Content hash mismatch");
            }
            var scriptFile = Path.Combine(staging, "main.lua");
            if (new FileInfo(scriptFile).Length > 65536 || Utf8.GetString(File.ReadAllBytes(scriptFile)).IndexOf('\0') >= 0) throw new InvalidDataException("Invalid script");
            Directory.Move(staging, destination);
            return manifest.id;
        }
        finally { if (Directory.Exists(staging)) Directory.Delete(staging, true); }
    }
    private static byte[] ReadSmall(ZipArchiveEntry entry, int max)
    {
        if (entry.Length <= 0 || entry.Length > max) throw new InvalidDataException("Invalid manifest size");
        using var source = entry.Open(); using var output = new MemoryStream();
        byte[] buffer = new byte[4096]; int count;
        while ((count = source.Read(buffer, 0, buffer.Length)) > 0) { if (output.Length + count > max) throw new InvalidDataException("Manifest too large"); output.Write(buffer, 0, count); }
        return output.ToArray();
    }
    private static bool ValidHash(string value)
    {
        if (value == null || value.Length != 64) return false;
        foreach (char c in value) if (!((c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F'))) return false;
        return true;
    }
    private static bool ValidId(string value)
    {
        if (string.IsNullOrEmpty(value) || value.Length > 64 || !AlphaNumeric(value[0])) return false;
        foreach (char c in value) if (!AlphaNumeric(c) && c != '-' && c != '_' && c != '.') return false;
        return true;
    }
    private static bool ValidPath(string path)
    {
        if (string.IsNullOrEmpty(path) || path.Length > 240 || path[0] == '/' || path.IndexOf('\\') >= 0) return false;
        string[] parts = path.Split('/'); if (parts.Length > 8) return false;
        foreach (string part in parts) if (!ValidId(part) || part == "." || part == "..") return false;
        return true;
    }
    private static bool AlphaNumeric(char c) => (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9');
}
