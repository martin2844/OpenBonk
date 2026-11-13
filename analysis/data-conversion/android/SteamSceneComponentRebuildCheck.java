package com.openbonk.dataprep;
import java.io.*;
import java.nio.file.*;

public final class SteamSceneComponentRebuildCheck {
    public static void main(String[] args)throws Exception{
        if(args.length!=5)throw new IllegalArgumentException("template steam plan output pass|reject");
        File out=new File(args[3]);
        try{
            SteamSceneComponentRebuild.restore(new File(args[0]),new File(args[1]),new File(args[2]),out);
            if(!args[4].equals("pass"))throw new AssertionError("invalid input accepted");
            System.out.println("PASS: exact scene base restored");
        }catch(IOException e){
            if(!args[4].equals("reject")||Files.exists(out.toPath(),LinkOption.NOFOLLOW_LINKS))throw e;
            System.out.println("PASS: invalid scene input rejected without output");
        }
    }
}
