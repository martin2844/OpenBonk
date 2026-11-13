package com.openbonk.dataprep;
import java.io.*;
import java.nio.file.*;
import java.security.MessageDigest;

public final class SteamShared2TextureRejectCheck {
    public static void main(String[] a)throws Exception{
        if(a.length!=5)throw new IllegalArgumentException("library base-asset corrupt-zip valid-zip plan");System.load(a[0]);
        byte[] base=Files.readAllBytes(Paths.get(a[1])),before=MessageDigest.getInstance("SHA-256").digest(base);
        try{SteamShared2Textures.restorePartial(base,new File(a[2]),new File(a[4]));throw new AssertionError("altered ZIP accepted");}catch(IOException expected){System.out.println("rejected altered ZIP: "+expected.getMessage());}
        try{SteamShared2Textures.restorePartial(base,new File(a[3]),new File(a[1]));throw new AssertionError("foreign plan accepted");}catch(IOException expected){System.out.println("rejected foreign plan: "+expected.getMessage());}
        if(!MessageDigest.isEqual(before,MessageDigest.getInstance("SHA-256").digest(base)))throw new AssertionError("base mutated");
        System.out.println("PASS: rejected invalid inputs without modifying base");
    }
}
