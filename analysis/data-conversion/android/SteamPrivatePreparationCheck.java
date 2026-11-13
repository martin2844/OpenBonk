package com.openbonk.dataprep;

import java.io.File;
import java.io.IOException;
import java.nio.file.Files;
import java.security.MessageDigest;

/** Private research test; its plan contains Android-derived bytes. */
public final class SteamPrivatePreparationCheck {
    public static void main(String[] a)throws Exception{
        if(a.length!=9)throw new IllegalArgumentException("base, Linux ZIP, Windows ZIP, scene plan, audio plan, mesh plan, texture plan, private animation plan, output parent");
        File base=new File(a[0]),linux=new File(a[1]),windows=new File(a[2]),scenes=new File(a[3]),audio=new File(a[4]),mesh=new File(a[5]),texture=new File(a[6]),animation=new File(a[7]),parent=new File(a[8]);
        File l=new File(parent,"linux"),w=new File(parent,"windows"),bad=new File(parent,"ordinary-must-reject");
        try{SteamPreparationPrototype.prepare(base,windows,scenes,audio,mesh,texture,animation,bad);throw new AssertionError("ordinary preparation accepted private plan");}
        catch(IOException expected){if(bad.exists())throw new AssertionError("private data published in ordinary mode");System.out.println("Ordinary mode rejected private plan: "+expected.getMessage());}
        SteamPreparationPrototype.preparePrivateAnimationDiagnostic(base,linux,scenes,audio,mesh,texture,animation,l);
        SteamPreparationPrototype.preparePrivateAnimationDiagnostic(base,windows,scenes,audio,mesh,texture,animation,w);
        int count=0;
        for(String folder:new String[]{"scenes","audio","meshes","textures","animations","video"}){
            File[] files=new File(l,folder).listFiles();if(files==null)throw new AssertionError("missing output");
            for(File f:files){File peer=new File(new File(w,folder),f.getName());if(!peer.isFile()||!MessageDigest.isEqual(Files.readAllBytes(f.toPath()),Files.readAllBytes(peer.toPath())))throw new AssertionError("output differs: "+f);count++;}
        }
        if(count!=344||new File(w,"animations").list().length!=108)throw new AssertionError("unexpected output count");
        try{SteamPreparationPrototype.preparePrivateAnimationDiagnostic(base,windows,scenes,audio,mesh,texture,animation,w);throw new AssertionError("existing output replaced");}catch(IOException expected){}
        File[] leftovers=parent.listFiles((dir,name)->name.startsWith(".openbonk-preparation-"));if(leftovers==null||leftovers.length!=0)throw new AssertionError("temporary output left behind");
        System.out.println("PASS: private diagnostic; 344 Linux/Windows outputs match, including 108 animations");
    }
}
