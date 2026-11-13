package com.openbonk.dataprep;
import java.io.*;
import java.nio.file.*;

public final class SteamShared2PartialAnimationRebuildCheck {
    public static void main(String[] args) throws Exception {
        if (args.length != 4) throw new IllegalArgumentException("template steam output pass|reject");
        File output = new File(args[2]);
        try {
            SteamShared2PartialAnimationRebuild.restore(new File(args[0]), new File(args[1]), output);
            if (!args[3].equals("pass")) throw new AssertionError("invalid input accepted");
            System.out.println("PASS: exact partially scrubbed base restored");
        } catch (IOException e) {
            if (!args[3].equals("reject") || Files.exists(output.toPath(), LinkOption.NOFOLLOW_LINKS)) throw e;
            System.out.println("PASS: invalid partial animation source rejected without output");
        }
    }
}
