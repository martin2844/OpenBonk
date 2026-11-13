package com.megabonk.launcher;
import java.io.*;
import java.nio.charset.StandardCharsets;
import java.nio.file.Files;
public final class PreparedSteamImportCheck {
 private static void write(File f,String s)throws IOException{Files.write(f.toPath(),s.getBytes(StandardCharsets.UTF_8));}
 private static String read(File f)throws IOException{return new String(Files.readAllBytes(f.toPath()),StandardCharsets.UTF_8);}
 private static InputStream stream(String s){return new ByteArrayInputStream(s.getBytes(StandardCharsets.UTF_8));}
 private static void require(boolean ok){if(!ok)throw new AssertionError();}
 private static void clean(File root)throws IOException{File[] files=root.listFiles((d,n)->n.startsWith("openbonk-steam-import-")||n.startsWith("openbonk-generated-obb-"));if(files==null||files.length!=0)throw new AssertionError("temporary file leaked");}
 public static void main(String[] a)throws Exception{
  File root=Files.createTempDirectory("openbonk-prepared-import-").toFile(),obb=new File(root,"data.obb"),steam=new File(root,"steam.zip");
  SplitFileRecovery.Validator zip=f->{try{return read(f).equals("steam new");}catch(IOException e){return false;}};
  SplitFileRecovery.Validator data=f->{try{return read(f).equals("obb new");}catch(IOException e){return false;}};
  LauncherPreparedSteamImport.Generator generator=(input,output)->{if(!zip.valid(input)||output.exists())throw new IOException("bad generator inputs");write(output,"obb new");};
  try{
   write(obb,"obb old");write(steam,"steam old");
   try{LauncherPreparedSteamImport.install(stream("broken"),root,obb,steam,zip,data,1024,generator);throw new AssertionError("accepted invalid ZIP");}catch(IOException expected){require(read(obb).equals("obb old")&&read(steam).equals("steam old"));}clean(root);
   try{LauncherPreparedSteamImport.install(stream("steam new"),root,obb,steam,zip,data,1024,(input,output)->{write(output,"partial");throw new IOException("failed generation");});throw new AssertionError("accepted failed generation");}catch(IOException expected){require(read(obb).equals("obb old")&&read(steam).equals("steam old"));}clean(root);
   try{LauncherPreparedSteamImport.install(stream("steam new"),root,obb,steam,zip,data,1024,(input,output)->{write(output,"partial");throw new UnsatisfiedLinkError("converter symbol missing");});throw new AssertionError("accepted native linkage failure");}catch(IOException expected){require("native converter unavailable".equals(expected.getMessage()) && expected.getCause() instanceof UnsatisfiedLinkError && read(obb).equals("obb old") && read(steam).equals("steam old"));}clean(root);
   try{LauncherPreparedSteamImport.install(stream("steam new"),root,obb,steam,zip,data,1024,(input,output)->write(output,"bad obb"));throw new AssertionError("accepted invalid OBB");}catch(IOException expected){require(read(obb).equals("obb old")&&read(steam).equals("steam old"));}clean(root);
   try{LauncherPreparedSteamImport.install(stream("steam new"),root,obb,steam,zip,data,3,generator);throw new AssertionError("accepted oversized ZIP");}catch(IOException expected){require(read(obb).equals("obb old")&&read(steam).equals("steam old"));}clean(root);
   LauncherPreparedSteamImport.install(stream("steam new"),root,obb,steam,zip,data,1024,generator);require(read(obb).equals("obb new")&&read(steam).equals("steam new"));clean(root);
   System.out.println("PASS: ZIP validation, generation failures, size limit, cleanup and atomic prepared-pair install");
  }finally{File[] files=root.listFiles();if(files!=null)for(File f:files)f.delete();root.delete();}
 }
}
