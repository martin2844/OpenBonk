package com.openbonk.dataprep;
import java.nio.file.Files;
import java.nio.file.Paths;
import java.security.MessageDigest;
import java.util.Arrays;

public final class LocalizationCabCheck {
    public static void main(String[] args) throws Exception {
        if (args.length != 4) throw new IllegalArgumentException("template source plan expected");
        byte[] template=Files.readAllBytes(Paths.get(args[0])), source=Files.readAllBytes(Paths.get(args[1]));
        byte[] plan=Files.readAllBytes(Paths.get(args[2])), expected=Files.readAllBytes(Paths.get(args[3]));
        if (!Arrays.equals(SteamLocalizationCab.restore(template,source,plan),expected)) throw new AssertionError("output differs");
        byte[] corrupt=source.clone(); corrupt[corrupt.length-1]^=1;
        try { SteamLocalizationCab.restore(template,corrupt,plan); throw new AssertionError("corrupt source accepted"); }
        catch (java.io.IOException ok) { }
        corrupt=plan.clone(); corrupt[corrupt.length-1]^=1;
        try { SteamLocalizationCab.restore(template,source,corrupt); throw new AssertionError("corrupt plan accepted"); }
        catch (java.io.IOException ok) { }
        System.out.println("PASS: reconstructed bundle exactly; modified source and plan rejected");
    }
}
