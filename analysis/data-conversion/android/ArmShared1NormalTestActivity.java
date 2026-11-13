package com.openbonk.dataprep;
import android.app.Activity;
import android.os.Bundle;
import android.util.Log;
import java.io.File;

/** Private ARM64 reconstruction check; run one Steam platform at a time. */
public final class ArmShared1NormalTestActivity extends Activity {
    @Override public void onCreate(Bundle state){
        super.onCreate(state);String source=getIntent().getStringExtra("source");
        new Thread(()->{try{
            if(!"linux".equals(source)&&!"windows".equals(source))throw new IllegalArgumentException("invalid source");
            System.loadLibrary("texture");File dir=getExternalFilesDir(null);if(dir==null)throw new AssertionError("missing storage");
            File out=new File(dir,"normal.assets");SteamShared1ExactTextures.restore(new File(dir,"three.assets"),new File(dir,source+".zip"),new File(dir,"normal-plan.bin"),out);
            Log.i("OpenBonkArmNormal","PASS: ARM64 "+source+" all three normal maps reconstructed");
        }catch(Throwable e){Log.e("OpenBonkArmNormal","FAIL: "+source,e);}}).start();
    }
}
