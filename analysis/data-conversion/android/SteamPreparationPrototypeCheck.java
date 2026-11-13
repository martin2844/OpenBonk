package com.openbonk.dataprep;

import java.io.File;
import java.io.IOException;
import java.nio.file.Files;
import java.security.MessageDigest;
import java.util.concurrent.CountDownLatch;
import java.util.concurrent.atomic.AtomicInteger;
import java.util.concurrent.atomic.AtomicReference;

public final class SteamPreparationPrototypeCheck {
    private static void fail(File base,File zip,File scenes,File audio,File mesh,File texture,File animation,File output)throws Exception{
        try{SteamPreparationPrototype.prepare(base,zip,scenes,audio,mesh,texture,animation,output);throw new AssertionError("invalid preparation accepted");}
        catch(IOException expected){if(output.exists())throw new AssertionError("partial output published");System.out.println("Rejected: "+expected.getMessage());}
    }
    public static void main(String[] a)throws Exception{
        if(a.length!=14)throw new IllegalArgumentException("base, Linux ZIP, Windows ZIP, mixed ZIP, partial plan, scene plan, audio plan, mesh plan, bad mesh plan, texture plan, bad texture plan, animation plan, bad animation plan, output parent");
        File base=new File(a[0]),linux=new File(a[1]),windows=new File(a[2]),mixed=new File(a[3]),partial=new File(a[4]),scenes=new File(a[5]),audio=new File(a[6]),mesh=new File(a[7]),badMesh=new File(a[8]),texture=new File(a[9]),badTexture=new File(a[10]),animation=new File(a[11]),badAnimation=new File(a[12]),parent=new File(a[13]);
        File l=new File(parent,"linux"),w=new File(parent,"windows"),bad=new File(parent,"invalid");
        fail(base,mixed,scenes,audio,mesh,texture,animation,bad);fail(base,windows,partial,audio,mesh,texture,animation,bad);fail(base,windows,scenes,audio,badMesh,texture,animation,bad);fail(base,windows,scenes,audio,mesh,badTexture,animation,bad);fail(base,windows,scenes,audio,mesh,texture,badAnimation,bad);
        SteamPreparationPrototype.prepare(base,linux,scenes,audio,mesh,texture,animation,l);
        SteamPreparationPrototype.prepare(base,windows,scenes,audio,mesh,texture,animation,w);
        int count=0;
        for(String folder:new String[]{"scenes","audio","meshes","textures","animations","video"}){
            File[] files=new File(l,folder).listFiles();if(files==null)throw new AssertionError("missing output");
            for(File f:files){File peer=new File(new File(w,folder),f.getName());if(!peer.isFile()||!MessageDigest.isEqual(Files.readAllBytes(f.toPath()),Files.readAllBytes(peer.toPath())))throw new AssertionError("output differs: "+f);count++;}
        }
        if(count!=261)throw new AssertionError("unexpected output count");
        try{SteamPreparationPrototype.prepare(base,windows,scenes,audio,mesh,texture,animation,w);throw new AssertionError("existing output replaced");}catch(IOException expected){}
        File raced=new File(parent,"raced");CountDownLatch start=new CountDownLatch(1);AtomicInteger success=new AtomicInteger(),rejected=new AtomicInteger();AtomicReference<Throwable> error=new AtomicReference<>();
        Runnable attempt=()->{try{start.await();SteamPreparationPrototype.prepare(base,windows,scenes,audio,mesh,texture,animation,raced);success.incrementAndGet();}catch(IOException expected){rejected.incrementAndGet();}catch(Throwable unexpected){error.set(unexpected);}};
        Thread first=new Thread(attempt),second=new Thread(attempt);first.start();second.start();start.countDown();first.join();second.join();
        if(error.get()!=null||success.get()!=1||rejected.get()!=1||!new File(raced,"scenes/level1").isFile())throw new AssertionError("concurrent publication failed",error.get());
        File[] leftovers=parent.listFiles((dir,name)->name.startsWith(".openbonk-preparation-"));if(leftovers==null||leftovers.length!=0)throw new AssertionError("temporary output left behind");
        System.out.println("PASS: scene/audio/mesh/texture/animation/video preparation; 261 Linux/Windows outputs match; concurrent publish protected");
    }
}
