package com.megabonk.launcher;
import android.content.Context;
import com.openbonk.dataprep.SteamFullDiagnosticPreparation;
import com.openbonk.dataprep.SteamDiagnosticInputCheck;
import com.openbonk.dataprep.SteamRuntimeRebase;
import com.openbonk.dataprep.SteamVideoStorage;
import com.openbonk.dataprep.SteamGeneratedAudio;
import com.openbonk.dataprep.SteamPackedAudio;
import com.openbonk.dataprep.SteamPlannedTextures;
import com.openbonk.dataprep.SteamPlannedMeshes;
import java.io.*;
import java.nio.file.*;
import java.nio.file.attribute.BasicFileAttributes;
import java.util.*;
import java.util.zip.*;
import java.util.function.Consumer;

/** Local research integration: embedded plans and base files are private. */
final class LauncherDiagnosticGenerator implements LauncherPreparedSteamImport.Generator {
 private static final String ROOT="openbonk-diagnostic/";
 private final Context context;
 private final Consumer<String> progress;
 LauncherDiagnosticGenerator(Context context,Consumer<String> progress){this.context=context.getApplicationContext();this.progress=progress;}
 private static void copy(InputStream in,File out,long limit)throws IOException{
  try(FileOutputStream stream=new FileOutputStream(out)){byte[] b=new byte[65536];long total=0;for(int n;(n=in.read(b))!=-1;){if(n==0)continue;if(n>limit-total)throw new IOException("diagnostic input too large");stream.write(b,0,n);total+=n;}if(total==0)throw new IOException("empty diagnostic input");stream.getFD().sync();}
 }
 private static void remove(Path root)throws IOException{
  if(!Files.exists(root,LinkOption.NOFOLLOW_LINKS))return;
  Files.walkFileTree(root,new SimpleFileVisitor<Path>(){@Override public FileVisitResult visitFile(Path file,BasicFileAttributes attrs)throws IOException{Files.delete(file);return FileVisitResult.CONTINUE;}@Override public FileVisitResult postVisitDirectory(Path dir,IOException error)throws IOException{if(error!=null)throw error;Files.delete(dir);return FileVisitResult.CONTINUE;}});
 }
 private static String smallAscii(InputStream in,int limit)throws IOException{
  ByteArrayOutputStream bytes=new ByteArrayOutputStream();byte[] b=new byte[128];int total=0;
  for(int n;(n=in.read(b))!=-1;){if(n==0)continue;if(n>limit-total)throw new IOException("diagnostic metadata too large");bytes.write(b,0,n);total+=n;}
  return new String(bytes.toByteArray(),java.nio.charset.StandardCharsets.US_ASCII);
 }
 @Override public void prepare(File steam,File output)throws IOException{
  Path temp=Files.createTempDirectory(context.getCacheDir().toPath(),".openbonk-full-diagnostic-input-");
  try{
   String[] available=context.getAssets().list(ROOT);
   if(available!=null&&Arrays.asList(available).contains("generated-audio-mode")){
    String mode;
    try(InputStream in=context.getAssets().open(ROOT+"generated-audio-mode")){mode=smallAscii(in,64);}
    if(!"OPENBONK_GENERATED_AUDIO_RESEARCH_V1".equals(mode)&&!"OPENBONK_GENERATED_AUDIO_RESEARCH_V2".equals(mode)&&!"OPENBONK_GENERATED_AUDIO_RESEARCH_V3".equals(mode)&&!"OPENBONK_GENERATED_AUDIO_RESEARCH_V4".equals(mode)&&!"OPENBONK_GENERATED_AUDIO_RESEARCH_V5".equals(mode)&&!"OPENBONK_GENERATED_AUDIO_RESEARCH_V6".equals(mode)&&!"OPENBONK_GENERATED_AUDIO_RESEARCH_V7".equals(mode))throw new IOException("invalid generated audio mode");
    String expected;
    try(InputStream in=context.getAssets().open(ROOT+"generated-audio-plan.sha256")){expected=smallAscii(in,64);}
    if(!expected.matches("[0-9a-f]{64}"))throw new IOException("invalid generated audio checksum");
    File template=temp.resolve("generated-audio-template.obb").toFile(),plan=temp.resolve("generated-audio-plan.json").toFile();
    try(InputStream in=context.getAssets().open(ROOT+"generated-audio-template.obb")){copy(in,template,512L*1024*1024);}
    try(InputStream in=context.getAssets().open(ROOT+"generated-audio-plan.json")){copy(in,plan,1024L*1024);}
    File animationTemplate=template,cubeTemplate=template,meshTemplate=template,textureTemplate=template,packedTemplate=template;
    if(mode.equals("OPENBONK_GENERATED_AUDIO_RESEARCH_V7")){
     String objectExpected;
     try(InputStream in=context.getAssets().open(ROOT+"object-plan.sha256")){objectExpected=smallAscii(in,64);}
     if(!objectExpected.matches("[0-9a-f]{64}"))throw new IOException("invalid object checksum");
     File objectPlan=temp.resolve("object-plan.json").toFile();animationTemplate=temp.resolve("object-intermediate.obb").toFile();
     try(InputStream in=context.getAssets().open(ROOT+"object-plan.json")){copy(in,objectPlan,2L*1024*1024);}
     if(progress!=null)progress.accept("Preparando recursos");
     SteamPlannedMeshes.restoreExactObjects(template,steam,objectPlan,objectExpected,animationTemplate);
    }
    if(mode.equals("OPENBONK_GENERATED_AUDIO_RESEARCH_V6")||mode.equals("OPENBONK_GENERATED_AUDIO_RESEARCH_V7")){
     String animationExpected;
     try(InputStream in=context.getAssets().open(ROOT+"animation-plan.sha256")){animationExpected=smallAscii(in,64);}
     if(!animationExpected.matches("[0-9a-f]{64}"))throw new IOException("invalid animation checksum");
     File animationPlan=temp.resolve("animation-plan.json").toFile();cubeTemplate=temp.resolve("animation-intermediate.obb").toFile();
     try(InputStream in=context.getAssets().open(ROOT+"animation-plan.json")){copy(in,animationPlan,2L*1024*1024);}
     if(progress!=null)progress.accept("Preparando animaciones");
     SteamPlannedMeshes.restoreAnimations(animationTemplate,steam,animationPlan,animationExpected,cubeTemplate);
    }
    if(mode.equals("OPENBONK_GENERATED_AUDIO_RESEARCH_V5")||mode.equals("OPENBONK_GENERATED_AUDIO_RESEARCH_V6")||mode.equals("OPENBONK_GENERATED_AUDIO_RESEARCH_V7")){
     String cubeExpected;
     try(InputStream in=context.getAssets().open(ROOT+"cubemap-plan.sha256")){cubeExpected=smallAscii(in,64);}
     if(!cubeExpected.matches("[0-9a-f]{64}"))throw new IOException("invalid cubemap checksum");
     File cubePlan=temp.resolve("cubemap-plan.json").toFile();meshTemplate=temp.resolve("cubemap-intermediate.obb").toFile();
     try(InputStream in=context.getAssets().open(ROOT+"cubemap-plan.json")){copy(in,cubePlan,2L*1024*1024);}
     try{System.loadLibrary("openbonk_texture");}catch(UnsatisfiedLinkError e){throw new IOException("cannot load texture converter",e);}
     if(progress!=null)progress.accept("Preparando cielos");
     SteamPlannedTextures.restoreCubemaps(cubeTemplate,steam,cubePlan,cubeExpected,meshTemplate);
    }
    if(mode.equals("OPENBONK_GENERATED_AUDIO_RESEARCH_V4")||mode.equals("OPENBONK_GENERATED_AUDIO_RESEARCH_V5")||mode.equals("OPENBONK_GENERATED_AUDIO_RESEARCH_V6")||mode.equals("OPENBONK_GENERATED_AUDIO_RESEARCH_V7")){
     String meshExpected;
     try(InputStream in=context.getAssets().open(ROOT+"mesh-plan.sha256")){meshExpected=smallAscii(in,64);}
     if(!meshExpected.matches("[0-9a-f]{64}"))throw new IOException("invalid mesh checksum");
     File meshPlan=temp.resolve("mesh-plan.json").toFile();textureTemplate=temp.resolve("mesh-intermediate.obb").toFile();
     try(InputStream in=context.getAssets().open(ROOT+"mesh-plan.json")){copy(in,meshPlan,2L*1024*1024);}
     if(progress!=null)progress.accept("Preparando modelos");
     SteamPlannedMeshes.restore(meshTemplate,steam,meshPlan,meshExpected,textureTemplate);
    }
    if(mode.equals("OPENBONK_GENERATED_AUDIO_RESEARCH_V3")||mode.equals("OPENBONK_GENERATED_AUDIO_RESEARCH_V4")||mode.equals("OPENBONK_GENERATED_AUDIO_RESEARCH_V5")||mode.equals("OPENBONK_GENERATED_AUDIO_RESEARCH_V6")||mode.equals("OPENBONK_GENERATED_AUDIO_RESEARCH_V7")){
     String textureExpected;
     try(InputStream in=context.getAssets().open(ROOT+"texture-plan.sha256")){textureExpected=smallAscii(in,64);}
     if(!textureExpected.matches("[0-9a-f]{64}"))throw new IOException("invalid texture checksum");
     File texturePlan=temp.resolve("texture-plan.json").toFile();packedTemplate=temp.resolve("texture-intermediate.obb").toFile();
     try(InputStream in=context.getAssets().open(ROOT+"texture-plan.json")){copy(in,texturePlan,2L*1024*1024);}
     try{System.loadLibrary("openbonk_texture");}catch(UnsatisfiedLinkError e){throw new IOException("cannot load texture converter",e);}
     if(progress!=null)progress.accept("Preparando texturas");
     SteamPlannedTextures.restore(textureTemplate,steam,texturePlan,textureExpected,packedTemplate);
    }
    if(progress!=null)progress.accept("Preparando audio");
    if(mode.equals("OPENBONK_GENERATED_AUDIO_RESEARCH_V2")||mode.equals("OPENBONK_GENERATED_AUDIO_RESEARCH_V3")||mode.equals("OPENBONK_GENERATED_AUDIO_RESEARCH_V4")||mode.equals("OPENBONK_GENERATED_AUDIO_RESEARCH_V5")||mode.equals("OPENBONK_GENERATED_AUDIO_RESEARCH_V6")||mode.equals("OPENBONK_GENERATED_AUDIO_RESEARCH_V7")){
     String packedExpected;
     try(InputStream in=context.getAssets().open(ROOT+"packed-audio-plan.sha256")){packedExpected=smallAscii(in,64);}
     if(!packedExpected.matches("[0-9a-f]{64}"))throw new IOException("invalid packed audio checksum");
     File packedPlan=temp.resolve("packed-audio-plan.json").toFile(),intermediate=temp.resolve("packed-audio-intermediate.obb").toFile();
     try(InputStream in=context.getAssets().open(ROOT+"packed-audio-plan.json")){copy(in,packedPlan,1024L*1024);}
     SteamPackedAudio.restore(packedTemplate,steam,packedPlan,packedExpected,intermediate);
     SteamGeneratedAudio.restore(intermediate,steam,plan,expected,output);
    }else SteamGeneratedAudio.restore(template,steam,plan,expected,output);
    return;
   }
   File shared0=temp.resolve("shared0.zip").toFile(),obb=temp.resolve("base.obb").toFile(),plans=temp.resolve("plans").toFile();if(!plans.mkdir())throw new IOException("cannot create diagnostic inputs");
   try(InputStream in=context.getAssets().open(ROOT+"shared0.zip")){copy(in,shared0,4L*1024*1024);}
   try(InputStream in=context.getAssets().open(ROOT+"base.obb")){copy(in,obb,16L*1024*1024);}
   String[] baseAssets=context.getAssets().list(ROOT);
   if(baseAssets!=null&&Arrays.asList(baseAssets).contains("scene-component-plan.bin"))
    try(InputStream in=context.getAssets().open(ROOT+"scene-component-plan.bin")){copy(in,new File(plans,"scene-component-plan.bin"),2L*1024*1024);}
   if(baseAssets!=null&&Arrays.asList(baseAssets).contains("shared-component-plan.bin"))
    try(InputStream in=context.getAssets().open(ROOT+"shared-component-plan.bin")){copy(in,new File(plans,"shared-component-plan.bin"),2L*1024*1024);}
   if(baseAssets!=null&&Arrays.asList(baseAssets).contains("material-plan.bin"))
    try(InputStream in=context.getAssets().open(ROOT+"material-plan.bin")){copy(in,new File(plans,"material-plan.bin"),2L*1024*1024);}
   int count=0;Set<String> names=new HashSet<>();try(ZipInputStream zip=new ZipInputStream(context.getAssets().open(ROOT+"plans.zip"))){for(ZipEntry e;(e=zip.getNextEntry())!=null;){String name=e.getName();if(e.isDirectory()||!name.matches("[a-z0-9-]+\\.(bin|zip)")||!names.add(name)||++count>50)throw new IOException("invalid diagnostic plan archive");copy(zip,new File(plans,name),8L*1024*1024);zip.closeEntry();}}
   if(count!=50)throw new IOException("incomplete diagnostic plan archive");
   String[] assetNames=context.getAssets().list(ROOT);
   boolean runtimeRebase=assetNames!=null&&Arrays.asList(assetNames).contains("runtime-rebase.zip");
   boolean videoOnly=assetNames!=null&&Arrays.asList(assetNames).contains("runtime-video-only");
   if(runtimeRebase&&videoOnly)throw new IOException("conflicting runtime preparation modes");
   File runtimePlan=temp.resolve("runtime-rebase.zip").toFile(),prepared=temp.resolve("previous-runtime.obb").toFile();
   if(runtimeRebase)try(InputStream in=context.getAssets().open(ROOT+"runtime-rebase.zip")){copy(in,runtimePlan,4L*1024*1024);}
   try{System.loadLibrary("openbonk_texture");}catch(UnsatisfiedLinkError e){throw new IOException("cannot load texture converter",e);}
   try {
    SteamFullDiagnosticPreparation.prepare(shared0,obb,steam,plans,(runtimeRebase||videoOnly)?prepared:output,label->{if(progress!=null)progress.accept(label);});
    if(runtimeRebase){if(progress!=null)progress.accept("Adaptando datos para el runtime");SteamRuntimeRebase.rebase(prepared,runtimePlan,output);}
    if(videoOnly){if(progress!=null)progress.accept("Preparando video");SteamVideoStorage.prepare(prepared,output);}
   } catch (SteamDiagnosticInputCheck.Failure e) {
    boolean es=Locale.getDefault().getLanguage().equals("es");
    String detail=e.missing?(es?"Falta el archivo ":"Missing file "):(es?"Archivo incompatible o dañado: ":"Unsupported or damaged file: ");
    throw new IOException(detail+e.file,e);
   }
  }finally{remove(temp);}
 }
}
