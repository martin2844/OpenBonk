package com.openbonk.dataprep;

import android.app.Activity;
import android.os.Bundle;
import android.util.Log;
import java.io.File;
import java.nio.file.Files;
import java.security.MessageDigest;

/** Local-only end-to-end preparer test with ARM64 native conversion. */
public final class ArmPreparationTestActivity extends Activity {
    @Override public void onCreate(Bundle state) {
        super.onCreate(state);
        new Thread(() -> {
            try {
                System.loadLibrary("texture");
                File d=getExternalFilesDir(null);if(d==null)throw new AssertionError("missing app storage");
                File base=new File(d,"base.obb"),linux=new File(d,"linux.zip"),windows=new File(d,"windows.zip"),scene=new File(d,"scene.bin"),audio=new File(d,"audio.bin"),mesh=new File(d,"mesh.bin"),exact=new File(d,"exact.bin"),animation=new File(d,"animation.bin"),converted=new File(d,"converted.bin"),extra=new File(d,"extra.bin");
                File l=new File(d,"linux-output"),w=new File(d,"windows-output");long start=System.nanoTime();
                SteamPreparationPrototype.prepareWithDerivedTextures(base,linux,scene,audio,mesh,exact,animation,converted,extra,l);
                SteamPreparationPrototype.prepareWithDerivedTextures(base,windows,scene,audio,mesh,exact,animation,converted,extra,w);
                int count=0;
                for(String dir:new String[]{"scenes","audio","meshes","textures","animations","converted-textures","derived-extra-textures","video"}) {
                    File[] files=new File(l,dir).listFiles();if(files==null)throw new AssertionError("missing "+dir);
                    for(File f:files){File other=new File(new File(w,dir),f.getName());if(!other.isFile()||!MessageDigest.isEqual(Files.readAllBytes(f.toPath()),Files.readAllBytes(other.toPath())))throw new AssertionError("platform differs: "+f);++count;}
                }
                if(count!=339)throw new AssertionError("unexpected output count: "+count);
                Log.i("OpenBonkArmPreparation","PASS: ARM64 JNI 339 Linux/Windows outputs; elapsed ms="+(System.nanoTime()-start)/1000000);
            } catch(Throwable error) { Log.e("OpenBonkArmPreparation","FAIL",error); }
        }).start();
    }
}
