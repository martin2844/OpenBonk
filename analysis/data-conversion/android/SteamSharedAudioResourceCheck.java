package com.openbonk.dataprep;
import java.io.*;
import java.nio.file.*;
import java.security.*;

public final class SteamSharedAudioResourceCheck {
    public static void main(String[] a)throws Exception{
        if(a.length!=6)throw new IllegalArgumentException("linux windows corrupt badplan plan output");
        File plan=new File(a[4]);byte[] linux=SteamSharedAudioResource.restore(new File(a[0]),plan),windows=SteamSharedAudioResource.restore(new File(a[1]),plan);
        if(!MessageDigest.isEqual(linux,windows))throw new AssertionError("platform output differs");
        try{SteamSharedAudioResource.restore(new File(a[2]),plan);throw new AssertionError("corrupt ZIP accepted");}catch(IOException expected){System.out.println("rejected corrupt ZIP: "+expected.getMessage());}
        try{SteamSharedAudioResource.restore(new File(a[0]),new File(a[3]));throw new AssertionError("foreign plan accepted");}catch(IOException expected){System.out.println("rejected foreign plan: "+expected.getMessage());}
        Files.write(Paths.get(a[5]),linux,StandardOpenOption.CREATE_NEW);System.out.println("PASS: 118 audio clips and video; Linux/Windows resources match, bytes="+linux.length);
    }
}
