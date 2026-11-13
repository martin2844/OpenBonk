package com.openbonk.dataprep;

import java.io.File;
import java.io.IOException;
import java.nio.file.Files;
import java.util.Arrays;

public final class SteamShared0FontsCheck {
    public static void main(String[] args)throws Exception{
        if(args.length!=5)throw new IllegalArgumentException("base-apk steam-zip plan output expected (pass or error message)");
        File output=new File(args[3]);File dir=output.getAbsoluteFile().getParentFile();
        String[] before=dir.list((d,n)->n.startsWith(".openbonk-shared0-"));if(before==null)throw new IOException("cannot inspect output directory");Arrays.sort(before);
        try{
            SteamShared0Fonts.restore(new File(args[0]),new File(args[1]),new File(args[2]),output);
            if(!args[4].equals("pass"))throw new AssertionError("invalid input accepted");
            if(output.length()!=132028896L)throw new AssertionError("invalid result size");
        }catch(IOException e){if(args[4].equals("pass")||!args[4].equals(e.getMessage()))throw e;if(output.exists())throw new AssertionError("partial output published");}
        String[] after=dir.list((d,n)->n.startsWith(".openbonk-shared0-"));if(after==null)throw new IOException("cannot inspect output directory");Arrays.sort(after);
        if(!Arrays.equals(before,after))throw new AssertionError("temporary file leaked");
        System.out.println("PASS: "+args[4]);
    }
}
