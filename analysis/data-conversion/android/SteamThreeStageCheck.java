package com.openbonk.dataprep;

import java.io.*;
import java.nio.file.*;
import java.security.*;
import java.util.*;

/** Private standalone check of the experimental asset preparation chain. */
public final class SteamThreeStageCheck {
    private static String hash(File file) throws Exception {
        MessageDigest digest = MessageDigest.getInstance("SHA-256"); byte[] buffer = new byte[65536];
        try (InputStream in = new FileInputStream(file)) { for (int n; (n = in.read(buffer)) != -1;) if (n > 0) digest.update(buffer, 0, n); }
        StringBuilder s = new StringBuilder(); for (byte b : digest.digest()) s.append(String.format("%02x", b & 255)); return s.toString();
    }
    public static void main(String[] args) throws Exception {
        if ((args.length < 8 || args.length > 12) || !args[7].matches("[0-9a-f]{64}")) throw new IllegalArgumentException("library template SteamZIP texturePlan packedPlan audioPlan new-work-dir expected-output-SHA256 [meshPlan [cubemapPlan [animationPlan [objectPlan]]]]");
        System.load(new File(args[0]).getCanonicalPath());
        Path root = Paths.get(args[6]); Files.createDirectory(root);
        try {
            File texture = root.resolve("texture.obb").toFile(), packed = root.resolve("packed.obb").toFile(), result = root.resolve("result.obb").toFile();
            File steam = new File(args[2]);
            File animationInput = new File(args[1]);
            if (args.length == 12) {
                animationInput = root.resolve("object.obb").toFile();
                SteamPlannedMeshes.restoreExactObjects(new File(args[1]), steam, new File(args[11]), hash(new File(args[11])), animationInput);
            }
            File cubeInput = animationInput;
            if (args.length >= 11) {
                cubeInput = root.resolve("animation.obb").toFile();
                SteamPlannedMeshes.restoreAnimations(animationInput, steam, new File(args[10]), hash(new File(args[10])), cubeInput);
            }
            File meshInput = cubeInput;
            if (args.length >= 10) {
                meshInput = root.resolve("cubemap.obb").toFile();
                SteamPlannedTextures.restoreCubemaps(cubeInput, steam, new File(args[9]), hash(new File(args[9])), meshInput);
            }
            File textureInput = meshInput;
            if (args.length >= 9) {
                textureInput = root.resolve("mesh.obb").toFile();
                SteamPlannedMeshes.restore(meshInput, steam, new File(args[8]), hash(new File(args[8])), textureInput);
            }
            SteamPlannedTextures.restore(textureInput, steam, new File(args[3]), hash(new File(args[3])), texture);
            SteamPackedAudio.restore(texture, steam, new File(args[4]), hash(new File(args[4])), packed);
            SteamGeneratedAudio.restore(packed, steam, new File(args[5]), hash(new File(args[5])), result);
            if (!hash(result).equals(args[7])) throw new IOException("final output checksum differs");
            System.out.println("PASS: all " + (args.length == 12 ? "seven" : args.length == 11 ? "six" : args.length == 10 ? "five" : args.length == 9 ? "four" : "three") + " preparation stages reproduced the expected OBB checksum");
        } finally {
            try (java.util.stream.Stream<Path> paths = Files.walk(root)) {
                paths.sorted(Comparator.reverseOrder()).forEach(p -> { try { Files.deleteIfExists(p); } catch (IOException ignored) {} });
            }
        }
    }
    private SteamThreeStageCheck() {}
}
