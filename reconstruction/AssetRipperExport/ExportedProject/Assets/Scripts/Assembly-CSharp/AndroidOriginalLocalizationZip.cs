using System;
using System.IO;
using System.IO.Compression;

// Reads only the requested original string bundle and its shared dependency.
// Callers must stage/publish both outputs as a unit after validating them.
public static class AndroidOriginalLocalizationZip
{
    private const long Limit = 8 * 1024 * 1024;
    private const string Shared = "localization-assets-shared_assets_all.bundle";
    private const string Prefix = "StreamingAssets/aa/StandaloneLinux64/";
    private const string WindowsPrefix = "StreamingAssets/aa/StandaloneWindows64/";
    public sealed class Converted { public byte[] Strings; public byte[] SharedData; }

    public static Converted Convert(Stream input, string stringBundle)
    {
        if (input == null || !input.CanRead || !input.CanSeek) throw new InvalidDataException("A readable, seekable ZIP is required");
        if (string.IsNullOrEmpty(stringBundle) || !stringBundle.StartsWith("localization-string-tables-", StringComparison.Ordinal) || !stringBundle.EndsWith("_assets_all.bundle", StringComparison.Ordinal) || stringBundle.IndexOfAny(new[] { '/', '\\', ':' }) >= 0)
            throw new InvalidDataException("Invalid localization bundle name");
        using var zip = new ZipArchive(input, ZipArchiveMode.Read, true);
        if (zip.Entries.Count > 20000) throw new InvalidDataException("Too many ZIP entries");
        ZipArchiveEntry strings = null, shared = null;
        foreach (var entry in zip.Entries)
        {
            string name = entry.FullName;
            if (name.StartsWith("/", StringComparison.Ordinal) || name.IndexOf('\\') >= 0 || name.IndexOf(':') >= 0) throw new InvalidDataException("Unsafe ZIP path");
            foreach (string part in name.Split('/')) if (part == "." || part == "..") throw new InvalidDataException("Unsafe ZIP path");
            if (Matches(name, stringBundle)) { if (strings != null) throw new InvalidDataException("Duplicate string bundle"); strings = entry; }
            if (Matches(name, Shared)) { if (shared != null) throw new InvalidDataException("Duplicate shared bundle"); shared = entry; }
        }
        if (strings == null || shared == null) throw new InvalidDataException("Required localization bundle missing");
        var sourceStrings = Read(strings); var sourceShared = Read(shared);
        // Both conversions must succeed before any result is returned.
        return new Converted { Strings = AndroidLocalizationBundleConverter.Convert(sourceStrings), SharedData = AndroidLocalizationBundleConverter.Convert(sourceShared) };
    }
    private static bool Matches(string path, string name)
    {
        return MatchesPrefix(path, Prefix + name) || MatchesPrefix(path, WindowsPrefix + name);
    }
    private static bool MatchesPrefix(string path, string suffix) =>
        path == suffix || path == "Megabonk_Data/" + suffix || path.EndsWith("/Megabonk_Data/" + suffix, StringComparison.Ordinal);
    private static byte[] Read(ZipArchiveEntry entry)
    {
        if (entry.Length <= 0 || entry.Length > Limit) throw new InvalidDataException("Invalid localization bundle size");
        using var input = entry.Open(); using var output = new MemoryStream();
        byte[] buf = new byte[32768]; int read; long total = 0;
        while ((read = input.Read(buf, 0, buf.Length)) != 0)
        {
            total += read; if (total > Limit) throw new InvalidDataException("Localization bundle too large");
            output.Write(buf, 0, read);
        }
        if (total != entry.Length) throw new InvalidDataException("Incomplete localization bundle");
        return output.ToArray();
    }
}
