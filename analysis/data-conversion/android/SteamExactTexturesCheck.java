package com.openbonk.dataprep;

import java.io.File;
import java.io.IOException;
import java.security.MessageDigest;
import java.util.Map;

public final class SteamExactTexturesCheck {
    public static void main(String[] a)throws Exception{
        if(a.length!=7&&a.length!=8)throw new IllegalArgumentException("base, Linux ZIP, Windows ZIP, corrupt ZIP, bad plan, plan, output, optional foreign plan");
        File base=new File(a[0]),plan=new File(a[5]);
        Map<String,byte[]> linux=SteamExactTextures.restore(base,new File(a[1]),plan);
        Map<String,byte[]> windows=SteamExactTextures.restore(base,new File(a[2]),plan);
        if(linux.size()!=56||!linux.keySet().equals(windows.keySet()))throw new AssertionError("missing textures");
        long total=0;for(String name:linux.keySet()){if(!MessageDigest.isEqual(linux.get(name),windows.get(name)))throw new AssertionError("platform output differs: "+name);total+=linux.get(name).length;}
        try{SteamExactTextures.restore(base,new File(a[3]),plan);throw new AssertionError("corrupt source accepted");}catch(IOException expected){System.out.println("Rejected corrupt source: "+expected.getMessage());}
        try{SteamExactTextures.restore(base,new File(a[2]),new File(a[4]));throw new AssertionError("bad plan accepted");}catch(IOException expected){System.out.println("Rejected bad plan: "+expected.getMessage());}
        if(a.length==8)try{SteamExactTextures.restore(base,new File(a[2]),new File(a[7]));throw new AssertionError("foreign plan accepted");}catch(IOException expected){System.out.println("Rejected foreign plan: "+expected.getMessage());}
        File output=new File(a[6]),bad=new File(output.getParentFile(),output.getName()+"-bad");
        if(bad.exists())throw new IOException("bad-output test path exists");
        try{SteamExactTextures.restoreToDirectory(base,new File(a[3]),plan,bad);throw new AssertionError("corrupt source published");}catch(IOException expected){if(bad.exists())throw new AssertionError("partial texture output published");}
        SteamExactTextures.restoreToDirectory(base,new File(a[2]),plan,output);
        for(String name:windows.keySet())if(!MessageDigest.isEqual(windows.get(name),java.nio.file.Files.readAllBytes(new File(output,name).toPath())))throw new AssertionError("published texture differs");
        try{SteamExactTextures.restoreToDirectory(base,new File(a[2]),plan,output);throw new AssertionError("existing output replaced");}catch(IOException expected){}
        System.out.println("PASS: 56 exact textures, "+total+" bytes; publication and rollback");
    }
}
