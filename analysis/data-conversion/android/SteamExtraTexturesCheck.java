package com.openbonk.dataprep;
import java.io.*;
import java.nio.file.*;
import java.security.MessageDigest;
import java.util.*;

public final class SteamExtraTexturesCheck {
    public static void main(String[] a)throws Exception{
        if(a.length!=9)throw new IllegalArgumentException("library base linux windows foreignplan plan corruptzip output private|derived");
        System.load(a[0]);File base=new File(a[1]),plan=new File(a[5]);
        boolean derived=a[8].equals("derived");if(!derived&&!a[8].equals("private"))throw new IllegalArgumentException("bad mode");
        Map<String,byte[]> linux=restore(base,new File(a[2]),plan,derived);
        Map<String,byte[]> windows=restore(base,new File(a[3]),plan,derived);
        if(linux.size()!=25||!linux.keySet().equals(windows.keySet()))throw new AssertionError("inventory differs");
        for(String n:linux.keySet())if(!MessageDigest.isEqual(linux.get(n),windows.get(n)))throw new AssertionError("platform differs: "+n);
        try{restore(base,new File(a[2]),new File(a[4]),derived);throw new AssertionError("foreign plan accepted");}catch(IOException expected){System.out.println("rejected foreign plan: "+expected.getMessage());}
        try{restore(base,new File(a[6]),plan,derived);throw new AssertionError("bad ZIP accepted");}catch(IOException expected){System.out.println("rejected bad ZIP: "+expected.getMessage());}
        Path dir=Paths.get(a[7]);Files.createDirectory(dir);
        for(Map.Entry<String,byte[]> e:linux.entrySet())Files.write(dir.resolve(e.getKey()),e.getValue(),StandardOpenOption.CREATE_NEW);
        System.out.println("PASS: 25 "+a[8]+" diagnostic textures match across Linux and Windows");
    }
    private static Map<String,byte[]> restore(File base,File zip,File plan,boolean derived)throws IOException{return derived?SteamExtraTextures.restore(base,zip,plan):SteamExtraTextures.restorePrivateDiagnostic(base,zip,plan);}
}
