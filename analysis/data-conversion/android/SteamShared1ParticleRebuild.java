package com.openbonk.dataprep;

import java.io.*;
import java.nio.file.*;
import java.util.*;
import java.util.zip.*;
import static com.openbonk.dataprep.SteamExactTextures.*;

/** Restores only particle bytes identical in both pinned Steam versions. */
public final class SteamShared1ParticleRebuild {
    public static final String SCRUBBED = "9060bf045d4da71d6a1663459361dc38c811a186683e3c92a482200163e41d79";
    private static final String LINUX_SHA = "d9c955e43af9151f4d9d4e3ae9328ff2582bde7a1c9aa4580ddb2ba95ad66b35";
    private static final String WINDOWS_SHA = "64c5156116e20fe645acc133bfae7ab1fdd3a7bbc99f915559efc0640be0349a";
    private static final int SPLIT = 1048576, FIRST = 104 * SPLIT;
    private static final int[] OFFSETS = {1691102, 1745438};
    private static final int[] PACKED = {54225, 38612};
    private static final int[] SIZES = {SPLIT, SPLIT};

    private static byte[] inflate(byte[] data, int offset, int length, int expected, boolean raw) throws IOException {
        Inflater inflater = new Inflater(raw);
        try {
            inflater.setInput(data, offset, length); byte[] out = new byte[expected]; int count = 0;
            while (!inflater.finished() && count < expected) {
                int n = inflater.inflate(out, count, expected-count);
                if (n == 0) throw new IOException("cannot inflate particle data");
                count += n;
            }
            if (count != expected || !inflater.finished()) throw new IOException("particle length differs");
            return out;
        } catch (DataFormatException e) { throw new IOException("invalid particle data", e); }
        finally { inflater.end(); }
    }

    public static void restore(File template, File steam, File output) throws IOException {
        Path dest = output.toPath().toAbsolutePath(), parent = dest.getParent();
        if (parent == null || !Files.isDirectory(parent) || Files.exists(dest, LinkOption.NOFOLLOW_LINKS))
            throw new IOException("output must be new");
        byte[] base = file(template, 4 * 1024 * 1024);
        if (!hex(hash(base)).equals(SCRUBBED)) throw new IOException("particle template differs");
        int total = 0; for (int size : SIZES) total += size;
        byte[] content = new byte[total]; int cursor = 0;
        for (int i=0; i<SIZES.length; i++) {
            byte[] split = inflate(base, OFFSETS[i], PACKED[i], SIZES[i], true);
            System.arraycopy(split, 0, content, cursor, split.length); cursor += split.length;
        }
        byte[] source;
        try (ZipFile zip = new ZipFile(steam)) {
            ZipEntry selected = null; Set<String> seen = new HashSet<>(); Enumeration<? extends ZipEntry> all = zip.entries();
            while (all.hasMoreElements()) {
                ZipEntry e = all.nextElement(); String name = e.getName();
                if (seen.size() >= 20000 || name.startsWith("/") || name.indexOf('\\') >= 0 || name.indexOf(':') >= 0 || !seen.add(name)) throw new IOException("unsafe Steam ZIP");
                for (String part : name.split("/")) if (part.isEmpty() || part.equals(".") || part.equals("..")) throw new IOException("unsafe Steam path");
                if (rootFor(name, "sharedassets1.assets") != null) { if (selected != null) throw new IOException("duplicate Steam asset"); selected = e; }
            }
            source = entry(zip, selected, 64 * 1024 * 1024);
        }
        boolean linux = source.length == 56239204 && hex(hash(source)).equals(LINUX_SHA);
        boolean windows = source.length == 50251684 && hex(hash(source)).equals(WINDOWS_SHA);
        if (!linux && !windows) throw new IOException("Steam particle source differs");
        int[] offsets = linux ? SteamShared1ParticleMasks.LINUX : SteamShared1ParticleMasks.WINDOWS;
        String[] hashes = linux ? SteamShared1ParticleMasks.LINUX_SHA : SteamShared1ParticleMasks.WINDOWS_SHA;
        for (int i=0; i<SteamShared1ParticleMasks.TARGET.length; i++) {
            int target = SteamShared1ParticleMasks.TARGET[i] - FIRST, size = SteamShared1ParticleMasks.SIZE[i], off = offsets[i];
            if (size <= 0 || target < 0 || target > content.length-size || off < 0 || off > source.length-size)
                throw new IOException("invalid particle range");
            byte[] value = Arrays.copyOfRange(source, off, off+size);
            if (!hex(hash(value)).equals(hashes[i])) throw new IOException("Steam particle differs");
            byte[] packed;
            try { packed = Base64.getDecoder().decode(SteamShared1ParticleMasks.MASKS[i]); }
            catch (IllegalArgumentException e) { throw new IOException("invalid particle mask", e); }
            byte[] mask = inflate(packed, 0, packed.length, (size+7)/8, false);
            for (int j=0; j<size; j++) if ((mask[j/8] & (1 << (j%8))) != 0) {
                if (content[target+j] != 0 || value[j] == 0) throw new IOException("particle bytes differ");
                content[target+j] = value[j];
            }
        }
        cursor = 0;
        for (int i=0; i<SIZES.length; i++) {
            Deflater deflater = new SteamStableDeflater(6); ByteArrayOutputStream packed = new ByteArrayOutputStream();
            try {
                deflater.setInput(content, cursor, SIZES[i]); deflater.finish(); byte[] buffer = new byte[4096];
                while (!deflater.finished()) {
                    int n = deflater.deflate(buffer); if (n == 0) throw new IOException("cannot compress particle template");
                    packed.write(buffer, 0, n); if (packed.size() > PACKED[i]) throw new IOException("particle stream differs");
                }
            } finally { deflater.end(); }
            byte[] encoded = packed.toByteArray(); Arrays.fill(base, OFFSETS[i], OFFSETS[i]+PACKED[i], (byte)0);
            System.arraycopy(encoded, 0, base, OFFSETS[i], encoded.length); cursor += SIZES[i];
        }
        if (!hex(hash(base)).equals(SteamShared2ParticleRebuild.SCRUBBED)) throw new IOException("restored particle base differs");
        Path temp = Files.createTempFile(parent, ".openbonk-particle-base-", ".tmp"); boolean moved = false;
        try { Files.write(temp, base); Files.move(temp, dest); moved = true; }
        finally { if (!moved) Files.deleteIfExists(temp); }
    }
    private SteamShared1ParticleRebuild() {}
}
