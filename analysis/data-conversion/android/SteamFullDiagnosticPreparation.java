package com.openbonk.dataprep;
import java.io.*;
import java.nio.file.*;
import java.nio.file.attribute.BasicFileAttributes;
import java.security.*;

/** End-to-end private research preparation. Inputs and output are not cleared for distribution. */
public final class SteamFullDiagnosticPreparation {
 private static final String RESULT="ceb0bc511c8ce043761733ab7486448e8dcefddace0183ad8fb28e1d420091b9";
 private static final String BANDIT_PLAN="4c7f8b9ac3561896589ceae08e074a6e6a0f0216e9347fcfc8e05ef52d62da0a",BANDIT_RESULT="576d0f7edf17c550d6ea3af4c623604c54984fbdc1c7a9c8d71839dc6aa6568a";
 private static final String KNIGHT_PLAN="5234be766f35adcef52b6b8114d0273f0be1f5a60d3e89d78e9e90a14e12ba89",KNIGHT_RESULT="30c2ee4f9c979a6dd0a963e0fa83967225f7b8eb68c3425e0bf739f7455db99d";
 private static final String BATCH_PLAN="4c31e0fa6c7c9b95bfbbccac7cc2786e6ed5e5d4d6faa59ddaf4b0a4f6528cfb",BATCH_RESULT="d81d59963ef4f7664638f59b96fb5c6735b12a4b4ae340d0edbc8cbd5731e564";
 private static final String BATCH2_PLAN="c744c59fa5d60e6f73285e8277c7615db8fd38201298d6466ca7d4bde47a04a7",BATCH2_RESULT="334ca54ad010cb754b11e548bd5401d83dc7f4f7853aa9c6a294692e248d1fc1";
 private static final String BATCH3_PLAN="857857af2a38b4452112d58c8e157f2448a5e8b58838159ef93b762676d64a33",BATCH3_RESULT="a2c93164b591dc75c67b4cd2bd1f67bc3ede3c68a869559a4c68b9959fb4d65f";
 private static final String SHARED_PLAN="72b2f5bbff0e255c2fa14af35d73b22a2c81f65bb5725cc92c00e09cffd3a975",SHARED_RESULT="632b8fca82f42c8971e5484a32d704568b345c6baa304a9254435f848ac94a29";
 private static final String[] FIRST={"scene","audio","mesh","exact","animation-direct","converted","extra-derived","partial","shared-audio1","shared-audio2","shared-audio4","map","shared2-mesh","shared2-texture","shared2-animation","shared2-final","shared0-font","shared0-atlas","shared0-metadata","shared0-final"};
 private static final String[] SHARED1={"shared1-layout","shared1-material","shared1-mesh","shared1-mesh-metadata","shared1-mesh-flags","shared1-mesh-stream","shared1-exact","shared1-rgba","shared1-converted","shared1-etc","shared1-bc-rgba","shared1-alias","shared1-matched","shared1-alpha","shared1-large","shared1-renamed","shared1-last","shared1-normal","shared1-shaders","shared1-animation","shared1-cubemap","shared1-outline","shared1-final"};
 private static final String[] LAST={"localization","localization-obb","animation-private","builtin-shaders","preload"};
 public interface Progress {void stage(String label);}
 private static File plan(Path directory,String name)throws IOException{Path p=directory.resolve(name+".bin");if(Files.isSymbolicLink(p)||!Files.isRegularFile(p,LinkOption.NOFOLLOW_LINKS))throw new IOException("missing preparation input: "+name);return p.toFile();}
 private static File resource(Path directory,String name)throws IOException{Path p=directory.resolve(name);if(Files.isSymbolicLink(p)||!Files.isRegularFile(p,LinkOption.NOFOLLOW_LINKS))throw new IOException("missing preparation input: "+name);return p.toFile();}
 private static void update(Progress progress,String label){if(progress!=null)progress.stage(label);}
 private static String sha(Path path)throws IOException{try{MessageDigest h=MessageDigest.getInstance("SHA-256");byte[] b=new byte[65536];try(InputStream in=Files.newInputStream(path)){for(int n;(n=in.read(b))!=-1;)if(n>0)h.update(b,0,n);}return SteamExactTextures.hex(h.digest());}catch(NoSuchAlgorithmException e){throw new AssertionError(e);}}
 private static void remove(Path root)throws IOException{if(!Files.exists(root,LinkOption.NOFOLLOW_LINKS))return;Files.walkFileTree(root,new SimpleFileVisitor<Path>(){@Override public FileVisitResult visitFile(Path file,BasicFileAttributes a)throws IOException{Files.delete(file);return FileVisitResult.CONTINUE;}@Override public FileVisitResult postVisitDirectory(Path dir,IOException error)throws IOException{if(error!=null)throw error;Files.delete(dir);return FileVisitResult.CONTINUE;}});}
 public static synchronized void prepare(File shared0Base,File baseObb,File steam,File inputDirectory,File output,Progress progress)throws IOException{
  Path dir=inputDirectory.toPath().toAbsolutePath(),dest=output.toPath().toAbsolutePath(),parent=dest.getParent();
  if(Files.isSymbolicLink(dir)||!Files.isDirectory(dir,LinkOption.NOFOLLOW_LINKS)||parent==null||!Files.isDirectory(parent)||Files.exists(dest,LinkOption.NOFOLLOW_LINKS))throw new IOException("invalid input directory or output");
  update(progress,"Verificando archivos del ZIP");SteamDiagnosticInputCheck.validate(steam);
  File[] a=new File[FIRST.length],b=new File[SHARED1.length],c=new File[LAST.length];for(int i=0;i<a.length;i++)a[i]=plan(dir,FIRST[i]);for(int i=0;i<b.length;i++)b[i]=plan(dir,SHARED1[i]);for(int i=0;i<c.length;i++)c[i]=plan(dir,LAST[i]);File shaderPack=resource(dir,"shared1-shader-pack.zip"),builtinPack=resource(dir,"builtin-shader-pack.zip");
  Path temp=Files.createTempDirectory(parent,".openbonk-full-diagnostic-"),prepared=temp.resolve("prepared"),partial=temp.resolve("partial.obb"),shared0=temp.resolve("shared0.assets"),obb0=temp.resolve("shared0.obb"),shared1=temp.resolve("shared1.assets"),obb1=temp.resolve("shared1.obb"),locales=temp.resolve("locales.obb"),animations=temp.resolve("animations.obb"),builtin=temp.resolve("builtin.obb"),result=temp.resolve("complete.obb");
  try{
   update(progress,"Preparando recursos originales");File normalizedBase=baseObb;
   if(sha(normalizedBase.toPath()).equals(SteamMaterialRebuild.SCRUBBED)){File materialBase=temp.resolve("materials-restored-base.obb").toFile();SteamMaterialRebuild.restore(normalizedBase,steam,resource(dir,"material-plan.bin"),materialBase);normalizedBase=materialBase;}
   if(sha(normalizedBase.toPath()).equals(SteamSharedComponentRebuild.SCRUBBED)){File sharedBase=temp.resolve("shared-components-restored-base.obb").toFile();SteamSharedComponentRebuild.restore(normalizedBase,steam,resource(dir,"shared-component-plan.bin"),sharedBase);normalizedBase=sharedBase;}
   if(sha(normalizedBase.toPath()).equals(SteamSceneComponentRebuild.SCRUBBED)){File sceneBase=temp.resolve("scene-components-restored-base.obb").toFile();SteamSceneComponentRebuild.restore(normalizedBase,steam,resource(dir,"scene-component-plan.bin"),sceneBase);normalizedBase=sceneBase;}
   if(sha(normalizedBase.toPath()).equals(SteamShared1ParticleRebuild.SCRUBBED)){File particleBase=temp.resolve("shared1-particle-restored-base.obb").toFile();SteamShared1ParticleRebuild.restore(normalizedBase,steam,particleBase);normalizedBase=particleBase;}
   if(sha(normalizedBase.toPath()).equals(SteamShared2ParticleRebuild.SCRUBBED)){File particleBase=temp.resolve("particle-restored-base.obb").toFile();SteamShared2ParticleRebuild.restore(normalizedBase,steam,particleBase);normalizedBase=particleBase;}
   if(sha(normalizedBase.toPath()).equals(SteamShared2PartialAnimationRebuild.SCRUBBED)){File partialAnimationBase=temp.resolve("partial-animation-restored-base.obb").toFile();SteamShared2PartialAnimationRebuild.restore(normalizedBase,steam,partialAnimationBase);normalizedBase=partialAnimationBase;}
   if(sha(normalizedBase.toPath()).equals(SteamChestBaseRebuild.SCRUBBED)){File chestBase=temp.resolve("chest-restored-base.obb").toFile();SteamChestBaseRebuild.restore(normalizedBase,steam,chestBase);normalizedBase=chestBase;}
   File restoredBase=temp.resolve("restored-base.obb").toFile();SteamDiagnosticBaseRebuild.restore(normalizedBase,steam,restoredBase);
   SteamPreparationPrototype.prepareWithDerivedTextures(restoredBase,steam,a[0],a[1],a[2],a[3],a[4],a[5],a[6],prepared.toFile());
   update(progress,"Preparando mapas y audio");SteamPartialObbStage.assembleShared2AndMapDiagnostic(restoredBase,prepared.toFile(),a[7],steam,a[8],a[9],a[10],a[11],a[12],a[13],a[14],a[15],partial.toFile());remove(prepared);
   update(progress,"Preparando recursos principales");SteamShared0Complete.restore(shared0Base,steam,a[16],a[17],a[18],a[19],shared0.toFile());SteamShared0DiagnosticObb.assemble(partial.toFile(),shared0.toFile(),obb0.toFile());Files.delete(partial);Files.delete(shared0);
   update(progress,"Preparando texturas y efectos");SteamShared1Complete.restore(restoredBase,steam,b,shaderPack,shared1.toFile());SteamShared1DiagnosticObb.assemble(obb0.toFile(),shared1.toFile(),obb1.toFile());Files.delete(obb0);Files.delete(shared1);
   update(progress,"Preparando idiomas");SteamLocalizationObbPreparation.prepare(obb1.toFile(),steam,c[0],c[1],locales.toFile());Files.delete(obb1);
   update(progress,"Preparando animaciones");SteamAnimationDiagnosticObb.assemble(locales.toFile(),restoredBase,steam,c[2],animations.toFile());Files.delete(locales);
   update(progress,"Preparando shaders");SteamBuiltinShaderDiagnosticObb.assemble(animations.toFile(),builtinPack,c[3],builtin.toFile());Files.delete(animations);
   update(progress,"Preparando objetos y efectos");SteamPreloadDiagnosticObb.assemble(builtin.toFile(),steam,c[4],result.toFile());Files.delete(builtin);
   update(progress,"Verificando datos");String planHash=sha(c[2].toPath());boolean noProbe=sha(normalizedBase.toPath()).equals("3f6f96ef96a2b976d6862d37f7b5c4a06c15eb4ef5bcbd8efa49af17da8aeb76");if(noProbe&&!planHash.equals(SHARED_PLAN))throw new IOException("incompatible reduced-base animation plan");String expected=noProbe?"b74cbadedf8a03eeb2cdeb4a6db9eda66dddd4ed4aa457e9ea80508d2e1aa230":planHash.equals(SHARED_PLAN)?SHARED_RESULT:planHash.equals(BATCH3_PLAN)?BATCH3_RESULT:planHash.equals(BATCH2_PLAN)?BATCH2_RESULT:planHash.equals(BATCH_PLAN)?BATCH_RESULT:planHash.equals(KNIGHT_PLAN)?KNIGHT_RESULT:planHash.equals(BANDIT_PLAN)?BANDIT_RESULT:RESULT;if(!sha(result).equals(expected))throw new IOException("diagnostic output differs");Files.move(result,dest);
  }finally{remove(temp);}
 }
 private SteamFullDiagnosticPreparation(){}
}
