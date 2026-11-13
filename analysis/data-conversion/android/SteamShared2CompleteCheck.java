package com.openbonk.dataprep;
import java.io.*;
import java.nio.file.*;
import java.security.MessageDigest;

public final class SteamShared2CompleteCheck {
    public static void main(String[] a)throws Exception{
        if(a.length!=8)throw new IllegalArgumentException("library base steam mesh-plan texture-plan animation-plan final-plan output");System.load(a[0]);
        byte[] asset=SteamShared2Complete.restore(new File(a[1]),new File(a[2]),new File(a[3]),new File(a[4]),new File(a[5]),new File(a[6]));
        String sha=SteamExactTextures.hex(MessageDigest.getInstance("SHA-256").digest(asset));
        if(!sha.equals("a3bb71785a61a2acddb230e28b1b1f8466c076a944112e18273433a635faea37"))throw new AssertionError("final asset differs");
        Files.write(Paths.get(a[7]),asset,StandardOpenOption.CREATE_NEW);System.out.println("PASS: complete sharedassets2 reconstructed from Steam; sha256="+sha);
    }
}
