package com.openbonk.dataprep;

import java.io.IOException;
import java.nio.ByteBuffer;
import java.nio.ByteOrder;
import java.util.Arrays;

/** Restricted UnityFS reader for experimentally reconstructing localization bundles. */
public final class SteamUnityFs {
    private static final int LIMIT = 32 * 1024 * 1024;
    private SteamUnityFs() {}

    private static IOException invalid() { return new IOException("Unsupported or malformed UnityFS bundle"); }
    private static void need(ByteBuffer b, int n) throws IOException { if (n < 0 || n > b.remaining()) throw invalid(); }
    private static int integer(ByteBuffer b) throws IOException { need(b, 4); return b.getInt(); }
    private static long longValue(ByteBuffer b) throws IOException { need(b, 8); return b.getLong(); }
    private static String string(ByteBuffer b) throws IOException {
        StringBuilder s = new StringBuilder();
        while (b.hasRemaining() && s.length() < 256) {
            int c = b.get() & 255; if (c == 0) return s.toString();
            if (c < 32 || c > 126) throw invalid(); s.append((char)c);
        }
        throw invalid();
    }
    private static int align16(int n) throws IOException { if (n > Integer.MAX_VALUE - 15) throw invalid(); return (n + 15) & ~15; }
    private static byte[] bytes(ByteBuffer b, int n) throws IOException { need(b,n); byte[] r = new byte[n]; b.get(r); return r; }
    private static int extended(byte[] input, int[] pos, int length) throws IOException {
        if (length == 15) {
            int n;
            do { if (pos[0] >= input.length) throw invalid(); n = input[pos[0]++] & 255; if (length > LIMIT - n) throw invalid(); length += n; } while (n == 255);
        }
        return length;
    }
    static byte[] decompress(byte[] input, int size, int kind) throws IOException {
        if (size < 0 || size > LIMIT || input.length > LIMIT) throw invalid();
        if (kind == 0) { if (input.length != size) throw invalid(); return input.clone(); }
        if (kind != 2 && kind != 3) throw invalid();
        byte[] output = new byte[size]; int[] p = {0}; int out = 0;
        while (p[0] < input.length) {
            int token = input[p[0]++] & 255;
            int literal = extended(input, p, token >>> 4);
            if (literal > input.length - p[0] || literal > size - out) throw invalid();
            System.arraycopy(input,p[0],output,out,literal); p[0] += literal; out += literal;
            if (p[0] == input.length) break;
            if (input.length - p[0] < 2) throw invalid();
            int distance = (input[p[0]] & 255) | ((input[p[0]+1] & 255) << 8); p[0] += 2;
            int match = extended(input,p,token & 15) + 4;
            if (distance == 0 || distance > out || match > size - out) throw invalid();
            for (int i=0; i<match; i++) output[out+i] = output[out+i-distance];
            out += match;
        }
        if (out != size || p[0] != input.length) throw invalid();
        return output;
    }

    /** Return the sole serialized CAB after checking all container bounds. */
    public static byte[] readSingleCab(byte[] bundle) throws IOException {
        if (bundle == null || bundle.length > LIMIT) throw invalid();
        ByteBuffer b = ByteBuffer.wrap(bundle).order(ByteOrder.BIG_ENDIAN);
        if (!"UnityFS".equals(string(b))) throw invalid();
        int version = integer(b); if (version != 7 && version != 8) throw invalid();
        string(b); if (!"2023.2.22f1".equals(string(b))) throw invalid();
        if (longValue(b) != bundle.length) throw invalid();
        int compressedInfo = integer(b), plainInfo = integer(b), flags = integer(b);
        if ((flags & ~0x2ff) != 0 || (flags & 0x40) == 0 || compressedInfo < 0 || compressedInfo > LIMIT || plainInfo < 0 || plainInfo > LIMIT) throw invalid();
        int start = align16(b.position()); if (start > bundle.length) throw invalid();
        boolean atEnd = (flags & 0x80) != 0;
        int infoStart = atEnd ? bundle.length - compressedInfo : start;
        if (infoStart < start || infoStart > bundle.length - compressedInfo) throw invalid();
        b.position(infoStart); byte[] info = decompress(bytes(b,compressedInfo),plainInfo,flags & 63);
        int dataStart = atEnd ? start : infoStart + compressedInfo;
        if ((flags & 0x200) != 0) dataStart = align16(dataStart);
        int dataEnd = atEnd ? infoStart : bundle.length;
        if (dataStart > dataEnd) throw invalid();
        ByteBuffer i = ByteBuffer.wrap(info).order(ByteOrder.BIG_ENDIAN); need(i,16); i.position(16);
        int count = integer(i); if (count < 1 || count > 1024) throw invalid();
        int[] uncompressed = new int[count], compressed = new int[count], kinds = new int[count];
        long total = 0, stored = 0;
        for (int n=0;n<count;n++) {
            uncompressed[n]=integer(i); compressed[n]=integer(i); need(i,2); kinds[n]=i.getShort() & 63;
            if (uncompressed[n]<0 || compressed[n]<0 || (total += uncompressed[n]) > LIMIT || (stored += compressed[n]) > LIMIT) throw invalid();
        }
        if (stored != dataEnd-dataStart || integer(i) != 1) throw invalid();
        long offset=longValue(i), length=longValue(i); integer(i); String name=string(i);
        if (i.hasRemaining() || !name.startsWith("CAB-") || offset < 0 || length < 0 || offset > total || length > total-offset) throw invalid();
        byte[] joined = new byte[(int)total]; int pos=0; b.position(dataStart);
        for (int n=0;n<count;n++) { byte[] chunk=decompress(bytes(b,compressed[n]),uncompressed[n],kinds[n]); System.arraycopy(chunk,0,joined,pos,chunk.length); pos += chunk.length; }
        return Arrays.copyOfRange(joined,(int)offset,(int)(offset+length));
    }
}
