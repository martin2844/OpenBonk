package com.openbonk.dataprep;

import java.io.*;
import java.nio.*;
import java.nio.file.*;
import java.security.*;
import java.util.*;
import java.util.zip.*;
import static com.openbonk.dataprep.SteamExactTextures.*;

/** Converts 94 font atlases in an incomplete sharedassets0 asset. */
public final class SteamShared0Atlases {
    private static final String PLAN="bbfdd475dad5adcdaf837752577f966d215676606c5c1650009a7f400bf0b0db";
    private static final int LENGTH=132028896,PIXELS=1048576;
    private static final class Atlas {int dst,header,filled;long linux,windows;byte[] headHash,pixelHash,objectHash,data;}
    private static MessageDigest digest(){try{return MessageDigest.getInstance("SHA-256");}catch(NoSuchAlgorithmException e){throw new AssertionError(e);}}
    private static byte[] fileHash(RandomAccessFile f)throws IOException{
        MessageDigest h=digest();byte[] b=new byte[65536];f.seek(0);for(int n;(n=f.read(b))!=-1;)if(n>0)h.update(b,0,n);return h.digest();
    }
    private static void convert(Atlas a,RandomAccessFile work)throws IOException{
        if(!MessageDigest.isEqual(hash(Arrays.copyOfRange(a.data,0,a.header)),a.headHash)||!MessageDigest.isEqual(hash(Arrays.copyOfRange(a.data,a.header,a.data.length)),a.pixelHash))throw new IOException("atlas source differs");
        byte[] head=Arrays.copyOf(a.data,a.header);ByteBuffer h=ByteBuffer.wrap(head).order(ByteOrder.LITTLE_ENDIAN);int name=h.getInt(0),end=4+((name+3)&~3);
        if(name<0||name>256||end+31>head.length||h.getInt(end+20)!=1||head[end+30]!=1)throw new IOException("invalid atlas header");
        h.putInt(end+20,47);head[end+30]=0;
        byte[] converted=SteamConvertedTextures.convertMip(Arrays.copyOfRange(a.data,a.header,a.data.length),1024,1024,1,47);
        if(converted==null||converted.length!=PIXELS)throw new IOException("atlas conversion failed");
        MessageDigest hash=digest();hash.update(head);hash.update(converted);hash.update(new byte[16]);if(!MessageDigest.isEqual(hash.digest(),a.objectHash))throw new IOException("converted atlas differs");
        work.seek(a.dst);byte[] check=new byte[65536];int left=a.header+PIXELS+16;
        while(left>0){int n=Math.min(left,check.length);work.readFully(check,0,n);for(int i=0;i<n;i++)if(check[i]!=0)throw new IOException("atlas target not blank");left-=n;}
        work.seek(a.dst);work.write(head);work.write(converted);work.write(new byte[16]);a.data=null;
    }
    public static void restore(File baseFile,File steamFile,File planFile,File output)throws IOException{
        byte[] planData=file(planFile,32768);if(!hex(hash(planData)).equals(PLAN))throw new IOException("incompatible atlas plan");
        Path dest=output.toPath().toAbsolutePath(),parent=dest.getParent();if(parent==null||!Files.isDirectory(parent)||Files.exists(dest,LinkOption.NOFOLLOW_LINKS))throw new IOException("output must be new");
        try(DataInputStream plan=new DataInputStream(new ByteArrayInputStream(planData))){
            if(!Arrays.equals(bytes(plan,8),new byte[]{'O','B','S','0','A','0','0','1'})||plan.readInt()!=LENGTH||plan.readInt()!=94)throw new IOException("invalid atlas plan");
            byte[] baseHash=bytes(plan,32),resultHash=bytes(plan,32);long linuxSize=plan.readLong();byte[] linuxHash=bytes(plan,32);long windowsSize=plan.readLong();byte[] windowsHash=bytes(plan,32);
            List<Atlas> atlases=new ArrayList<>();long last=0;
            for(int i=0;i<94;i++){Atlas a=new Atlas();a.dst=plan.readInt();a.header=plan.readInt();a.linux=plan.readLong();a.windows=plan.readLong();a.headHash=bytes(plan,32);a.pixelHash=bytes(plan,32);a.objectHash=bytes(plan,32);long size=(long)a.header+PIXELS+16;
                if(a.dst<last||a.header<80||a.header>512||a.dst>LENGTH-size||a.linux<0||a.windows<0||a.linux>linuxSize-a.header-PIXELS||a.windows>windowsSize-a.header-PIXELS)throw new IOException("invalid atlas range");last=a.dst+size;atlases.add(a);}
            if(plan.read()!=-1)throw new IOException("trailing atlas plan");
            if(Files.isSymbolicLink(baseFile.toPath())||!Files.isRegularFile(baseFile.toPath(),LinkOption.NOFOLLOW_LINKS)||baseFile.length()!=LENGTH)throw new IOException("invalid base asset");
            Path temp=Files.createTempFile(parent,".openbonk-atlas-",".tmp");boolean moved=false;
            try(RandomAccessFile work=new RandomAccessFile(temp.toFile(),"rw");InputStream base=Files.newInputStream(baseFile.toPath());ZipFile steam=new ZipFile(steamFile)){
                MessageDigest baseDigest=digest();byte[] buffer=new byte[65536];long copied=0;
                for(int n;(n=base.read(buffer))!=-1;){if(n==0)continue;if(n>LENGTH-copied)throw new IOException("base too large");work.write(buffer,0,n);baseDigest.update(buffer,0,n);copied+=n;}
                if(copied!=LENGTH||!MessageDigest.isEqual(baseDigest.digest(),baseHash))throw new IOException("base asset differs");
                Set<String> names=new HashSet<>();ZipEntry source=null;Enumeration<? extends ZipEntry> entries=steam.entries();
                while(entries.hasMoreElements()){ZipEntry e=entries.nextElement();String n=e.getName();if(names.size()>=20000||n.startsWith("/")||n.indexOf('\\')>=0||n.indexOf(':')>=0||!names.add(n))throw new IOException("unsafe Steam ZIP");for(String part:n.split("/"))if(part.isEmpty()||part.equals(".")||part.equals(".."))throw new IOException("unsafe Steam path");if(rootFor(n,"sharedassets0.assets")!=null){if(source!=null)throw new IOException("duplicate Steam asset");source=e;}}
                if(source==null||source.isDirectory())throw new IOException("missing Steam asset");boolean windows;if(source.getSize()==linuxSize)windows=false;else if(source.getSize()==windowsSize)windows=true;else throw new IOException("unsupported Steam asset");
                long size=windows?windowsSize:linuxSize,pos=0;MessageDigest sourceDigest=digest();
                try(InputStream in=steam.getInputStream(source)){for(int n;(n=in.read(buffer))!=-1;){if(n==0)continue;if(n>size-pos)throw new IOException("Steam asset too large");sourceDigest.update(buffer,0,n);
                    for(Atlas a:atlases){long start=windows?a.windows:a.linux,from=Math.max(pos,start),to=Math.min(pos+n,start+a.header+PIXELS);if(from<to){if(a.data==null)a.data=new byte[a.header+PIXELS];int count=(int)(to-from);System.arraycopy(buffer,(int)(from-pos),a.data,(int)(from-start),count);a.filled+=count;if(a.filled==a.data.length)convert(a,work);}}
                    pos+=n;}}
                if(pos!=size||!MessageDigest.isEqual(sourceDigest.digest(),windows?windowsHash:linuxHash))throw new IOException("Steam asset differs");
                for(Atlas a:atlases)if(a.filled!=a.header+PIXELS||a.data!=null)throw new IOException("incomplete atlas");
                if(work.length()!=LENGTH||!MessageDigest.isEqual(fileHash(work),resultHash))throw new IOException("reconstructed asset differs");
                work.getFD().sync();Files.move(temp,dest);moved=true;
            }finally{if(!moved)Files.deleteIfExists(temp);}
        }
    }
    private SteamShared0Atlases(){}
}
