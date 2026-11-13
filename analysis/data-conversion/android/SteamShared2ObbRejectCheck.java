package com.openbonk.dataprep;

import java.io.File;
import java.io.IOException;
import java.nio.file.Files;

/** Checks plan rejection and cleanup without relying on Android's crash handler. */
public final class SteamShared2ObbRejectCheck {
    public static void main(String[] a) throws Exception {
        if (a.length != 15) throw new IllegalArgumentException("library base prep partial steam audio1 audio2 audio4 map mesh texture animation final output expected-error");
        System.load(a[0]);
        File output = new File(a[13]);
        if (Files.exists(output.toPath())) throw new IllegalArgumentException("output already exists");
        String[] before = output.getAbsoluteFile().getParentFile().list((dir, name) -> name.startsWith(".openbonk-"));
        if (before == null) throw new IOException("cannot inspect temporary directory");
        java.util.Arrays.sort(before);
        try {
            SteamPartialObbStage.assembleShared2AndMapDiagnostic(new File(a[1]), new File(a[2]), new File(a[3]), new File(a[4]), new File(a[5]), new File(a[6]), new File(a[7]), new File(a[8]), new File(a[9]), new File(a[10]), new File(a[11]), new File(a[12]), output);
            throw new AssertionError("invalid plan was accepted");
        } catch (IOException expected) {
            if (!a[14].equals(expected.getMessage())) throw expected;
        }
        if (Files.exists(output.toPath())) throw new AssertionError("partial output published");
        String[] after = output.getAbsoluteFile().getParentFile().list((dir, name) -> name.startsWith(".openbonk-"));
        if (after == null) throw new IOException("cannot inspect temporary directory");
        java.util.Arrays.sort(after);
        if (!java.util.Arrays.equals(before, after)) throw new AssertionError("temporary files leaked");
        System.out.println("PASS: " + a[14] + "; no published output or leaked temporary files");
    }
}
