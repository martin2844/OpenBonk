package com.openbonk.dataprep;
import java.io.*;
import java.nio.file.*;

public final class SteamShared1ParticleRebuildCheck {
    public static void main(String[] args) throws Exception {
        if (args.length != 4) throw new IllegalArgumentException("template steam output pass|reject");
        File output = new File(args[2]);
        try {
            SteamShared1ParticleRebuild.restore(new File(args[0]), new File(args[1]), output);
            if (!args[3].equals("pass")) throw new AssertionError("invalid input accepted");
            System.out.println("PASS: exact shared1 particle base restored");
        } catch (IOException e) {
            if (!args[3].equals("reject") || Files.exists(output.toPath(), LinkOption.NOFOLLOW_LINKS)) throw e;
            System.out.println("PASS: invalid shared1 particle input rejected without output");
        }
    }
}
