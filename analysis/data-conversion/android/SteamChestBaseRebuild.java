package com.openbonk.dataprep;

import java.io.*;
import java.nio.file.*;
import java.util.*;
import java.util.zip.*;
import static com.openbonk.dataprep.SteamExactTextures.*;

/** Restores two exact chest clips into the reduced private base template. */
public final class SteamChestBaseRebuild {
    public static final String SCRUBBED = "fe677585c79002200881375cababe346a42b3a6a911763426264700a80712c27";
    private static final String RESTORED = "3f6f96ef96a2b976d6862d37f7b5c4a06c15eb4ef5bcbd8efa49af17da8aeb76";
    private static final String LINUX = "e4c4298e57fe5aaa76836132e6821b297199c60b2106fa12a0db4ef17a67f5da";
    private static final String WINDOWS = "cb59b62352b2ed5654fc1f1df451cf8f221c260627df6ee5feb7fcedac71cfa9";
    private static final String[] CLIPS = {"7318f74aab3f42f6e12b1c232fc545f9c9328f929abeafcb095bf1b615ee4f45", "be9f3631749314fc7cf2ec844f765a88894e7607b29f7bf1d991bc56db4eff4f"};
    private static final int OFFSET = 2344643, PACKED = 45527, SPLIT = 1048576;
    private static final int[] TARGET = {18655856 - 17 * SPLIT, 18870536 - 17 * SPLIT}, SIZE = {2304, 2500};
    private static final int[] LINUX_OFFSET = {2402600, 2396720}, WINDOWS_OFFSET = {2081632, 2075752};

    public static void restore(File template, File steam, File output) throws IOException {
        Path dest = output.toPath().toAbsolutePath(), parent = dest.getParent();
        if (parent == null || !Files.isDirectory(parent) || Files.exists(dest, LinkOption.NOFOLLOW_LINKS)) throw new IOException("output must be new");
        byte[] base = file(template, 4 * 1024 * 1024);
        if (base.length != 2724266 || !hex(hash(base)).equals(SCRUBBED)) throw new IOException("chest template differs");
        byte[] value = new byte[SPLIT]; Inflater inflater = new Inflater(true);
        try {
            inflater.setInput(base, OFFSET, PACKED); int count = 0;
            while (!inflater.finished() && count < value.length) {
                int n = inflater.inflate(value, count, value.length - count);
                if (n == 0) throw new IOException("cannot inflate chest template");
                count += n;
            }
            if (count != SPLIT || !inflater.finished()) throw new IOException("chest template length differs");
        } catch (DataFormatException e) { throw new IOException("invalid chest template", e); }
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
        boolean linux = source.length == 5824160 && hex(hash(source)).equals(LINUX);
        boolean windows = source.length == 5503192 && hex(hash(source)).equals(WINDOWS);
        if (!linux && !windows) throw new IOException("Steam chest source differs");
        int[] offsets = linux ? LINUX_OFFSET : WINDOWS_OFFSET;
        for (int i = 0; i < TARGET.length; i++) {
            for (int j = TARGET[i]; j < TARGET[i] + SIZE[i]; j++) if (value[j] != 0) throw new IOException("chest clip not blank");
            byte[] clip = Arrays.copyOfRange(source, offsets[i], offsets[i] + SIZE[i]);
            if (!hex(hash(clip)).equals(CLIPS[i])) throw new IOException("Steam chest clip differs");
            System.arraycopy(clip, 0, value, TARGET[i], SIZE[i]);
        }
        Deflater deflater = new SteamStableDeflater(6); ByteArrayOutputStream packed = new ByteArrayOutputStream();
        try {
            deflater.setInput(value); deflater.finish(); byte[] buffer = new byte[4096];
            while (!deflater.finished()) { int n = deflater.deflate(buffer); if (n == 0) throw new IOException("cannot compress chest template"); packed.write(buffer, 0, n); if (packed.size() > PACKED) throw new IOException("chest stream differs"); }
        } finally { deflater.end(); }
        byte[] encoded = packed.toByteArray(); if (encoded.length != PACKED) throw new IOException("chest stream length differs");
        System.arraycopy(encoded, 0, base, OFFSET, PACKED);
        if (!hex(hash(base)).equals(RESTORED)) throw new IOException("restored chest base differs");
        Path temp = Files.createTempFile(parent, ".openbonk-chest-base-", ".tmp"); boolean moved = false;
        try { Files.write(temp, base); Files.move(temp, dest); moved = true; }
        finally { if (!moved) Files.deleteIfExists(temp); }
    }
    private SteamChestBaseRebuild() {}
}
