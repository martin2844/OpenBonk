package com.openbonk.dataprep;
import java.io.*;
import java.nio.file.*;
import java.security.MessageDigest;
import java.util.*;

public final class SteamConvertedTexturesCheck {
    public static void main(String[] args)throws Exception {
        if(args.length!=8)throw new IllegalArgumentException("library base linux windows badplan plan output corruptzip");
        System.load(args[0]); File base=new File(args[1]),plan=new File(args[5]);
        Map<String,byte[]> linux=SteamConvertedTextures.restore(base,new File(args[2]),plan);
        Map<String,byte[]> windows=SteamConvertedTextures.restore(base,new File(args[3]),plan);
        if(linux.size()!=53||!linux.keySet().equals(windows.keySet()))throw new AssertionError("inventory differs");
        for(String name:linux.keySet())if(!MessageDigest.isEqual(linux.get(name),windows.get(name)))throw new AssertionError("platform differs: "+name);
        try{SteamConvertedTextures.restore(base,new File(args[2]),new File(args[4]));throw new AssertionError("bad plan accepted");}catch(IOException expected){System.out.println("rejected bad plan: "+expected.getMessage());}
        try{SteamConvertedTextures.restore(base,new File(args[7]),plan);throw new AssertionError("bad ZIP accepted");}catch(IOException expected){System.out.println("rejected bad ZIP: "+expected.getMessage());}
        Path dest=Paths.get(args[6]);Files.createDirectory(dest);
        for(Map.Entry<String,byte[]> r:linux.entrySet())Files.write(dest.resolve(r.getKey()),r.getValue(),StandardOpenOption.CREATE_NEW);
        System.out.println("PASS: 53 converted textures match across Linux and Windows");
    }
}
