package com.openbonk.dataprep;

import android.app.Activity;
import android.os.Bundle;
import android.util.Log;
import java.io.File;
import java.security.MessageDigest;
import java.util.Map;

/** Local-only ARM64 JNI test activity; its input files are private Steam data. */
public final class ArmTextureTestActivity extends Activity {
    private static native String nativeAbi();
    @Override public void onCreate(Bundle state) {
        super.onCreate(state);
        new Thread(() -> {
            try {
                System.loadLibrary("texture");
                if(!"arm64".equals(nativeAbi()))throw new AssertionError("library is not ARM64");
                File dir=getExternalFilesDir(null);if(dir==null)throw new AssertionError("external app storage unavailable");
                File base=new File(dir,"base.obb"),plan=new File(dir,"plan.bin");
                long begin=System.nanoTime();
                Map<String,byte[]> linux=SteamConvertedTextures.restore(base,new File(dir,"linux.zip"),plan);
                Map<String,byte[]> windows=SteamConvertedTextures.restore(base,new File(dir,"windows.zip"),plan);
                if(linux.size()!=53||!linux.keySet().equals(windows.keySet()))throw new AssertionError("texture count differs");
                for(String name:linux.keySet())if(!MessageDigest.isEqual(linux.get(name),windows.get(name)))throw new AssertionError("platform output differs");
                File extraPlan=new File(dir,"extra-plan.bin");
                Map<String,byte[]> extraLinux=SteamExtraTextures.restore(base,new File(dir,"linux.zip"),extraPlan);
                Map<String,byte[]> extraWindows=SteamExtraTextures.restore(base,new File(dir,"windows.zip"),extraPlan);
                if(extraLinux.size()!=25||!extraLinux.keySet().equals(extraWindows.keySet()))throw new AssertionError("extra texture count differs");
                for(String name:extraLinux.keySet())if(!MessageDigest.isEqual(extraLinux.get(name),extraWindows.get(name)))throw new AssertionError("extra platform output differs");
                File mapPlan=new File(dir,"map-plan.bin");
                Map<String,byte[]> mapLinux=SteamShared4MapAsset.restore(base,new File(dir,"linux.zip"),mapPlan),mapWindows=SteamShared4MapAsset.restore(base,new File(dir,"windows.zip"),mapPlan);
                if(mapLinux.size()!=2||!mapLinux.keySet().equals(mapWindows.keySet()))throw new AssertionError("map split count differs");
                for(String name:mapLinux.keySet())if(!MessageDigest.isEqual(mapLinux.get(name),mapWindows.get(name)))throw new AssertionError("map platform output differs");
                Log.i("OpenBonkArmTextureTest","PASS: ARM64 JNI 78 textures and final-map shared asset from both Steam ZIPs; elapsed ms="+(System.nanoTime()-begin)/1000000);
            } catch(Throwable error) { Log.e("OpenBonkArmTextureTest","FAIL",error); }
        }).start();
    }
}
