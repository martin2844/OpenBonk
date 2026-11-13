package com.openbonk.dataprep;

import java.io.*;
import java.nio.file.*;
import java.nio.file.attribute.BasicFileAttributes;
import java.security.MessageDigest;
import java.security.NoSuchAlgorithmException;
import java.util.*;
import java.util.zip.ZipEntry;
import java.util.zip.ZipFile;

/** Transactionally combines experimental scene, audio, mesh, texture, animation and video stages. */
public final class SteamPreparationPrototype {
    private static final String LINUX_SCENE="fe6e50ab3323b523dc53a4151fcc12dcf4d5fd0076d14b5150ba16dd64371551";
    private static final String WINDOWS_SCENE="9fa2811a0dab3be720f1c626a12b6d8062e7219a60f513e10de9519f1339c93c";
    private static final String LINUX_ASSET="d9c955e43af9151f4d9d4e3ae9328ff2582bde7a1c9aa4580ddb2ba95ad66b35";
    private static final String WINDOWS_ASSET="64c5156116e20fe645acc133bfae7ab1fdd3a7bbc99f915559efc0640be0349a";
    private static MessageDigest digest(){try{return MessageDigest.getInstance("SHA-256");}catch(NoSuchAlgorithmException e){throw new AssertionError(e);}}
    private static String hex(byte[] b){char[] d="0123456789abcdef".toCharArray(),r=new char[b.length*2];for(int i=0;i<b.length;i++){r[2*i]=d[(b[i]&255)>>>4];r[2*i+1]=d[b[i]&15];}return new String(r);}
    private static String rootFor(String n,String part){if(n.equals(part))return "";if(n.equals("Megabonk_Data/"+part)||n.endsWith("/Megabonk_Data/"+part))return n.substring(0,n.length()-part.length());return null;}
    private static String hashEntry(ZipFile zip,ZipEntry entry,long limit)throws IOException{
        if(entry==null||entry.isDirectory()||entry.getSize()<=0||entry.getSize()>limit)throw new IOException("invalid Steam entry");
        MessageDigest hash=digest();byte[] buf=new byte[65536];long total=0;
        try(InputStream in=zip.getInputStream(entry)){for(int n;(n=in.read(buf))!=-1;){if(n==0)continue;if(n>limit-total)throw new IOException("Steam entry too large");hash.update(buf,0,n);total+=n;}}
        if(total!=entry.getSize())throw new IOException("truncated Steam entry");return hex(hash.digest());
    }
    private static void checkPlatform(File steam)throws IOException{
        if(!steam.isFile()||steam.length()<=0||steam.length()>1024L*1024*1024)throw new IOException("invalid Steam ZIP size");
        try(ZipFile zip=new ZipFile(steam)){
            ZipEntry scene=null,asset=null;String sceneRoot=null,assetRoot=null;Set<String> names=new HashSet<>();Enumeration<? extends ZipEntry> entries=zip.entries();
            while(entries.hasMoreElements()){
                ZipEntry e=entries.nextElement();String n=e.getName();
                if(names.size()>=20000||n.startsWith("/")||n.indexOf('\\')>=0||n.indexOf(':')>=0||!names.add(n))throw new IOException("unsafe or duplicate ZIP entry");
                for(String part:n.split("/"))if(part.isEmpty()||part.equals(".")||part.equals(".."))throw new IOException("unsafe ZIP path");
                String root=rootFor(n,"level1");if(root!=null){if(scene!=null)throw new IOException("multiple scene roots");scene=e;sceneRoot=root;}
                root=rootFor(n,"sharedassets1.assets");if(root!=null){if(asset!=null)throw new IOException("multiple asset roots");asset=e;assetRoot=root;}
            }
            if(scene==null||asset==null||!sceneRoot.equals(assetRoot))throw new IOException("conflicting Steam roots");
            String sceneHash=hashEntry(zip,scene,32L*1024*1024),assetHash=hashEntry(zip,asset,64L*1024*1024);
            if(!(sceneHash.equals(LINUX_SCENE)&&assetHash.equals(LINUX_ASSET))&&!(sceneHash.equals(WINDOWS_SCENE)&&assetHash.equals(WINDOWS_ASSET)))throw new IOException("incompatible or mixed Steam platforms");
        }
    }
    private static void removeTree(Path root)throws IOException{
        if(!Files.exists(root,LinkOption.NOFOLLOW_LINKS))return;
        Files.walkFileTree(root,new SimpleFileVisitor<Path>(){
            @Override public FileVisitResult visitFile(Path f,BasicFileAttributes a)throws IOException{Files.delete(f);return FileVisitResult.CONTINUE;}
            @Override public FileVisitResult postVisitDirectory(Path d,IOException error)throws IOException{if(error!=null)throw error;Files.delete(d);return FileVisitResult.CONTINUE;}
        });
    }
    private static void writeSet(Path dir,Map<String,byte[]> files)throws IOException{
        Files.createDirectory(dir);
        for(Map.Entry<String,byte[]> item:files.entrySet()){
            String name=item.getKey();if(name.contains("/")||name.contains("\\")||name.equals(".")||name.equals(".."))throw new IOException("invalid output name");
            Path path=dir.resolve(name);Files.write(path,item.getValue(),StandardOpenOption.CREATE_NEW,StandardOpenOption.WRITE);
            byte[] stored=Files.readAllBytes(path);if(!MessageDigest.isEqual(digest().digest(stored),digest().digest(item.getValue())))throw new IOException("staged file differs");
        }
    }
    // One process owns app-private preparation; never race two publishers.
    public static synchronized void prepare(File base,File steam,File scenePlan,File audioPlan,File meshPlan,File texturePlan,File animationPlan,File output)throws IOException{
        prepareInternal(base,steam,scenePlan,audioPlan,meshPlan,texturePlan,animationPlan,null,null,output,false,false);
    }
    /** Experimental stage requires the caller to load the native texture library first. */
    public static synchronized void prepareWithConvertedTextures(File base,File steam,File scenePlan,File audioPlan,File meshPlan,File texturePlan,File animationPlan,File convertedPlan,File output)throws IOException{
        if(convertedPlan==null)throw new IOException("missing converted texture plan");
        prepareInternal(base,steam,scenePlan,audioPlan,meshPlan,texturePlan,animationPlan,convertedPlan,null,output,false,false);
    }
    /** Experimental derived-field path; plans and base remain subject to distribution review. */
    public static synchronized void prepareWithDerivedTextures(File base,File steam,File scenePlan,File audioPlan,File meshPlan,File texturePlan,File animationPlan,File convertedPlan,File extraPlan,File output)throws IOException{
        if(convertedPlan==null||extraPlan==null)throw new IOException("missing texture plan");
        prepareInternal(base,steam,scenePlan,audioPlan,meshPlan,texturePlan,animationPlan,convertedPlan,extraPlan,output,false,true);
    }
    /** Local diagnostic: this plan contains Android-derived private differences. */
    public static synchronized void preparePrivateAnimationDiagnostic(File base,File steam,File scenePlan,File audioPlan,File meshPlan,File texturePlan,File animationPlan,File output)throws IOException{
        prepareInternal(base,steam,scenePlan,audioPlan,meshPlan,texturePlan,animationPlan,null,null,output,true,false);
    }
    /** Local-only diagnostic: both supplied private plans contain Android-derived bytes. */
    public static synchronized void preparePrivateExtendedDiagnostic(File base,File steam,File scenePlan,File audioPlan,File meshPlan,File texturePlan,File animationPlan,File convertedPlan,File extraPlan,File output)throws IOException{
        if(convertedPlan==null||extraPlan==null)throw new IOException("missing texture plan");
        prepareInternal(base,steam,scenePlan,audioPlan,meshPlan,texturePlan,animationPlan,convertedPlan,extraPlan,output,true,false);
    }
    private static void prepareInternal(File base,File steam,File scenePlan,File audioPlan,File meshPlan,File texturePlan,File animationPlan,File convertedPlan,File extraPlan,File output,boolean privateAnimations,boolean derivedExtra)throws IOException{
        Path dest=output.toPath().toAbsolutePath(),parent=dest.getParent();
        if(parent==null||!Files.isDirectory(parent)||Files.exists(dest,LinkOption.NOFOLLOW_LINKS))throw new IOException("output must be a new directory");
        checkPlatform(steam);
        Map<String,byte[]> scenes=SteamSceneValues.restore(base,steam,scenePlan);
        if(scenes.size()!=3)throw new IOException("incomplete scene reconstruction");
        String[][] expectedScenes={{"level1","164012e485062f572bba37de85aafe42a494810202d80e13d4f72dbf20ce24dc"},
            {"level2","bbee6f3c886a795f0029626ed62ccc6d569fca3ce9ee821876619d3009a55dea"},
            {"level4","665e167cbcc2fd9c43c46e1d2c84f83c6ca9e0ad6c9756f439e10b90d5758972"}};
        for(String[] row:expectedScenes)if(!scenes.containsKey(row[0])||!hex(digest().digest(scenes.get(row[0]))).equals(row[1]))throw new IOException("incomplete scene reconstruction");
        Map<String,byte[]> audio=SteamAudioResources.restore(base,steam,audioPlan);
        Map<String,byte[]> meshes=SteamMeshResources.restore(base,steam,meshPlan);
        Map<String,byte[]> textures=SteamExactTextures.restore(base,steam,texturePlan);
        Map<String,byte[]> animations=SteamAnimationResources.restore(base,steam,animationPlan);
        Map<String,byte[]> converted=convertedPlan==null?Collections.emptyMap():SteamConvertedTextures.restore(base,steam,convertedPlan);
        Map<String,byte[]> extra=extraPlan==null?Collections.emptyMap():derivedExtra?SteamExtraTextures.restore(base,steam,extraPlan):SteamExtraTextures.restorePrivateDiagnostic(base,steam,extraPlan);
        byte[] video=SteamBufferingClip.extract(steam);
        if(audio.size()!=126||meshes.size()!=50||textures.size()!=56||animations.size()!=(privateAnimations?108:25))throw new IOException("incompatible animation scope or incomplete prepared data");
        if(convertedPlan!=null&&(converted.size()!=53||!Collections.disjoint(textures.keySet(),converted.keySet())))throw new IOException("incomplete or overlapping converted textures");
        if(extraPlan!=null&&(extra.size()!=25||!Collections.disjoint(textures.keySet(),extra.keySet())||!Collections.disjoint(converted.keySet(),extra.keySet())))throw new IOException("incomplete or overlapping private textures");
        Path staged=Files.createTempDirectory(parent,".openbonk-preparation-");
        try{writeSet(staged.resolve("scenes"),scenes);writeSet(staged.resolve("audio"),audio);writeSet(staged.resolve("meshes"),meshes);writeSet(staged.resolve("textures"),textures);writeSet(staged.resolve("animations"),animations);if(convertedPlan!=null)writeSet(staged.resolve("converted-textures"),converted);if(extraPlan!=null)writeSet(staged.resolve(derivedExtra?"derived-extra-textures":"private-extra-textures"),extra);writeSet(staged.resolve("video"),Collections.singletonMap("Buffering3.webm",video));Files.move(staged,dest);}
        finally{removeTree(staged);}
    }
    private SteamPreparationPrototype(){}
}
