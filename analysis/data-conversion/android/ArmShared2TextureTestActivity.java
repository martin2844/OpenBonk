package com.openbonk.dataprep;
import android.app.Activity;
import android.os.Bundle;
import android.util.Log;
import java.io.File;
import java.security.MessageDigest;

/** Local-only single-platform ARM64 texture reconstruction test. */
public final class ArmShared2TextureTestActivity extends Activity {
    @Override public void onCreate(Bundle state){
        super.onCreate(state);
        final String source=getIntent().getStringExtra("source");
        new Thread(()->{
            try{
                if(!"linux".equals(source)&&!"windows".equals(source))throw new IllegalArgumentException("invalid source");
                System.loadLibrary("texture");File dir=getExternalFilesDir(null);if(dir==null)throw new AssertionError("missing app storage");
                File steam=new File(dir,source+".zip");long start=System.nanoTime();
                byte[] meshes=SteamShared2Meshes.restorePartial(new File(dir,"base.obb"),steam,new File(dir,"mesh-plan.bin"));
                byte[] result=SteamShared2Textures.restorePartial(meshes,steam,new File(dir,"texture-plan.bin"));
                String actual=SteamExactTextures.hex(MessageDigest.getInstance("SHA-256").digest(result));
                if(!actual.equals("8835049aa6e69f1fe689150ab004f749d9a8fb7bcbe0ac1176352b2d1d33e32f"))throw new AssertionError("result differs");
                Log.i("OpenBonkArmShared2","PASS: ARM64 "+source+" 72 textures including BC7; elapsed ms="+(System.nanoTime()-start)/1000000);
            }catch(Throwable error){Log.e("OpenBonkArmShared2","FAIL: "+source,error);}
        }).start();
    }
}
