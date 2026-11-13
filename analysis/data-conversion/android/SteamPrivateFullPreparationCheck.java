package com.openbonk.dataprep;
import java.io.*;
import java.nio.file.*;
import java.security.MessageDigest;

public final class SteamPrivateFullPreparationCheck {
    public static void main(String[] a)throws Exception{
        if(a.length!=13)throw new IllegalArgumentException("library base linux windows scene audio mesh exact privateAnimation converted privateExtra badExtra parent");
        System.load(a[0]);File base=new File(a[1]),linux=new File(a[2]),windows=new File(a[3]),scene=new File(a[4]),audio=new File(a[5]),mesh=new File(a[6]),exact=new File(a[7]),anim=new File(a[8]),converted=new File(a[9]),extra=new File(a[10]),badExtra=new File(a[11]),parent=new File(a[12]);
        File bad=new File(parent,"invalid"),l=new File(parent,"linux"),w=new File(parent,"windows");
        try{SteamPreparationPrototype.preparePrivateExtendedDiagnostic(base,windows,scene,audio,mesh,exact,anim,converted,badExtra,bad);throw new AssertionError("bad plan accepted");}catch(IOException expected){if(bad.exists())throw new AssertionError("partial output published");System.out.println("rejected bad extra plan: "+expected.getMessage());}
        SteamPreparationPrototype.preparePrivateExtendedDiagnostic(base,linux,scene,audio,mesh,exact,anim,converted,extra,l);
        SteamPreparationPrototype.preparePrivateExtendedDiagnostic(base,windows,scene,audio,mesh,exact,anim,converted,extra,w);
        int count=0;
        for(String dir:new String[]{"scenes","audio","meshes","textures","animations","converted-textures","private-extra-textures","video"}){
            File[] files=new File(l,dir).listFiles();if(files==null)throw new AssertionError("missing "+dir);
            for(File f:files){File other=new File(new File(w,dir),f.getName());if(!other.isFile()||!MessageDigest.isEqual(Files.readAllBytes(f.toPath()),Files.readAllBytes(other.toPath())))throw new AssertionError("platform differs: "+f);++count;}
        }
        if(count!=422)throw new AssertionError("unexpected count: "+count);
        File[] leftovers=parent.listFiles((dir,name)->name.startsWith(".openbonk-preparation-"));if(leftovers==null||leftovers.length!=0)throw new AssertionError("temporary output left behind");
        System.out.println("PASS: 422 private diagnostic assets match across Linux and Windows");
    }
}
