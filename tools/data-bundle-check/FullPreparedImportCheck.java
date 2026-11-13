package com.megabonk.launcher;
import com.openbonk.dataprep.SteamFullDiagnosticPreparation;
import java.io.*;
import java.nio.charset.StandardCharsets;
import java.nio.file.*;
import java.nio.file.attribute.BasicFileAttributes;
import java.security.*;
import java.util.zip.ZipFile;
public final class FullPreparedImportCheck {
 private static String sha(File file)throws Exception{MessageDigest h=MessageDigest.getInstance("SHA-256");byte[] b=new byte[65536];try(InputStream in=new FileInputStream(file)){for(int n;(n=in.read(b))!=-1;)if(n>0)h.update(b,0,n);}StringBuilder s=new StringBuilder();for(byte v:h.digest())s.append(String.format(java.util.Locale.ROOT,"%02x",v&255));return s.toString();}
 private static void remove(Path p)throws IOException{if(!Files.exists(p,LinkOption.NOFOLLOW_LINKS))return;Files.walkFileTree(p,new SimpleFileVisitor<Path>(){@Override public FileVisitResult visitFile(Path f,BasicFileAttributes a)throws IOException{Files.delete(f);return FileVisitResult.CONTINUE;}@Override public FileVisitResult postVisitDirectory(Path d,IOException e)throws IOException{if(e!=null)throw e;Files.delete(d);return FileVisitResult.CONTINUE;}});}
 public static void main(String[] a)throws Exception{
  if(a.length!=6)throw new IllegalArgumentException("library base-apk base-obb steam plan-dir work-parent");System.load(a[0]);File baseApk=new File(a[1]),baseObb=new File(a[2]),original=new File(a[3]),plans=new File(a[4]);Path work=Files.createTempDirectory(new File(a[5]).toPath(),".openbonk-full-import-test-");
  try{
   File cache=work.resolve("cache").toFile(),obb=work.resolve("main.obb").toFile(),steam=work.resolve("steam.zip").toFile();if(!cache.mkdir())throw new IOException("cannot create cache");Files.write(obb.toPath(),"old obb".getBytes(StandardCharsets.UTF_8));Files.write(steam.toPath(),"old zip".getBytes(StandardCharsets.UTF_8));String steamHash=sha(original);
   SplitFileRecovery.Validator obbValidator=f->{try{if(!sha(f).equals("ceb0bc511c8ce043761733ab7486448e8dcefddace0183ad8fb28e1d420091b9"))return false;try(ZipFile z=new ZipFile(f)){return z.size()==1081;}}catch(Exception e){return false;}};
   try(InputStream in=new FileInputStream(original)){LauncherPreparedSteamImport.install(in,cache,obb,steam,OriginalGameDataValidator::valid,obbValidator,1024L*1024*1024,(zip,output)->SteamFullDiagnosticPreparation.prepare(baseApk,baseObb,zip,plans,output,label->System.out.println("STAGE: "+label)));}
   if(!obbValidator.valid(obb)||!OriginalGameDataValidator.valid(steam)||!steamHash.equals(sha(steam))||!steamHash.equals(sha(original)))throw new AssertionError("installed pair differs");
   String[] leftovers=cache.list();if(leftovers==null||leftovers.length!=0)throw new AssertionError("cache files leaked");
   if(SplitFileRecovery.backupFor(obb).exists()||SplitFileRecovery.backupFor(steam).exists()||new File(work.toFile(),"steam.zip.bundle.transaction").exists())throw new AssertionError("backup or marker leaked");
   System.out.println("PASS: selected Steam ZIP reconstructed and atomically installed with diagnostic OBB");
  }finally{remove(work);}
 }
}
