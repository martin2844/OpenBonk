using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Assets.Scripts.Saves___Serialization.SaveFiles;

// Profile archive conversion only. Call while SaveManager is not using the
// target account. The caller chooses the account and coordinates app lifecycle.
public static class AndroidProfileArchive
{
    private const long ZipLimit = 16 * 1024 * 1024, FileLimit = 8 * 1024 * 1024, ExistingLimit = 64 * 1024 * 1024;
    private static readonly UTF8Encoding StrictUtf8 = new UTF8Encoding(false, true);
    public static void Recover(string cloudRoot, string account)
    {
        ValidateAccount(account);
        string current = Path.Combine(cloudRoot, account), pending = Path.Combine(cloudRoot, ".profile-previous-" + account);
        if (!Directory.Exists(pending)) return;
        if (!Directory.Exists(current)) Directory.Move(pending, current);
        else ArchivePrevious(cloudRoot, account, pending);
    }
    public static string Import(Stream input, string cloudRoot, string account)
    {
        ValidateAccount(account);
        var files = ReadArchive(input);
        ValidateProfile(files.progression, files.stats);
        Directory.CreateDirectory(cloudRoot);
        Recover(cloudRoot, account);
        string current = Path.Combine(cloudRoot, account), previous = Path.Combine(cloudRoot, ".profile-previous-" + account);
        string stage = Path.Combine(cloudRoot, ".profile-stage-" + Guid.NewGuid().ToString("N"));
        bool moved = false, published = false;
        try
        {
            Directory.CreateDirectory(stage);
            if (Directory.Exists(current)) { long total = 0; int count = 0; CopyExisting(current, stage, ref total, ref count, 0); }
            foreach (var item in new[] { ("progression.json", files.progression), ("stats.json", files.stats) })
            {
                File.WriteAllText(Path.Combine(stage, item.Item1), item.Item2, StrictUtf8);
                File.WriteAllText(Path.Combine(stage, item.Item1 + ".bak"), item.Item2, StrictUtf8);
            }
            if (Directory.Exists(current)) { Directory.Move(current, previous); moved = true; }
            Directory.Move(stage, current); published = true;
            if (!moved) return null;
            // The new directory is already published. If archiving the old
            // directory fails, report the retained recovery path as the backup;
            // Recover can retry the rename later without losing either copy.
            try { return ArchivePrevious(cloudRoot, account, previous); }
            catch (IOException) { return previous; }
            catch (UnauthorizedAccessException) { return previous; }
        }
        catch
        {
            if (!published && moved && !Directory.Exists(current) && Directory.Exists(previous)) Directory.Move(previous, current);
            throw;
        }
        finally { if (Directory.Exists(stage)) Directory.Delete(stage, true); }
    }
    private static string ArchivePrevious(string root, string account, string previous)
    {
        string backup = Path.Combine(root, ".profile-backup-" + account + "-" + Guid.NewGuid().ToString("N"));
        Directory.Move(previous, backup); return backup;
    }
    private static void ValidateAccount(string account)
    {
        if (string.IsNullOrEmpty(account) || account.Length > 20) throw new InvalidDataException("Invalid account ID");
        foreach (char c in account) if (c < '0' || c > '9') throw new InvalidDataException("Invalid account ID");
    }
    private static (string progression, string stats) ReadArchive(Stream input)
    {
        if (input == null || !input.CanRead || !input.CanSeek || input.Length > ZipLimit) throw new InvalidDataException("Invalid profile ZIP");
        input.Position = 0; string progression = null, stats = null, prefix = null;
        using var zip = new ZipArchive(input, ZipArchiveMode.Read, true);
        if (zip.Entries.Count > 128) throw new InvalidDataException("Too many archive entries");
        foreach (var entry in zip.Entries)
        {
            string name = entry.FullName;
            if (string.IsNullOrEmpty(name) || name.StartsWith("/", StringComparison.Ordinal) || name.IndexOf('\\') >= 0 || name.IndexOf(':') >= 0) throw new InvalidDataException("Unsafe archive path");
            foreach (string part in name.Split('/')) if (part == "." || part == "..") throw new InvalidDataException("Unsafe archive path");
            if (name.EndsWith("/", StringComparison.Ordinal)) continue;
            string leaf = name.Substring(name.LastIndexOf('/') + 1);
            if (leaf == "progression.json" || leaf == "stats.json")
            {
                string parent = name.Substring(0, name.Length - leaf.Length);
                if (prefix == null) prefix = parent;
                else if (parent != prefix) throw new InvalidDataException("Profile files are from different folders");
                if (leaf == "progression.json") { if (progression != null) throw new InvalidDataException("Duplicate progression"); progression = Read(entry); }
                else { if (stats != null) throw new InvalidDataException("Duplicate stats"); stats = Read(entry); }
            }
        }
        if (progression == null || stats == null) throw new InvalidDataException("Profile ZIP needs progression.json and stats.json");
        return (progression, stats);
    }
    private static string Read(ZipArchiveEntry entry)
    {
        if (entry.Length <= 0 || entry.Length > FileLimit) throw new InvalidDataException("Invalid profile file size");
        using var stream = entry.Open(); using var memory = new MemoryStream();
        var buffer = new byte[65536]; int n; long total = 0;
        while ((n = stream.Read(buffer, 0, buffer.Length)) != 0) { total += n; if (total > FileLimit) throw new InvalidDataException("Profile file too large"); memory.Write(buffer, 0, n); }
        if (total != entry.Length) throw new InvalidDataException("Truncated profile file");
        return StrictUtf8.GetString(memory.ToArray());
    }
    private static void ValidateProfile(string progression, string stats)
    {
        try
        {
            string progressJson = SimpleEncryptor.Decrypt(progression), statsJson = SimpleEncryptor.Decrypt(stats);
            var progressObject = JObject.Parse(progressJson); var statsObject = JObject.Parse(statsJson);
            if (progressObject["silver"]?.Type != JTokenType.Integer || progressObject["achievements"]?.Type != JTokenType.Array || statsObject["stats"]?.Type != JTokenType.Object)
                throw new InvalidDataException("Missing required profile fields");
            var p = JsonConvert.DeserializeObject<ProgressionSaveFile>(progressJson);
            var s = JsonConvert.DeserializeObject<StatsSaveFile>(statsJson);
            if (p?.achievements == null || p.shopItems == null || p.characterProgression == null || s?.stats == null || s.times == null || s.enemyLogs == null)
                throw new InvalidDataException("Invalid profile structure");
        }
        catch (InvalidDataException) { throw; }
        catch (Exception e) { throw new InvalidDataException("Profile could not be decoded", e); }
    }
    private static void CopyExisting(string source, string dest, ref long total, ref int count, int depth)
    {
        if (depth > 16) throw new InvalidDataException("Account directory too deep");
        if ((File.GetAttributes(source) & FileAttributes.ReparsePoint) != 0) throw new InvalidDataException("Links in account directory are unsupported");
        foreach (string path in Directory.GetFiles(source))
        {
            var info = new FileInfo(path);
            if ((info.Attributes & FileAttributes.ReparsePoint) != 0 || ++count > 512 || info.Length > ExistingLimit - total) throw new InvalidDataException("Account directory too large or contains a link");
            total += info.Length; File.Copy(path, Path.Combine(dest, info.Name));
        }
        foreach (string path in Directory.GetDirectories(source))
        {
            if (++count > 512) throw new InvalidDataException("Account directory too large");
            string next = Path.Combine(dest, Path.GetFileName(path)); Directory.CreateDirectory(next); CopyExisting(path, next, ref total, ref count, depth + 1);
        }
    }
}
