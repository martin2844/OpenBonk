package com.openbonk.dataprep;
import android.app.Activity;
import android.os.Bundle;
import android.util.Log;
import java.io.File;
public final class ArmCubemapTestActivity extends Activity {
    @Override public void onCreate(Bundle state){
        super.onCreate(state);String source=getIntent().getStringExtra("source");
        new Thread(()->{try{
            if(!"linux".equals(source)&&!"windows".equals(source))throw new IllegalArgumentException("invalid source");System.loadLibrary("texture");File dir=getExternalFilesDir(null);if(dir==null)throw new AssertionError("missing storage");
            SteamCubemapResources.restore(new File(dir,"base.assets"),new File(dir,source+".zip"),new File(dir,"plan.bin"),new File(dir,"result.assets"));Log.i("OpenBonkArmCube","PASS: ARM64 "+source+" seven cubemaps reconstructed");
        }catch(Throwable e){Log.e("OpenBonkArmCube","FAIL: "+source,e);}}).start();
    }
}
