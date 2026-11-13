package com.openbonk.dataprep;

import java.io.File;
import java.io.IOException;

public final class SteamVideoObbStageCheck {
    public static void main(String[] args) throws Exception {
        if(args.length!=5)throw new IllegalArgumentException("base OBB, Windows ZIP, Linux ZIP, corrupt ZIP, output directory required");
        File base=new File(args[0]),dir=new File(args[4]);
        File windows=new File(dir,"windows-video.obb"),linux=new File(dir,"linux-video.obb"),failed=new File(dir,"failed.obb");
        SteamVideoObbStage.restore(base,new File(args[1]),windows);
        SteamVideoObbStage.restore(base,new File(args[2]),linux);
        try { SteamVideoObbStage.restore(base,new File(args[3]),failed);throw new AssertionError("bad Steam ZIP accepted"); }
        catch(IOException expected) { if(failed.exists())throw new AssertionError("failed output published"); }
        try { SteamVideoObbStage.restore(base,new File(args[1]),windows);throw new AssertionError("existing output replaced"); }
        catch(IOException expected) { }
        System.out.println("PASS: Android video OBB stage, invalid source and existing output protection");
    }
}
