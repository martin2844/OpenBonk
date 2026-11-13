package com.openbonk.dataprep;

import java.io.*;
import java.nio.file.*;
import java.security.*;
import java.util.*;
import java.util.zip.*;
import static com.openbonk.dataprep.SteamExactTextures.*;

/** Restores seven font payloads in an incomplete sharedassets0 asset. */
public final class SteamShared0Fonts {
    private static final String PLAN="c042ba2ce6c82430c4975ec04ba7256c7ef482cd426a500599466b812f250b07";
    private static final String SCRUBBED="b244a5c73a1e9ed1ddbb3a4de9125c4ded769ef9cf9f60067b041ad2c3969418";
    private static final String SKY_STREAM="374d7bd078c7428c9a1815d3ef3d942a26353c7e3af63258aa55dbb9837cee7b";
    private static final String SKY_HASH="b7607c1b9a4cd75ca0c688665e68429c846abada785dc63c9d367f3759efefdf";
    private static final int SKY_TARGET=98689760, SKY_OFFSET=364576, SKY_SIZE=524280, SKY_STREAM_SIZE=888864;
    private static final int ALAGARD_TARGET=132021416, ALAGARD_SIZE=7480, ALAGARD_LINUX=131473648, ALAGARD_WINDOWS=131692136;
    private static final String ALAGARD_LINUX_SHA="4a26dc893b780dacfb9aea03d2daa590a995cad6ad0038e278bd50ebc4441e90";
    private static final String ALAGARD_WINDOWS_SHA="dd0223fecbe92fc98980be04306da2505a1919d1049468f35aeaa7f4cefaec69";
    private static final String ALAGARD_TARGET_SHA="822b721abc587cd2b463138007ea4f0ba162d92dc64c44ad52c8f83c242bb3c8";
    private static final int LENGTH=132028896, SPLIT=1048576, COUNT=126;
    private static final class Range {long target,linux,windows;int size,filled;byte[] hash;}
    private static MessageDigest digest(){try{return MessageDigest.getInstance("SHA-256");}catch(NoSuchAlgorithmException e){throw new AssertionError(e);}}
    private static byte[] checksum(RandomAccessFile file,long start,int size)throws IOException{
        MessageDigest h=digest();byte[] buffer=new byte[65536];file.seek(start);
        for(int left=size;left>0;){int n=Math.min(left,buffer.length);file.readFully(buffer,0,n);h.update(buffer,0,n);left-=n;}return h.digest();
    }
    public static void restore(File apkFile,File steamFile,File planFile,File output)throws IOException{
        byte[] planData=file(planFile,4096);if(!hex(hash(planData)).equals(PLAN))throw new IOException("incompatible font plan");
        Path dest=output.toPath().toAbsolutePath(),parent=dest.getParent();
        if(parent==null||!Files.isDirectory(parent)||Files.exists(dest,LinkOption.NOFOLLOW_LINKS))throw new IOException("output must be new");
        try(DataInputStream plan=new DataInputStream(new ByteArrayInputStream(planData))){
            if(!Arrays.equals(bytes(plan,8),new byte[]{'O','B','S','0','F','0','0','1'})||plan.readInt()!=LENGTH||plan.readInt()!=7)throw new IOException("invalid font plan");
            byte[] baseHash=bytes(plan,32),resultHash=bytes(plan,32);long linuxSize=plan.readLong();byte[] linuxHash=bytes(plan,32);long windowsSize=plan.readLong();byte[] windowsHash=bytes(plan,32);
            List<Range> ranges=new ArrayList<>();long previous=0;
            for(int i=0;i<7;i++){Range r=new Range();r.target=plan.readLong();r.size=plan.readInt();r.linux=plan.readLong();r.windows=plan.readLong();r.hash=bytes(plan,32);
                if(r.size<=0||r.target<previous||r.target>LENGTH-r.size||r.linux<0||r.windows<0||r.linux>linuxSize-r.size||r.windows>windowsSize-r.size)throw new IOException("invalid font range");previous=r.target+r.size;ranges.add(r);}
            if(plan.read()!=-1)throw new IOException("trailing font plan");
            Path temp=Files.createTempFile(parent,".openbonk-shared0-",".tmp");boolean moved=false;
            try(RandomAccessFile work=new RandomAccessFile(temp.toFile(),"rw");ZipFile apk=new ZipFile(apkFile);ZipFile steam=new ZipFile(steamFile)){
                Set<String> seen=new HashSet<>();Enumeration<? extends ZipEntry> entries=apk.entries();
                while(entries.hasMoreElements())if(!seen.add(entries.nextElement().getName()))throw new IOException("duplicate APK entry");
                MessageDigest base=digest();byte[] buffer=new byte[65536];long total=0;
                for(int i=0;i<COUNT;i++){
                    ZipEntry e=apk.getEntry("assets/bin/Data/sharedassets0.assets.split"+i);int expected=Math.min(SPLIT,LENGTH-i*SPLIT);
                    if(e==null||e.isDirectory()||e.getSize()!=expected)throw new IOException("missing base asset split");int count=0;
                    try(InputStream in=apk.getInputStream(e)){for(int n;(n=in.read(buffer))!=-1;){if(n==0)continue;if(n>expected-count)throw new IOException("oversized base split");work.write(buffer,0,n);base.update(buffer,0,n);count+=n;}}
                    if(count!=expected)throw new IOException("truncated base split");total+=count;
                }
                if(total!=LENGTH||!hex(base.digest()).equals(SCRUBBED))throw new IOException("base asset differs");
                ZipEntry source=null,sky=null;String sourceRoot=null,skyRoot=null;seen.clear();entries=steam.entries();
                while(entries.hasMoreElements()){
                    ZipEntry e=entries.nextElement();String name=e.getName();
                    if(seen.size()>=20000||name.startsWith("/")||name.indexOf('\\')>=0||name.indexOf(':')>=0||!seen.add(name))throw new IOException("unsafe Steam ZIP");
                    for(String part:name.split("/"))if(part.isEmpty()||part.equals(".")||part.equals(".."))throw new IOException("unsafe Steam path");
                    String root=rootFor(name,"sharedassets0.assets");
                    if(root!=null){if(source!=null)throw new IOException("duplicate Steam asset");source=e;sourceRoot=root;}
                    root=rootFor(name,"sharedassets0.assets.resS");
                    if(root!=null){if(sky!=null)throw new IOException("duplicate sky stream");sky=e;skyRoot=root;}
                }
                if(source==null||source.isDirectory())throw new IOException("missing Steam asset");
                if(sky==null||sky.isDirectory()||sky.getSize()!=SKY_STREAM_SIZE||!sourceRoot.equals(skyRoot))throw new IOException("missing sky stream");
                byte[] skyBytes=new byte[SKY_STREAM_SIZE];
                try(DataInputStream in=new DataInputStream(steam.getInputStream(sky))){in.readFully(skyBytes);if(in.read()!=-1)throw new IOException("oversized sky stream");}
                if(!hex(hash(skyBytes)).equals(SKY_STREAM)||!hex(hash(Arrays.copyOfRange(skyBytes,SKY_OFFSET,SKY_OFFSET+SKY_SIZE))).equals(SKY_HASH))throw new IOException("sky stream differs");
                work.seek(SKY_TARGET);work.write(skyBytes,SKY_OFFSET,SKY_SIZE);
                boolean windows;if(source.getSize()==linuxSize)windows=false;else if(source.getSize()==windowsSize)windows=true;else throw new IOException("unsupported Steam asset");
                byte[] alagard=new byte[ALAGARD_SIZE];int alagardFilled=0;long alagardOffset=windows?ALAGARD_WINDOWS:ALAGARD_LINUX;
                MessageDigest input=digest();long pos=0,sourceSize=windows?windowsSize:linuxSize;
                try(InputStream in=steam.getInputStream(source)){for(int n;(n=in.read(buffer))!=-1;){
                    if(n==0)continue;if(n>sourceSize-pos)throw new IOException("oversized Steam asset");input.update(buffer,0,n);
                    long fontFrom=Math.max(pos,alagardOffset),fontTo=Math.min(pos+n,alagardOffset+ALAGARD_SIZE);
                    if(fontFrom<fontTo){int count=(int)(fontTo-fontFrom);System.arraycopy(buffer,(int)(fontFrom-pos),alagard,(int)(fontFrom-alagardOffset),count);alagardFilled+=count;}
                    for(Range r:ranges){long start=windows?r.windows:r.linux,from=Math.max(pos,start),to=Math.min(pos+n,start+r.size);if(from<to){int count=(int)(to-from);work.seek(r.target+from-start);work.write(buffer,(int)(from-pos),count);r.filled+=count;}}
                    pos+=n;
                }}
                if(pos!=sourceSize||!MessageDigest.isEqual(input.digest(),windows?windowsHash:linuxHash))throw new IOException("Steam asset differs");
                if(alagardFilled!=ALAGARD_SIZE||!hex(hash(alagard)).equals(windows?ALAGARD_WINDOWS_SHA:ALAGARD_LINUX_SHA))throw new IOException("font metadata differs");
                int[] referenceOffsets={20,160,376,6872,6884,7092};int[] referenceValues={56,9,126,24,114,131};
                for(int i=0;i<referenceOffsets.length;i++)alagard[referenceOffsets[i]]=(byte)referenceValues[i];
                if(!hex(hash(alagard)).equals(ALAGARD_TARGET_SHA))throw new IOException("font metadata reconstruction differs");
                work.seek(ALAGARD_TARGET);work.write(alagard);
                for(Range r:ranges)if(r.filled!=r.size||!MessageDigest.isEqual(checksum(work,r.target,r.size),r.hash))throw new IOException("font differs");
                if(!MessageDigest.isEqual(checksum(work,0,LENGTH),resultHash))throw new IOException("reconstructed asset differs");
                work.getFD().sync();
                Files.move(temp,dest);moved=true;
            }finally{if(!moved)Files.deleteIfExists(temp);}
        }
    }
    private SteamShared0Fonts(){}
}
