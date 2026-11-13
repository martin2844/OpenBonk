package com.openbonk.dataprep;
import java.io.*;
import java.nio.file.*;
import java.security.MessageDigest;

public final class SteamShared2MeshesCheck {
    public static void main(String[] a)throws Exception{
        if(a.length!=6)throw new IllegalArgumentException("base linux windows corrupt plan output");
        File base=new File(a[0]),plan=new File(a[4]);byte[] l=SteamShared2Meshes.restorePartial(base,new File(a[1]),plan),w=SteamShared2Meshes.restorePartial(base,new File(a[2]),plan);
        if(!MessageDigest.isEqual(l,w))throw new AssertionError("platform assets differ");
        try{SteamShared2Meshes.restorePartial(base,new File(a[3]),plan);throw new AssertionError("bad source accepted");}catch(IOException expected){System.out.println("rejected altered source: "+expected.getMessage());}
        try{SteamShared2Meshes.restorePartial(base,new File(a[1]),base);throw new AssertionError("foreign plan accepted");}catch(IOException expected){System.out.println("rejected foreign plan: "+expected.getMessage());}
        Files.write(Paths.get(a[5]),l,StandardOpenOption.CREATE_NEW);System.out.println("PASS: mesh/material plan matches between Linux and Windows; intermediate asset remains incomplete");
    }
}
