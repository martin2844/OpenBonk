#if UNITY_EDITOR
using System;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class OpenBonkContentArchiveCheck
{
    private static string Hash(byte[] data) { using var h = SHA256.Create(); return BitConverter.ToString(h.ComputeHash(data)).Replace("-", "").ToLowerInvariant(); }
    private static MemoryStream Make(string manifest, params (string path, byte[] data)[] files)
    {
        var buffer = new MemoryStream();
        using (var zip = new ZipArchive(buffer, ZipArchiveMode.Create, true))
        {
            using (var stream = zip.CreateEntry("manifest.json").Open()) { var bytes = Encoding.UTF8.GetBytes(manifest); stream.Write(bytes, 0, bytes.Length); }
            foreach (var item in files) using (var stream = zip.CreateEntry(item.path).Open()) stream.Write(item.data, 0, item.data.Length);
        }
        buffer.Position = 0; return buffer;
    }
    private static void Require(bool value, string message) { if (!value) throw new Exception(message); }
    private static void Reject(string root, string manifest, params (string, byte[])[] files)
    {
        bool rejected = false;
        try { using var zip = Make(manifest, files); OpenBonkContentArchive.Install(zip, root); }
        catch (Exception e) when (e is InvalidDataException || e is IOException || e is DecoderFallbackException) { rejected = true; }
        Require(rejected, "archive should be rejected");
    }
    public static void Run()
    {
        string root = Path.Combine(Application.temporaryCachePath, "content-check-" + Guid.NewGuid().ToString("N"));
        string source = Path.GetFullPath(Path.Combine(Application.dataPath, "../../../../mods/pikachu/study/android-bundle/pikachu-model"));
        byte[] bundle = File.ReadAllBytes(source), script = Encoding.UTF8.GetBytes("return {}\n");
        string manifest = "{\"api\":3,\"kind\":\"content\",\"id\":\"pikachu-test\",\"files\":[" +
            "{\"path\":\"main.lua\",\"size\":" + script.Length + ",\"sha256\":\"" + Hash(script) + "\"}," +
            "{\"path\":\"assets/model.bundle\",\"size\":" + bundle.Length + ",\"sha256\":\"" + Hash(bundle) + "\"}]}";
        var files = new[] { ("main.lua", script), ("assets/model.bundle", bundle) };
        try
        {
            using (var zip = Make(manifest, files)) Require(OpenBonkContentArchive.Install(zip, root) == "pikachu-test", "valid import");
            string model = Path.Combine(root, "pikachu-test", "assets/model.bundle");
            Require(Hash(File.ReadAllBytes(model)) == Hash(bundle), "bundle changed on import");
            Reject(root, manifest, files);
            Require(Hash(File.ReadAllBytes(model)) == Hash(bundle), "duplicate replaced existing content");
            var mutated = (byte[])bundle.Clone(); mutated[100] ^= 1;
            string invalid = root + "-invalid";
            Reject(invalid, manifest, ("main.lua", script), ("assets/model.bundle", mutated));
            Reject(invalid, manifest, ("main.lua", script));
            Reject(invalid, manifest, ("main.lua", script), ("assets/model.bundle", bundle), ("extra", script));
            Reject(invalid, manifest, ("main.lua", script), ("assets/model.bundle", bundle), ("../escape", script));
            Reject(invalid, manifest, ("main.lua", script), ("assets/model.bundle", bundle), ("MAIN.LUA", script));
            Reject(invalid, manifest.Replace("assets/model.bundle", "../model.bundle"), ("main.lua", script), ("assets/model.bundle", bundle));
            Require(!Directory.Exists(invalid) || Directory.GetFileSystemEntries(invalid).Length == 0, "staging remains after failure");
            Debug.Log("OpenBonkContentArchiveCheck PASS: real Android bundle, exact hash, preservation, corruption, missing/extra/duplicate paths and traversal");
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); if (Directory.Exists(root + "-invalid")) Directory.Delete(root + "-invalid", true); }
    }
}
#endif
