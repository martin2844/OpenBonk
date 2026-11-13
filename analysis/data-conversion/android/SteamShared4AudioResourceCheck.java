package com.openbonk.dataprep;
import java.io.*;
import java.nio.file.*;
import java.security.MessageDigest;

public final class SteamShared4AudioResourceCheck {
    public static void main(String[] a)throws Exception{
        if(a.length!=6)throw new IllegalArgumentException("linux windows corrupt foreignplan plan output");
        File plan=new File(a[4]);byte[] l=SteamShared4AudioResource.restore(new File(a[0]),plan),w=SteamShared4AudioResource.restore(new File(a[1]),plan);
        if(!MessageDigest.isEqual(l,w))throw new AssertionError("platform output differs");
        try{SteamShared4AudioResource.restore(new File(a[2]),plan);throw new AssertionError("corrupt ZIP accepted");}catch(IOException expected){System.out.println("rejected corrupt ZIP: "+expected.getMessage());}
        try{SteamShared4AudioResource.restore(new File(a[0]),new File(a[3]));throw new AssertionError("foreign plan accepted");}catch(IOException expected){System.out.println("rejected foreign plan: "+expected.getMessage());}
        try{SteamShared2AudioResource.restore(new File(a[0]),plan);throw new AssertionError("wrong resource plan accepted");}catch(IOException expected){System.out.println("rejected wrong resource plan: "+expected.getMessage());}
        Files.write(Paths.get(a[5]),l,StandardOpenOption.CREATE_NEW);System.out.println("PASS: 6 audio clips; Linux/Windows resources match, bytes="+l.length);
    }
}
