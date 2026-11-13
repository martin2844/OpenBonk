package com.openbonk.dataprep;

import java.io.File;
import java.io.IOException;
import java.security.MessageDigest;
import java.util.Map;

public final class SteamSceneValuesCheck {
    public static void main(String[] a)throws Exception{
        if(a.length!=6&&a.length!=7)throw new IllegalArgumentException("base, Linux ZIP, Windows ZIP, mixed ZIP, bad plan, plan, optional output directory");
        File base=new File(a[0]),plan=new File(a[5]);
        Map<String,byte[]> linux=SteamSceneValues.restore(base,new File(a[1]),plan);
        Map<String,byte[]> windows=SteamSceneValues.restore(base,new File(a[2]),plan);
        if(linux.size()!=3||!linux.keySet().equals(windows.keySet()))throw new AssertionError("missing scenes");
        long total=0;for(String n:linux.keySet()){if(!MessageDigest.isEqual(linux.get(n),windows.get(n)))throw new AssertionError("platform output differs: "+n);total+=linux.get(n).length;}
        try{SteamSceneValues.restore(base,new File(a[3]),plan);throw new AssertionError("mixed sources accepted");}catch(IOException expected){System.out.println("Rejected mixed sources: "+expected.getMessage());}
        try{SteamSceneValues.restore(base,new File(a[2]),new File(a[4]));throw new AssertionError("bad plan accepted");}catch(IOException expected){System.out.println("Rejected bad plan: "+expected.getMessage());}
        if(a.length==7){
            File output=new File(a[6]),badOutput=new File(output.getParentFile(),output.getName()+"-bad");
            if(badOutput.exists())throw new IOException("bad-output test path exists");
            try{SteamSceneValues.restoreToDirectory(base,new File(a[3]),plan,badOutput);throw new AssertionError("mixed source published");}
            catch(IOException expected){if(badOutput.exists())throw new AssertionError("partial scene set published");}
            SteamSceneValues.restoreToDirectory(base,new File(a[2]),plan,output);
            for(String name:windows.keySet())if(!MessageDigest.isEqual(windows.get(name),java.nio.file.Files.readAllBytes(new File(output,name).toPath())))throw new AssertionError("published scene differs");
            try{SteamSceneValues.restoreToDirectory(base,new File(a[2]),plan,output);throw new AssertionError("existing output replaced");}catch(IOException expected){}
            System.out.println("PASS: complete scene set published; existing output preserved");
        }
        System.out.println("PASS: 3 scene value reconstructions; "+total+" bytes, Linux and Windows match");
    }
}
