using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;

// Version-pinned streamed VFX atlases; this is not a general Unity texture parser.
public static class AndroidOriginalAtlasZip
{
    public sealed class Spec
    {
        public readonly string Name, Hash;
        public readonly int Width, Height, Mips, Length;
        public readonly long Offset;
        internal Spec(string name, int width, int height, int mips, long offset, int length, string hash)
        { Name = name; Width = width; Height = height; Mips = mips; Offset = offset; Length = length; Hash = hash; }
    }
    public static readonly Spec[] Atlases = {
        new Spec("atlas01", 512, 256, 10, 28982176, 699052, "be48b9af9e6ee22ea571a6e27e5a7c5c0ac41979c01a1fbc7cc375ca0ddfdd68"),
        new Spec("atlas02", 512, 256, 10, 81937968, 699052, "e62078f8a6d85b2aff3eca210ea33eb8462d342b83a77849ea5a64c7bc15ae9d"),
        new Spec("atlas03", 512, 512, 1, 27476512, 1048576, "80af17f56fe82de5f1efaeb47027d925f97111dec995b48c59d1f8fc8d0af707"),
        new Spec("atlas01_custom", 512, 256, 10, 62652272, 699052, "6e2792a81d5d85821d806d710ab762ad3095012f572af9f6654ea327faa070bc"),
        new Spec("atlas03_Custom_psd", 512, 512, 1, 59592608, 1048576, "58dfacabf2d9ebad62a9c2aec41d1477b775e5f9baf3e541ce0d37ddd98f4784"),
        new Spec("FBX_PoisonFlask_Color", 128, 128, 8, 1924416, 87380, "0c01e8834eae9e8fd5441c295fda3fa9910c41ef07393c3188ae1994a2eb2f5d"),
        new Spec("FBX_Mine_Color", 128, 128, 8, 8739280, 87380, "fe4c8418819270fe7f3e17a4803a6b91add59d3a10cef7c0675fab1a47a652a0"),
        new Spec("FBX_Dice_Color", 128, 128, 8, 82812864, 87380, "a54d83560fc2d46227428daa8387e5ec416d753438a80b7e1e43f96a94d36d8b"),
        new Spec("Axe_Default", 128, 128, 8, 85845040, 87380, "9a1b16064e60eb9a18d55db52f3b31b837fd6d5dbcb9b86f80697f3cc8288f80"),
        new Spec("FBX_PoisonFlask_Color_1", 128, 128, 8, 94669680, 87380, "7fd29f1bcde496d3a386682d2906e6f3c9ca23143daa723c689f91aabc20e570"),
        new Spec("FBX_PoisonFlask_Color_2", 128, 128, 8, 100369056, 87380, "1aab0d6e11e382530ed9111bd19bfaeec207be6bd3125893e94d73bf27138b34"),
        new Spec("ThinCircle", 64, 64, 1, 96411776, 16384, "b95b652d5121565636cd993366a8e2ab819861f9d386508780a49a911e2e2986"),
        new Spec("beam_dark", 128, 16, 1, 97594096, 8192, "a6d3f6657c03863b462ccc8e2d9d8d83f5716cf730f5ed2d0a86716917edf4ef")
    };
    private const long AssetLength = 56239204, ResourceLength = 113115008;
    private const string AssetHash = "d9c955e43af9151f4d9d4e3ae9328ff2582bde7a1c9aa4580ddb2ba95ad66b35";
    private const long WindowsAssetLength = 50251684;
    private const string WindowsAssetHash = "64c5156116e20fe645acc133bfae7ab1fdd3a7bbc99f915559efc0640be0349a";
    private const string ResourceHash = "43c08c5f3228f725f01567bacfabfbb6d2ff50afd7a0536c5a9e1953f6e86fea";
    public static byte[] Extract(Stream input) => ExtractAll(input)["atlas01"];
    public static Dictionary<string, byte[]> ExtractAll(Stream input)
    {
        if (input == null || !input.CanRead || !input.CanSeek) throw new InvalidDataException("A readable, seekable ZIP is required");
        input.Position = 0;
        using var zip = new ZipArchive(input, ZipArchiveMode.Read, true);
        if (zip.Entries.Count > 20000) throw new InvalidDataException("Too many ZIP entries");
        ZipArchiveEntry asset = null, resource = null;
        foreach (var entry in zip.Entries)
        {
            string name = entry.FullName;
            if (name.StartsWith("/", StringComparison.Ordinal) || name.IndexOf('\\') >= 0 || name.IndexOf(':') >= 0) throw new InvalidDataException("Unsafe ZIP path");
            foreach (string part in name.Split('/')) if (part == "." || part == "..") throw new InvalidDataException("Unsafe ZIP path");
            if (Matches(name, "sharedassets1.assets")) { if (asset != null) throw new InvalidDataException("Duplicate asset data"); asset = entry; }
            if (Matches(name, "sharedassets1.assets.resS")) { if (resource != null) throw new InvalidDataException("Duplicate texture resource"); resource = entry; }
        }
        if (asset == null || resource == null || (asset.Length != AssetLength && asset.Length != WindowsAssetLength) || resource.Length != ResourceLength) throw new InvalidDataException("Unsupported original texture data");
        Verify(asset, asset.Length == WindowsAssetLength ? WindowsAssetHash : AssetHash, null);
        var result = new Dictionary<string, byte[]>(StringComparer.Ordinal);
        foreach (var spec in Atlases) result.Add(spec.Name, new byte[spec.Length]);
        Verify(resource, ResourceHash, result);
        foreach (var spec in Atlases) VerifyRaw(spec, result[spec.Name]);
        return result;
    }
    public static void VerifyRaw(byte[] bytes) => VerifyRaw(Atlases[0], bytes);
    public static void VerifyRaw(Spec spec, byte[] bytes)
    {
        if (bytes == null || bytes.Length != spec.Length) throw new InvalidDataException("Invalid atlas size: " + spec.Name);
        using var hash = SHA256.Create();
        if (Hex(hash.ComputeHash(bytes)) != spec.Hash) throw new InvalidDataException("Atlas hash mismatch: " + spec.Name);
    }
    private static bool Matches(string path, string file) => path == file || path == "Megabonk_Data/" + file || path.EndsWith("/Megabonk_Data/" + file, StringComparison.Ordinal);
    private static void Verify(ZipArchiveEntry entry, string expected, Dictionary<string, byte[]> output)
    {
        using var stream = entry.Open(); using var hash = SHA256.Create();
        var buffer = new byte[65536]; long total = 0; int read;
        while ((read = stream.Read(buffer, 0, buffer.Length)) != 0)
        {
            long end = total + read;
            if (end > entry.Length) throw new InvalidDataException("Oversized original texture data");
            if (output != null) foreach (var spec in Atlases)
            {
                long from = Math.Max(total, spec.Offset), to = Math.Min(end, spec.Offset + spec.Length);
                if (from < to) Buffer.BlockCopy(buffer, (int)(from - total), output[spec.Name], (int)(from - spec.Offset), (int)(to - from));
            }
            hash.TransformBlock(buffer, 0, read, buffer, 0); total = end;
        }
        hash.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
        if (total != entry.Length || Hex(hash.Hash) != expected) throw new InvalidDataException("Unsupported original texture data hash");
    }
    private static string Hex(byte[] bytes) => BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
}
