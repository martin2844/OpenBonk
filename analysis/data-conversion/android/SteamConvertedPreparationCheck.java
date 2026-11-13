package com.openbonk.dataprep;

import java.io.*;
import java.nio.file.*;
import java.security.MessageDigest;

public final class SteamConvertedPreparationCheck {
    public static void main(String[] a)throws Exception {
        if(a.length!=12)throw new IllegalArgumentException("native base linux windows scenes audio mesh exact animations converted badplan parent");
        System.load(a[0]);File base=new File(a[1]),linux=new File(a[2]),windows=new File(a[3]),scenes=new File(a[4]),audio=new File(a[5]),mesh=new File(a[6]),exact=new File(a[7]),animations=new File(a[8]),converted=new File(a[9]),badPlan=new File(a[10]),parent=new File(a[11]);
        File bad=new File(parent,"invalid"),l=new File(parent,"linux"),w=new File(parent,"windows");
        try {SteamPreparationPrototype.prepareWithConvertedTextures(base,windows,scenes,audio,mesh,exact,animations,badPlan,bad);throw new AssertionError("bad plan accepted");}
        catch(IOException expected){if(bad.exists())throw new AssertionError("partial output published");System.out.println("rejected bad plan: "+expected.getMessage());}
        SteamPreparationPrototype.prepareWithConvertedTextures(base,linux,scenes,audio,mesh,exact,animations,converted,l);
        SteamPreparationPrototype.prepareWithConvertedTextures(base,windows,scenes,audio,mesh,exact,animations,converted,w);
        int count=0;
        for(String dir:new String[]{"scenes","audio","meshes","textures","animations","converted-textures","video"}) {
            File[] files=new File(l,dir).listFiles(); if(files==null)throw new AssertionError("missing output: "+dir);
            for(File f:files){File other=new File(new File(w,dir),f.getName());if(!other.isFile()||!MessageDigest.isEqual(Files.readAllBytes(f.toPath()),Files.readAllBytes(other.toPath())))throw new AssertionError("output differs: "+f);++count;}
        }
        if(count!=314)throw new AssertionError("unexpected output count: "+count);
        try{SteamPreparationPrototype.prepareWithConvertedTextures(base,windows,scenes,audio,mesh,exact,animations,converted,w);throw new AssertionError("output replaced");}catch(IOException expected){}
        File[] leftovers=parent.listFiles((dir,name)->name.startsWith(".openbonk-preparation-"));
        if(leftovers==null||leftovers.length!=0)throw new AssertionError("temporary output left behind");
        System.out.println("PASS: 314 scene/audio/mesh/texture/animation/video outputs match across Steam platforms");
    }
}
