package com.openbonk.dataprep;
import java.io.*;
import java.nio.file.*;
import java.security.MessageDigest;

public final class SteamShared2FinalTexturesCheck {
    public static void main(String[] a)throws Exception{
        if(a.length!=5)throw new IllegalArgumentException("library base-asset steam-zip plan output");System.load(a[0]);
        byte[] base=Files.readAllBytes(Paths.get(a[1])),before=MessageDigest.getInstance("SHA-256").digest(base);
        byte[] result=SteamShared2FinalTextures.restore(base,new File(a[2]),new File(a[3]));
        if(!MessageDigest.isEqual(before,MessageDigest.getInstance("SHA-256").digest(base)))throw new AssertionError("base mutated");
        Files.write(Paths.get(a[4]),result,StandardOpenOption.CREATE_NEW);System.out.println("PASS: final three textures; full sharedassets2 restored exactly");
    }
}
