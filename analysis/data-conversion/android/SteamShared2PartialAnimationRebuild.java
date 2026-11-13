package com.openbonk.dataprep;

import java.io.*;
import java.nio.file.*;
import java.util.*;
import java.util.zip.*;
import static com.openbonk.dataprep.SteamExactTextures.*;

/** Restores Steam-identical bytes while retaining the template's differing bytes. */
public final class SteamShared2PartialAnimationRebuild {
    public static final String SCRUBBED = "a6a91aeefb8095e2d096967220541eb1247a0fb27e08f1f799ee2cd71c410892";
    private static final String LINUX_SHA = "e4c4298e57fe5aaa76836132e6821b297199c60b2106fa12a0db4ef17a67f5da";
    private static final String WINDOWS_SHA = "cb59b62352b2ed5654fc1f1df451cf8f221c260627df6ee5feb7fcedac71cfa9";
    private static final int OFFSET = 2344643, PACKED = 45527, SPLIT = 1048576;
    public static void restore(File template, File steam, File output) throws IOException {
        Path dest = output.toPath().toAbsolutePath(), parent = dest.getParent();
        if (parent == null || !Files.isDirectory(parent) || Files.exists(dest, LinkOption.NOFOLLOW_LINKS)) throw new IOException("output must be new");
        byte[] base = file(template, 4 * 1024 * 1024);
        if (base.length != 2724266 || !hex(hash(base)).equals(SCRUBBED)) throw new IOException("partial animation template differs");
        byte[] value = new byte[SPLIT]; Inflater inflater = new Inflater(true);
        try {
            inflater.setInput(base, OFFSET, PACKED); int count = 0;
            while (!inflater.finished() && count < value.length) {
                int n = inflater.inflate(value, count, value.length - count); if (n == 0) throw new IOException("cannot inflate animation template"); count += n;
            }
            if (count != SPLIT || !inflater.finished()) throw new IOException("animation template length differs");
        } catch (DataFormatException e) { throw new IOException("invalid animation template", e); }
        finally { inflater.end(); }
        byte[] source;
        try (ZipFile zip = new ZipFile(steam)) {
            ZipEntry selected = null; Set<String> seen = new HashSet<>(); Enumeration<? extends ZipEntry> all = zip.entries();
            while (all.hasMoreElements()) {
                ZipEntry e = all.nextElement(); String name = e.getName();
                if (seen.size() >= 20000 || name.startsWith("/") || name.indexOf('\\') >= 0 || name.indexOf(':') >= 0 || !seen.add(name)) throw new IOException("unsafe Steam ZIP");
                for (String part : name.split("/")) if (part.isEmpty() || part.equals(".") || part.equals("..")) throw new IOException("unsafe Steam path");
                if (rootFor(name, "sharedassets2.assets") != null) { if (selected != null) throw new IOException("duplicate Steam asset"); selected = e; }
            }
            source = entry(zip, selected, 6 * 1024 * 1024);
        }
        boolean linux = source.length == 5824160 && hex(hash(source)).equals(LINUX_SHA);
        boolean windows = source.length == 5503192 && hex(hash(source)).equals(WINDOWS_SHA);
        if (!linux && !windows) throw new IOException("Steam animation source differs");
        int[] offsets = linux ? SteamShared2AnimationMasks.LINUX : SteamShared2AnimationMasks.WINDOWS;
        for (int i = 0; i < SteamShared2AnimationMasks.TARGET.length; i++) {
            int target = SteamShared2AnimationMasks.TARGET[i], size = SteamShared2AnimationMasks.SIZE[i];
            byte[] clip = Arrays.copyOfRange(source, offsets[i], offsets[i] + size);
            if (clip.length != size || !hex(hash(clip)).equals(SteamShared2AnimationMasks.SOURCE_SHA[i])) throw new IOException("Steam animation differs");
            byte[] mask;
            try { mask = Base64.getDecoder().decode(SteamShared2AnimationMasks.POSITIONS[i]); }
            catch (IllegalArgumentException e) { throw new IOException("invalid animation mask", e); }
            if ((mask.length & 1) != 0) throw new IOException("invalid animation mask");
            boolean[] retained = new boolean[size]; int previous = -1;
            for (int p = 0; p < mask.length; p += 2) {
                int index = (mask[p] & 255) | ((mask[p + 1] & 255) << 8);
                if (index <= previous || index >= size) throw new IOException("invalid animation mask position");
                retained[index] = true; previous = index;
            }
            for (int j = 0; j < size; j++) if (!retained[j]) {
                if (value[target + j] != 0) throw new IOException("animation bytes were not blank");
                value[target + j] = clip[j];
            }
        }
        Deflater deflater = new SteamStableDeflater(6); ByteArrayOutputStream packed = new ByteArrayOutputStream();
        try {
            deflater.setInput(value); deflater.finish(); byte[] buffer = new byte[4096];
            while (!deflater.finished()) { int n = deflater.deflate(buffer); if (n == 0) throw new IOException("cannot compress animation template"); packed.write(buffer, 0, n); if (packed.size() > PACKED) throw new IOException("animation stream differs"); }
        } finally { deflater.end(); }
        byte[] encoded = packed.toByteArray(); Arrays.fill(base, OFFSET, OFFSET + PACKED, (byte)0); System.arraycopy(encoded, 0, base, OFFSET, encoded.length);
        if (!hex(hash(base)).equals(SteamChestBaseRebuild.SCRUBBED)) throw new IOException("restored animation base differs");
        Path temp = Files.createTempFile(parent, ".openbonk-animation-base-", ".tmp"); boolean moved = false;
        try { Files.write(temp, base); Files.move(temp, dest); moved = true; }
        finally { if (!moved) Files.deleteIfExists(temp); }
    }
    private SteamShared2PartialAnimationRebuild() {}
}
