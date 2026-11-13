#if UNITY_EDITOR
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using UnityEngine;

public static class AndroidOriginalAtlasZipCheck
{
    public static void Run()
    {
        string zipPath = Environment.GetEnvironmentVariable("MEGABONK_ATLAS_TEST_ZIP");
        if (!string.IsNullOrEmpty(zipPath))
        {
            using var original = File.OpenRead(zipPath);
            var extracted = AndroidOriginalAtlasZip.ExtractAll(original);
            using var resource = File.OpenRead("/home/your-user/.local/share/Steam/steamapps/common/Megabonk/Megabonk_Data/sharedassets1.assets.resS");
            foreach (var spec in AndroidOriginalAtlasZip.Atlases)
            {
                byte[] expected = new byte[spec.Length]; resource.Position = spec.Offset;
                int total = 0; while (total < expected.Length) { int n = resource.Read(expected, total, expected.Length - total); if (n == 0) throw new EndOfStreamException(); total += n; }
                if (!extracted[spec.Name].SequenceEqual(expected)) throw new Exception("Extracted atlas differs: " + spec.Name);
                expected[0] ^= 1; bool rejected = false;
                try { AndroidOriginalAtlasZip.VerifyRaw(spec, expected); } catch (InvalidDataException) { rejected = true; }
                if (!rejected) throw new Exception("Accepted corrupted atlas: " + spec.Name);
                Debug.Log("Original atlas exact comparison and corruption rejection PASS: " + spec.Name);
            }
            Debug.Log("Original atlas ZIP positive fixture PASS");
        }
        else Debug.Log("Original atlas ZIP positive fixture SKIPPED: MEGABONK_ATLAS_TEST_ZIP is not set");
        foreach (string variant in new[] { "missing", "traversal", "duplicate", "wronghash" })
        {
            using var zip = Fixture(variant); bool rejected = false;
            try { AndroidOriginalAtlasZip.Extract(zip); }
            catch (InvalidDataException) { rejected = true; }
            if (!rejected) throw new Exception("Accepted invalid ZIP: " + variant);
        }
        Debug.Log("Original atlas ZIP checks PASS: missing, traversal, duplicate, wrong hash");
    }
    private static MemoryStream Fixture(string variant)
    {
        var output = new MemoryStream();
        using (var zip = new ZipArchive(output, ZipArchiveMode.Create, true))
        {
            const string prefix = "Megabonk_Data/";
            if (variant == "wronghash")
            {
                AddZeros(zip, prefix + "sharedassets1.assets", 56239204);
                AddZeros(zip, prefix + "sharedassets1.assets.resS", 113115008);
            }
            else
            {
                AddZeros(zip, prefix + "sharedassets1.assets", 1);
                if (variant == "duplicate") AddZeros(zip, prefix + "sharedassets1.assets", 1);
                if (variant == "traversal") AddZeros(zip, "../unsafe", 1);
            }
        }
        output.Position = 0; return output;
    }
    private static void AddZeros(ZipArchive zip, string name, int size)
    {
        using var stream = zip.CreateEntry(name, System.IO.Compression.CompressionLevel.Fastest).Open(); var buffer = new byte[65536];
        while (size > 0) { int n = Math.Min(size, buffer.Length); stream.Write(buffer, 0, n); size -= n; }
    }
}
#endif
