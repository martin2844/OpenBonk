package com.megabonk.launcher;
import android.content.Context;
import java.io.*;
import java.nio.file.*;
import java.security.MessageDigest;
import java.util.Arrays;
import java.util.stream.Stream;

/** Private host-only exercise of the launcher preparation sequence. */
public final class LocalizationPipelineIntegrationCheck {
    private static byte[] hash(Path p)throws Exception{MessageDigest d=MessageDigest.getInstance("SHA-256");try(InputStream in=Files.newInputStream(p)){byte[] b=new byte[65536];for(int n;(n=in.read(b))!=-1;)if(n>0)d.update(b,0,n);}return d.digest();}
    public static void main(String[] args)throws Exception{
        if(args.length!=4)throw new IllegalArgumentException("assets scratch steam-zip expected-obb");
        Path assets=Paths.get(args[0]),scratch=Paths.get(args[1]);Files.createDirectory(scratch);Context context=new Context(assets,scratch.toFile());
        Path output=scratch.getParent().resolve("locale-complete.obb");
        new LauncherDiagnosticGenerator(context,null).prepare(new File(args[2]),output.toFile());
        if(!Arrays.equals(hash(output),hash(Paths.get(args[3]))))throw new AssertionError("result differs");
        Files.delete(output);
        Path check=assets.resolve("openbonk-diagnostic/localization-plan.sha256");byte[] old=Files.readAllBytes(check);
        try{
            Files.write(check,"0000000000000000000000000000000000000000000000000000000000000000".getBytes("US-ASCII"));
            try{new LauncherDiagnosticGenerator(context,null).prepare(new File(args[2]),output.toFile());throw new AssertionError("bad plan hash accepted");}
            catch(IOException expected){if(Files.exists(output))throw new AssertionError("output left after failure");}
        }finally{Files.write(check,old);}
        try(Stream<Path> children=Files.list(context.getFilesDir().toPath())){if(children.findAny().isPresent())throw new AssertionError("temporary files retained");}
        System.out.println("PASS: V9 launcher restored expected OBB and rejected altered plan hash");
    }
}
