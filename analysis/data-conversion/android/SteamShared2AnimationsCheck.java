package com.openbonk.dataprep;
import java.io.*;
import java.nio.file.*;
import java.security.MessageDigest;

public final class SteamShared2AnimationsCheck {
    public static void main(String[] a)throws Exception{
        if(a.length!=6)throw new IllegalArgumentException("base-asset linux windows corrupt plan output");
        byte[] base=Files.readAllBytes(Paths.get(a[0])),before=MessageDigest.getInstance("SHA-256").digest(base);File plan=new File(a[4]);
        byte[] l=SteamShared2Animations.restorePartial(base,new File(a[1]),plan),w=SteamShared2Animations.restorePartial(base,new File(a[2]),plan);
        if(!MessageDigest.isEqual(l,w))throw new AssertionError("platform outputs differ");
        try{SteamShared2Animations.restorePartial(base,new File(a[3]),plan);throw new AssertionError("altered source accepted");}catch(IOException expected){System.out.println("rejected altered source: "+expected.getMessage());}
        try{SteamShared2Animations.restorePartial(base,new File(a[1]),new File(a[0]));throw new AssertionError("foreign plan accepted");}catch(IOException expected){System.out.println("rejected foreign plan: "+expected.getMessage());}
        if(!MessageDigest.isEqual(before,MessageDigest.getInstance("SHA-256").digest(base)))throw new AssertionError("base mutated");
        Files.write(Paths.get(a[5]),l,StandardOpenOption.CREATE_NEW);System.out.println("PASS: both animation objects restored from Steam; asset remains incomplete");
    }
}
