package com.openbonk.dataprep;

import java.io.File;
import java.io.IOException;
import java.security.MessageDigest;
import java.util.Map;

public final class SteamAnimationResourcesCheck {
    public static void main(String[] a)throws Exception{
        if(a.length!=8)throw new IllegalArgumentException("base, Linux ZIP, Windows ZIP, corrupt ZIP, bad plan, texture plan, animation plan, expected count");
        File base=new File(a[0]),plan=new File(a[6]);
        Map<String,byte[]> linux=SteamAnimationResources.restore(base,new File(a[1]),plan);
        Map<String,byte[]> windows=SteamAnimationResources.restore(base,new File(a[2]),plan);
        int expectedCount=Integer.parseInt(a[7]);if(linux.size()!=expectedCount||!linux.keySet().equals(windows.keySet()))throw new AssertionError("missing animations");
        long total=0;for(String name:linux.keySet()){if(!MessageDigest.isEqual(linux.get(name),windows.get(name)))throw new AssertionError("platform output differs: "+name);total+=linux.get(name).length;}
        for(String path:new String[]{a[4],a[5]})try{SteamAnimationResources.restore(base,new File(a[2]),new File(path));throw new AssertionError("invalid plan accepted");}catch(IOException expected){System.out.println("Rejected plan: "+expected.getMessage());}
        try{SteamAnimationResources.restore(base,new File(a[3]),plan);throw new AssertionError("corrupt source accepted");}catch(IOException expected){System.out.println("Rejected corrupt source: "+expected.getMessage());}
        System.out.println("PASS: "+expectedCount+" animations, "+total+" bytes; Linux/Windows match");
    }
}
