package com.megabonk.launcher;

import java.io.*;
import java.nio.file.Files;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.zip.ZipEntry;
import java.util.zip.ZipOutputStream;

/** Creates a validated private snapshot; callers must keep the game closed. */
final class LauncherProfileExport {
    private static final long FILE_LIMIT = 8L * 1024 * 1024;

    static ArrayList<String> accounts(File externalRoot) throws IOException {
        ArrayList<String> result = new ArrayList<>();
        if (externalRoot == null) return result;
        File root = new File(externalRoot, "Saves/CloudDir");
        if (!root.isDirectory() || Files.isSymbolicLink(root.toPath())) return result;
        File[] dirs = root.listFiles();
        if (dirs == null) throw new IOException("cannot list profiles");
        Arrays.sort(dirs, (a, b) -> a.getName().compareTo(b.getName()));
        for (File dir : dirs) {
            String id = dir.getName();
            if (!id.matches("[0-9]{1,20}") || Files.isSymbolicLink(dir.toPath()) || !dir.isDirectory()) continue;
            if (!dir.getCanonicalFile().getParentFile().equals(root.getCanonicalFile())) continue;
            if (new File(dir, "progression.json").isFile() && new File(dir, "stats.json").isFile()) result.add(id);
        }
        return result;
    }

    static void stage(File externalRoot, String id, File dest) throws IOException {
        if (!accounts(externalRoot).contains(id)) throw new IOException("profile unavailable");
        File dir = new File(externalRoot, "Saves/CloudDir/" + id);
        File[] files = { new File(dir, "progression.json"), new File(dir, "stats.json") };
        byte[][] data = new byte[files.length][];
        long[] modified = new long[files.length], lengths = new long[files.length];
        for (int i = 0; i < files.length; i++) {
            File file = files[i];
            if (Files.isSymbolicLink(file.toPath()) || !file.getCanonicalFile().getParentFile().equals(dir.getCanonicalFile())) throw new IOException("invalid profile path");
            lengths[i] = file.length(); modified[i] = file.lastModified();
            if (lengths[i] <= 0 || lengths[i] > FILE_LIMIT) throw new IOException("invalid profile size");
        }
        for (int i = 0; i < files.length; i++) {
            ByteArrayOutputStream bytes = new ByteArrayOutputStream();
            try (InputStream in = new FileInputStream(files[i])) {
                byte[] b = new byte[65536]; long total = 0;
                for (int n; (n = in.read(b)) != -1;) {
                    if (n == 0) continue;
                    total += n; if (total > FILE_LIMIT) throw new IOException("profile too large");
                    bytes.write(b, 0, n);
                }
            }
            data[i] = bytes.toByteArray();
        }
        for (int i = 0; i < files.length; i++)
            if (data[i].length != lengths[i] || files[i].length() != lengths[i] || files[i].lastModified() != modified[i]) throw new IOException("profile changed during export");
        try (FileOutputStream out = new FileOutputStream(dest); ZipOutputStream zip = new ZipOutputStream(out)) {
            for (int i = 0; i < files.length; i++) {
                zip.putNextEntry(new ZipEntry(files[i].getName())); zip.write(data[i]); zip.closeEntry();
            }
            zip.finish(); out.getFD().sync();
        }
        if (!LauncherProfileZipValidator.valid(dest)) { dest.delete(); throw new IOException("profile did not pass validation"); }
    }
    private LauncherProfileExport() {}
}
