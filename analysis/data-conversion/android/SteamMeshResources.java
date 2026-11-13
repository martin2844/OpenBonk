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

/** Experimental recovery of 50 pinned mesh assets from Steam buffers. */
public final class SteamMeshResources {
    private static final String BASE_SHA="ca36181d234fe22d0d565bd2bc199d7fab1be4e880e3276083b43c071f66716d";
    private static boolean compatibleBaseHash(String hash){return hash.equals(BASE_SHA)||hash.equals("f0bb1f860804e25687085bca64a4554fc292bd03129eb0bb2303a277da52e003");}
    private static final String PLAN_SHA="f9f08f2a77b87a9c99e0f3e52d205f3a7bdef80d94b91bede0dd2aba07875cba";
    private static final long STREAM_SIZE=113115008L;
    private static final class Range {int target,size,linuxFile,windowsFile,filled;long linuxOffset,windowsOffset;byte[] hash,data;}
    private static final class Mesh {String name;byte[] templateHash,resultHash;Range[] ranges;}
    private static MessageDigest digest(){try{return MessageDigest.getInstance("SHA-256");}catch(NoSuchAlgorithmException e){throw new AssertionError(e);}}
    private static byte[] hash(byte[] b){return digest().digest(b);}
    private static String hex(byte[] b){char[] d="0123456789abcdef".toCharArray(),r=new char[b.length*2];for(int i=0;i<b.length;i++){r[2*i]=d[(b[i]&255)>>>4];r[2*i+1]=d[b[i]&15];}return new String(r);}
    private static byte[] read(InputStream in,int limit)throws IOException{ByteArrayOutputStream out=new ByteArrayOutputStream();byte[] b=new byte[65536];int total=0;for(int n;(n=in.read(b))!=-1;){if(n==0)continue;if(n>limit-total)throw new IOException("input too large");out.write(b,0,n);total+=n;}return out.toByteArray();}
    private static byte[] file(File f,int limit)throws IOException{try(InputStream in=new FileInputStream(f)){return read(in,limit);}}
    private static byte[] entry(ZipFile zip,ZipEntry e,int limit)throws IOException{if(e==null||e.isDirectory()||e.getSize()<0||e.getSize()>limit)throw new IOException("invalid ZIP entry");try(InputStream in=zip.getInputStream(e)){byte[] b=read(in,limit);if(b.length!=e.getSize())throw new IOException("truncated ZIP entry");return b;}}
    private static byte[] bytes(DataInputStream in,int n)throws IOException{byte[] b=new byte[n];in.readFully(b);return b;}
    private static String rootFor(String name,String suffix){if(name.equals(suffix))return "";if(name.equals("Megabonk_Data/"+suffix)||name.endsWith("/Megabonk_Data/"+suffix))return name.substring(0,name.length()-suffix.length());return null;}
    private static void collect(ZipFile zip,ZipEntry entry,long expected,byte[] wanted,int fileId,boolean windows,List<Mesh> meshes)throws IOException{
        if(entry==null||entry.isDirectory()||entry.getSize()!=expected)throw new IOException("invalid mesh source");
        MessageDigest hash=digest();byte[] buf=new byte[65536];long offset=0;
        try(InputStream in=zip.getInputStream(entry)){
            for(int n;(n=in.read(buf))!=-1;){
                if(n==0)continue;if(n>expected-offset)throw new IOException("mesh source too large");hash.update(buf,0,n);
                for(Mesh mesh:meshes)for(Range r:mesh.ranges){
                    int selected=windows?r.windowsFile:r.linuxFile;if(selected!=fileId)continue;
                    long start=windows?r.windowsOffset:r.linuxOffset,from=Math.max(start,offset),to=Math.min(start+r.size,offset+n);
                    if(from<to){int len=(int)(to-from);System.arraycopy(buf,(int)(from-offset),r.data,(int)(from-start),len);r.filled+=len;}
                }
                offset+=n;
            }
        }
        if(offset!=expected||!MessageDigest.isEqual(hash.digest(),wanted))throw new IOException("mesh source checksum differs");
    }
    public static Map<String,byte[]> restore(File baseFile,File steamFile,File planFile)throws IOException{
        if(!baseFile.isFile()||baseFile.length()>16*1024*1024||!compatibleBaseHash(hex(hash(file(baseFile,16*1024*1024)))))throw new IOException("incompatible base OBB");
        byte[] planBytes=file(planFile,1024*1024);if(!hex(hash(planBytes)).equals(PLAN_SHA))throw new IOException("incompatible mesh plan");
        if(!steamFile.isFile()||steamFile.length()<=0||steamFile.length()>1024L*1024*1024)throw new IOException("invalid Steam ZIP size");
        try(ZipFile base=new ZipFile(baseFile);ZipFile steam=new ZipFile(steamFile);DataInputStream plan=new DataInputStream(new ByteArrayInputStream(planBytes))){
            if(!Arrays.equals(bytes(plan,8),new byte[]{'O','B','S','M','E','0','0','1'})||plan.readInt()!=50)throw new IOException("invalid mesh plan");
            byte[] linuxAsset=bytes(plan,32),windowsAsset=bytes(plan,32),streamHash=bytes(plan,32);List<Mesh> meshes=new ArrayList<>();Set<String> outputs=new HashSet<>();long total=0;
            for(int i=0;i<50;i++){
                Mesh m=new Mesh();m.name=new String(bytes(plan,32),"US-ASCII");m.templateHash=bytes(plan,32);m.resultHash=bytes(plan,32);m.ranges=new Range[2];
                if(!m.name.matches("[0-9a-f]{32}")||!outputs.add(m.name))throw new IOException("invalid mesh name");
                for(int j=0;j<2;j++){
                    Range r=new Range();r.target=plan.readInt();r.size=plan.readInt();r.linuxFile=plan.readInt();r.linuxOffset=plan.readLong();r.windowsFile=plan.readInt();r.windowsOffset=plan.readLong();r.hash=bytes(plan,32);
                    if(r.target<0||r.size<=0||r.size>8*1024*1024||r.linuxFile<0||r.linuxFile>1||r.windowsFile<0||r.windowsFile>1||r.linuxOffset<0||r.windowsOffset<0||r.linuxOffset>(r.linuxFile==0?56239204L:STREAM_SIZE)-r.size||r.windowsOffset>(r.windowsFile==0?50251684L:STREAM_SIZE)-r.size)throw new IOException("invalid mesh range");
                    total+=r.size;if(total>16*1024*1024)throw new IOException("mesh limit exceeded");r.data=new byte[r.size];m.ranges[j]=r;
                }
                meshes.add(m);
            }
            if(plan.read()!=-1)throw new IOException("trailing mesh plan data");
            ZipEntry asset=null,stream=null;String assetRoot=null,streamRoot=null;Set<String> names=new HashSet<>();Enumeration<? extends ZipEntry> entries=steam.entries();
            while(entries.hasMoreElements()){
                ZipEntry e=entries.nextElement();String name=e.getName();
                if(names.size()>=20000||name.startsWith("/")||name.indexOf('\\')>=0||name.indexOf(':')>=0||!names.add(name))throw new IOException("unsafe or duplicate ZIP entry");
                for(String part:name.split("/"))if(part.isEmpty()||part.equals(".")||part.equals(".."))throw new IOException("unsafe ZIP path");
                String root=rootFor(name,"sharedassets1.assets");if(root!=null){if(asset!=null)throw new IOException("multiple asset roots");asset=e;assetRoot=root;}
                root=rootFor(name,"sharedassets1.assets.resS");if(root!=null){if(stream!=null)throw new IOException("multiple stream roots");stream=e;streamRoot=root;}
            }
            if(asset==null||stream==null||!assetRoot.equals(streamRoot))throw new IOException("missing or conflicting Steam data");
            boolean windows;if(asset.getSize()==50251684L)windows=true;else if(asset.getSize()==56239204L)windows=false;else throw new IOException("unsupported Steam asset");
            collect(steam,asset,asset.getSize(),windows?windowsAsset:linuxAsset,0,windows,meshes);
            collect(steam,stream,STREAM_SIZE,streamHash,1,windows,meshes);
            Map<String,byte[]> result=new LinkedHashMap<>();
            for(Mesh m:meshes){
                byte[] template=entry(base,base.getEntry("assets/bin/Data/"+m.name),8*1024*1024);
                if(!MessageDigest.isEqual(hash(template),m.templateHash))throw new IOException("mesh template differs");
                BitSet used=new BitSet(template.length);
                for(Range r:m.ranges){
                    if(r.filled!=r.size||!MessageDigest.isEqual(hash(r.data),r.hash)||r.target>template.length-r.size)throw new IOException("mesh range differs");
                    int overlap=used.nextSetBit(r.target);if(overlap>=0&&overlap<r.target+r.size)throw new IOException("overlapping mesh ranges");
                    for(int i=r.target;i<r.target+r.size;i++)if(template[i]!=0)throw new IOException("mesh target not blank");
                    System.arraycopy(r.data,0,template,r.target,r.size);used.set(r.target,r.target+r.size);
                }
                if(!MessageDigest.isEqual(hash(template),m.resultHash))throw new IOException("mesh result differs");result.put(m.name,template);
            }
            return result;
        }
    }
    public static void restoreToDirectory(File base,File steam,File plan,File output)throws IOException{
        Path dest=output.toPath().toAbsolutePath(),parent=dest.getParent();
        if(parent==null||!Files.isDirectory(parent)||Files.exists(dest,LinkOption.NOFOLLOW_LINKS))throw new IOException("output must be a new directory");
        Map<String,byte[]> files=restore(base,steam,plan);Path staged=Files.createTempDirectory(parent,".openbonk-mesh-");
        try{
            for(Map.Entry<String,byte[]> item:files.entrySet()){
                Path path=staged.resolve(item.getKey());Files.write(path,item.getValue(),StandardOpenOption.CREATE_NEW,StandardOpenOption.WRITE);
                if(!MessageDigest.isEqual(hash(file(path.toFile(),8*1024*1024)),hash(item.getValue())))throw new IOException("staged mesh differs");
            }
            Files.move(staged,dest);
        }finally{if(Files.exists(staged,LinkOption.NOFOLLOW_LINKS)){for(String name:files.keySet())Files.deleteIfExists(staged.resolve(name));Files.deleteIfExists(staged);}}
    }
    private SteamMeshResources(){}
}
