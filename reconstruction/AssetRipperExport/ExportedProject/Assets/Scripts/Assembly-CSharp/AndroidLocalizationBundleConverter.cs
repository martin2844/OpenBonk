using System;
using System.IO;
using System.Text;

// Narrow UnityFS v8 retargeter for the original Linux and Windows localization bundles.
// It rewrites one serialized-file target field and emits uncompressed UnityFS.
// It is not a general Unity asset converter.
public static class AndroidLocalizationBundleConverter
{
    private const int Limit = 8 * 1024 * 1024;
    private sealed class Reader
    {
        internal readonly byte[] data; internal int pos;
        internal Reader(byte[] bytes) { data = bytes; }
        internal byte[] Take(int n) { if (n < 0 || n > data.Length - pos) throw new InvalidDataException("truncated bundle"); var b = new byte[n]; Buffer.BlockCopy(data, pos, b, 0, n); pos += n; return b; }
        internal int U16() { var b = Take(2); return b[0] << 8 | b[1]; }
        internal int U32() { var b = Take(4); uint n = (uint)b[0] << 24 | (uint)b[1] << 16 | (uint)b[2] << 8 | b[3]; if (n > int.MaxValue) throw new InvalidDataException("integer too large"); return (int)n; }
        internal long U64() { long a = U32(); long b = U32Unsigned(); return (a << 32) | b; }
        private long U32Unsigned() { var b = Take(4); return (long)b[0] << 24 | (long)b[1] << 16 | (long)b[2] << 8 | b[3]; }
        internal string Text() { int start = pos; while (pos < data.Length && data[pos] != 0 && pos - start < 256) pos++; if (pos >= data.Length || data[pos] != 0) throw new InvalidDataException("invalid string"); string s = Encoding.ASCII.GetString(data, start, pos - start); pos++; return s; }
        internal void Align() { int next = (pos + 15) & ~15; Take(next - pos); }
    }
    private static byte[] Decompress(byte[] input, int length, int compression)
    {
        if (length < 0 || length > Limit) throw new InvalidDataException("decompressed size limit");
        if (compression == 0) { if (input.Length != length) throw new InvalidDataException("block length mismatch"); return input; }
        if (compression != 2 && compression != 3) throw new InvalidDataException("unsupported compression");
        var output = new byte[length]; int i = 0, o = 0;
        while (i < input.Length)
        {
            int token = input[i++], literals = token >> 4;
            if (literals == 15) { int n; do { if (i >= input.Length) throw new InvalidDataException("bad LZ4 literals"); n = input[i++]; literals += n; } while (n == 255); }
            if (literals > input.Length - i || literals > output.Length - o) throw new InvalidDataException("bad LZ4 literal length");
            Buffer.BlockCopy(input, i, output, o, literals); i += literals; o += literals;
            if (i == input.Length) break;
            if (input.Length - i < 2) throw new InvalidDataException("bad LZ4 offset");
            int offset = input[i] | input[i + 1] << 8; i += 2;
            if (offset == 0 || offset > o) throw new InvalidDataException("bad LZ4 back reference");
            int count = (token & 15) + 4;
            if ((token & 15) == 15) { int n; do { if (i >= input.Length) throw new InvalidDataException("bad LZ4 match"); n = input[i++]; count += n; } while (n == 255); }
            if (count > output.Length - o) throw new InvalidDataException("bad LZ4 match length");
            for (int j = 0; j < count; j++) output[o + j] = output[o + j - offset];
            o += count;
        }
        if (o != length) throw new InvalidDataException("incomplete LZ4 block");
        return output;
    }
    private static void U16(Stream s, int n) { s.WriteByte((byte)(n >> 8)); s.WriteByte((byte)n); }
    private static void U32(Stream s, int n) { s.WriteByte((byte)(n >> 24)); s.WriteByte((byte)(n >> 16)); s.WriteByte((byte)(n >> 8)); s.WriteByte((byte)n); }
    private static void U64(Stream s, long n) { U32(s, (int)(n >> 32)); U32(s, (int)n); }
    private static void Text(Stream s, string x) { var b = Encoding.ASCII.GetBytes(x); s.Write(b, 0, b.Length); s.WriteByte(0); }
    private static void Align(Stream s) { while ((s.Position & 15) != 0) s.WriteByte(0); }
    public static byte[] Convert(byte[] input)
    {
        if (input == null || input.Length > Limit) throw new InvalidDataException("bundle size limit");
        var r = new Reader(input);
        if (r.Text() != "UnityFS" || r.U32() != 8) throw new InvalidDataException("unsupported UnityFS");
        string player = r.Text(), engine = r.Text();
        if (player != "5.x.x" || engine != "2023.2.22f1") throw new InvalidDataException("unsupported Unity version");
        if (r.U64() != input.Length) throw new InvalidDataException("bundle size mismatch");
        int compressed = r.U32(), uncompressed = r.U32(), flags = r.U32();
        // Original localization bundles use combined metadata, LZ4HC and 16-byte data padding.
        if (flags != 0x243 || compressed > Limit || uncompressed > Limit) throw new InvalidDataException("unsupported bundle layout");
        r.Align(); var info = new Reader(Decompress(r.Take(compressed), uncompressed, flags & 63));
        info.Take(16); int blocks = info.U32();
        if (blocks < 1 || blocks > 128) throw new InvalidDataException("bad block count");
        int[] sizes = new int[blocks], zipped = new int[blocks], kinds = new int[blocks]; int total = 0;
        for (int i = 0; i < blocks; i++) { sizes[i] = info.U32(); zipped[i] = info.U32(); kinds[i] = info.U16(); if (sizes[i] > Limit - total || zipped[i] > Limit) throw new InvalidDataException("block size limit"); total += sizes[i]; }
        if (info.U32() != 1 || info.U64() != 0 || info.U64() != total) throw new InvalidDataException("unsupported directory");
        int entryFlags = info.U32(); string name = info.Text();
        if (!name.StartsWith("CAB-", StringComparison.Ordinal) || info.pos != info.data.Length) throw new InvalidDataException("invalid serialized entry");
        r.Align(); var data = new byte[total]; int cursor = 0;
        for (int i = 0; i < blocks; i++) { var bytes = Decompress(r.Take(zipped[i]), sizes[i], kinds[i] & 63); Buffer.BlockCopy(bytes, 0, data, cursor, bytes.Length); cursor += bytes.Length; }
        if (r.pos != input.Length || data.Length < 68 || data[8] != 0 || data[9] != 0 || data[10] != 0 || data[11] != 22 || data[16] != 0) throw new InvalidDataException("unsupported serialized file");
        int end = 48; while (end < data.Length && data[end] != 0 && end < 80) end++;
        if (end >= data.Length || Encoding.ASCII.GetString(data, 48, end - 48) != engine || end + 5 > data.Length || (data[end + 1] != 24 && data[end + 1] != 19) || data[end + 2] != 0 || data[end + 3] != 0 || data[end + 4] != 0) throw new InvalidDataException("unsupported serialized target");
        data[end + 1] = 13;
        using var meta = new MemoryStream(); meta.Write(new byte[16], 0, 16); U32(meta, 1); U32(meta, total); U32(meta, total); U16(meta, 0); U32(meta, 1); U64(meta, 0); U64(meta, total); U32(meta, entryFlags); Text(meta, name);
        byte[] directory = meta.ToArray();
        using var result = new MemoryStream(); Text(result, "UnityFS"); U32(result, 8); Text(result, player); Text(result, engine); long sizeAt = result.Position; U64(result, 0); U32(result, directory.Length); U32(result, directory.Length); U32(result, 0x40); Align(result); result.Write(directory, 0, directory.Length); result.Write(data, 0, data.Length);
        long size = result.Length; result.Position = sizeAt; U64(result, size); return result.ToArray();
    }
}
