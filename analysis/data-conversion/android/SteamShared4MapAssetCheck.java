package com.openbonk.dataprep;
import java.io.*;
import java.nio.file.*;
import java.security.MessageDigest;
import java.util.*;

public final class SteamShared4MapAssetCheck {
    public static void main(String[] a)throws Exception{
        if(a.length!=8)throw new IllegalArgumentException("library base linux windows corrupt badplan plan output");
        System.load(a[0]);File base=new File(a[1]),plan=new File(a[6]);
        Map<String,byte[]> l=SteamShared4MapAsset.restore(base,new File(a[2]),plan),w=SteamShared4MapAsset.restore(base,new File(a[3]),plan);
        if(l.size()!=2||!l.keySet().equals(w.keySet()))throw new AssertionError("missing splits");
        for(String n:l.keySet())if(!MessageDigest.isEqual(l.get(n),w.get(n)))throw new AssertionError("platform differs");
        try{SteamShared4MapAsset.restore(base,new File(a[4]),plan);throw new AssertionError("bad ZIP accepted");}catch(IOException expected){System.out.println("rejected bad ZIP: "+expected.getMessage());}
        try{SteamShared4MapAsset.restore(base,new File(a[2]),new File(a[5]));throw new AssertionError("bad plan accepted");}catch(IOException expected){System.out.println("rejected bad plan: "+expected.getMessage());}
        Path dir=Paths.get(a[7]);Files.createDirectory(dir);for(Map.Entry<String,byte[]> r:l.entrySet())Files.write(dir.resolve(r.getKey()),r.getValue(),StandardOpenOption.CREATE_NEW);
        System.out.println("PASS: final-map shared asset reconstructed from Linux and Windows Steam ZIPs");
    }
}
