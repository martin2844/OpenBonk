package com.openbonk.dataprep;

import java.io.*;
import java.nio.file.Files;
import java.nio.file.LinkOption;
import java.nio.file.Path;
import java.nio.file.StandardOpenOption;
import java.security.MessageDigest;
import java.security.NoSuchAlgorithmException;
import java.util.*;
import java.util.zip.ZipEntry;
import java.util.zip.ZipFile;

/** Offset-only experimental restoration of scene values; not a complete OBB builder. */
public final class SteamSceneValues {
    private static final String BASE_SHA="ca36181d234fe22d0d565bd2bc199d7fab1be4e880e3276083b43c071f66716d";
    private static boolean compatibleBaseHash(String hash){return hash.equals(BASE_SHA)||hash.equals("f0bb1f860804e25687085bca64a4554fc292bd03129eb0bb2303a277da52e003");}
    private static final String PLAN_SHA="bccd34ac5e257e622e2214e5a970a9da0edf22a39c4ffe334ab1e3d1483b22e3";
    private static final String FULL_PLAN_SHA="d64baaedbddc6a9dd1d0fd27e5986aabaa8dd3d4390fd43f9a5d875b51448d1b";
    private static final int LIMIT=32*1024*1024;
    private static MessageDigest digest(){try{return MessageDigest.getInstance("SHA-256");}catch(NoSuchAlgorithmException e){throw new AssertionError(e);}}
    private static byte[] hash(byte[] b){return digest().digest(b);}
    private static String hex(byte[] b){char[] d="0123456789abcdef".toCharArray(),r=new char[b.length*2];for(int i=0;i<b.length;i++){r[2*i]=d[(b[i]&255)>>>4];r[2*i+1]=d[b[i]&15];}return new String(r);}
    private static byte[] read(InputStream in,int limit)throws IOException{
        ByteArrayOutputStream out=new ByteArrayOutputStream();byte[] b=new byte[65536];int total=0;
        for(int n;(n=in.read(b))!=-1;){if(n==0)continue;if(n>limit-total)throw new IOException("input too large");out.write(b,0,n);total+=n;}
        return out.toByteArray();
    }
    private static byte[] file(File f,int limit)throws IOException{try(InputStream in=new FileInputStream(f)){return read(in,limit);}}
    private static byte[] entry(ZipFile zip,ZipEntry e,int limit)throws IOException{
        if(e==null||e.isDirectory()||e.getSize()<0||e.getSize()>limit)throw new IOException("invalid ZIP entry");
        try(InputStream in=zip.getInputStream(e)){byte[] b=read(in,limit);if(b.length!=e.getSize())throw new IOException("truncated ZIP entry");return b;}
    }
    private static String rootFor(String name,String scene){
        if(name.equals(scene))return "";
        if(name.equals("Megabonk_Data/"+scene)||name.endsWith("/Megabonk_Data/"+scene))return name.substring(0,name.length()-scene.length());
        return null;
    }
    private static byte[] bytes(DataInputStream in,int size)throws IOException{byte[] b=new byte[size];in.readFully(b);return b;}
    private static void ranges(DataInputStream plan,byte[] dest,byte[] source,int count,boolean windows,BitSet used)throws IOException{
        if(count<0||count>50000)throw new IOException("invalid plan count");
        for(int i=0;i<count;i++){
            int target=plan.readInt(),linux=plan.readInt(),win=plan.readInt(),size=plan.readInt(),src=windows?win:linux;
            if(target<0||src<0||size<=0||target>dest.length-size||src>source.length-size||used.nextSetBit(target)>=0&&used.nextSetBit(target)<target+size)throw new IOException("invalid or overlapping plan range");
            for(int j=target;j<target+size;j++)if(dest[j]!=0)throw new IOException("target range not blank");
            System.arraycopy(source,src,dest,target,size);used.set(target,target+size);
        }
    }
    public static Map<String,byte[]> restore(File baseFile,File steamFile,File planFile)throws IOException{
        if(!baseFile.isFile()||baseFile.length()>16*1024*1024||!compatibleBaseHash(hex(hash(file(baseFile,16*1024*1024)))))throw new IOException("incompatible base OBB");
        byte[] planBytes=file(planFile,1024*1024);String planHash=hex(hash(planBytes));boolean full=planHash.equals(FULL_PLAN_SHA);
        if(!full&&!planHash.equals(PLAN_SHA))throw new IOException("incompatible scene plan");
        if(!steamFile.isFile()||steamFile.length()<=0||steamFile.length()>1024L*1024*1024)throw new IOException("invalid Steam ZIP size");
        try(ZipFile base=new ZipFile(baseFile);ZipFile steam=new ZipFile(steamFile);DataInputStream plan=new DataInputStream(new ByteArrayInputStream(planBytes))){
            if(!Arrays.equals(bytes(plan,8),new byte[]{'O','B','S','C','N','0','0',(byte)(full?'2':'1')})||plan.readInt()!=3)throw new IOException("invalid scene plan");
            String[] names={"level1","level2","level4"};Map<String,ZipEntry> sourceEntries=new HashMap<>();Set<String> seen=new HashSet<>();String sourceRoot=null;
            Enumeration<? extends ZipEntry> enumeration=steam.entries();
            while(enumeration.hasMoreElements()){
                ZipEntry e=enumeration.nextElement();String name=e.getName();
                if(seen.size()>=20000||name.startsWith("/")||name.indexOf('\\')>=0||name.indexOf(':')>=0||!seen.add(name))throw new IOException("unsafe or duplicate ZIP entry");
                for(String part:name.split("/"))if(part.isEmpty()||part.equals(".")||part.equals(".."))throw new IOException("unsafe ZIP path");
                for(String scene:names){String root=rootFor(name,scene);if(root!=null){if(sourceEntries.put(scene,e)!=null||sourceRoot!=null&&!sourceRoot.equals(root))throw new IOException("conflicting Steam roots");sourceRoot=root;}}
            }
            Map<String,byte[]> result=new LinkedHashMap<>();Boolean platform=null;
            for(String scene:names){
                byte[] label=bytes(plan,8);byte[] expectedLabel=new byte[8];byte[] labelBytes=scene.getBytes("US-ASCII");System.arraycopy(labelBytes,0,expectedLabel,0,labelBytes.length);
                if(!Arrays.equals(label,expectedLabel))throw new IOException("scene order mismatch");
                byte[] linuxHash=bytes(plan,32),windowsHash=bytes(plan,32),blankHash=bytes(plan,32),firstHash=bytes(plan,32),finalHash=bytes(plan,32);
                byte[] componentHash=full?bytes(plan,32):null,sceneHash=full?bytes(plan,32):null;
                int linuxSize=plan.readInt(),windowsSize=plan.readInt(),targetSize=plan.readInt(),gameCount=plan.readInt(),transformCount=plan.readInt();
                int componentCount=full?plan.readInt():0,bodyCount=full?plan.readInt():0;
                if(linuxSize<=0||linuxSize>LIMIT||windowsSize<=0||windowsSize>LIMIT||targetSize<=0||targetSize>LIMIT)throw new IOException("scene exceeds limit");
                byte[] source=entry(steam,sourceEntries.get(scene),LIMIT),sourceHash=hash(source);boolean windows;
                if(source.length==linuxSize&&MessageDigest.isEqual(sourceHash,linuxHash))windows=false;
                else if(source.length==windowsSize&&MessageDigest.isEqual(sourceHash,windowsHash))windows=true;
                else throw new IOException("unsupported Steam scene: "+scene);
                if(platform!=null&&platform.booleanValue()!=windows)throw new IOException("mixed Steam platforms");platform=windows;
                ByteArrayOutputStream raw=new ByteArrayOutputStream(targetSize);String prefix="assets/bin/Data/"+scene+".split";
                int count=(targetSize+1048575)/1048576;
                for(int i=0;i<count;i++){byte[] part=entry(base,base.getEntry(prefix+i),1048576);if(i<count-1&&part.length!=1048576)throw new IOException("invalid scene fragment");raw.write(part);}
                if(base.getEntry(prefix+count)!=null)throw new IOException("extra scene fragment");
                byte[] dest=raw.toByteArray();if(dest.length!=targetSize||!MessageDigest.isEqual(hash(dest),blankHash))throw new IOException("incompatible scene template");
                BitSet used=new BitSet(targetSize);ranges(plan,dest,source,gameCount,windows,used);
                if(!MessageDigest.isEqual(hash(dest),firstHash))throw new IOException("game object reconstruction differs");
                ranges(plan,dest,source,transformCount,windows,used);
                if(!MessageDigest.isEqual(hash(dest),finalHash))throw new IOException("transform reconstruction differs");
                if(full){
                    ranges(plan,dest,source,componentCount,windows,used);
                    if(!MessageDigest.isEqual(hash(dest),componentHash))throw new IOException("component reconstruction differs");
                    ranges(plan,dest,source,bodyCount,windows,used);
                    if(!MessageDigest.isEqual(hash(dest),sceneHash))throw new IOException("complete scene reconstruction differs");
                }
                result.put(scene,dest);
            }
            if(plan.read()!=-1)throw new IOException("trailing plan data");
            return result;
        }
    }
    /** Publish a complete verified scene set into a new app-private directory. */
    public static void restoreToDirectory(File baseFile,File steamFile,File planFile,File output)throws IOException{
        Path dest=output.toPath().toAbsolutePath(),parent=dest.getParent();
        if(parent==null||!Files.isDirectory(parent)||Files.exists(dest,LinkOption.NOFOLLOW_LINKS))throw new IOException("output must be a new directory");
        Map<String,byte[]> scenes=restore(baseFile,steamFile,planFile);
        Path staged=Files.createTempDirectory(parent,".openbonk-scenes-");
        try{
            for(Map.Entry<String,byte[]> scene:scenes.entrySet()){
                Path file=staged.resolve(scene.getKey());
                Files.write(file,scene.getValue(),StandardOpenOption.CREATE_NEW,StandardOpenOption.WRITE);
                if(!MessageDigest.isEqual(hash(file(file.toFile(),LIMIT)),hash(scene.getValue())))throw new IOException("staged scene differs");
            }
            Files.move(staged,dest);
        }finally{
            if(Files.exists(staged,LinkOption.NOFOLLOW_LINKS)){
                for(String name:scenes.keySet())Files.deleteIfExists(staged.resolve(name));
                Files.deleteIfExists(staged);
            }
        }
    }
    private SteamSceneValues(){}
}
