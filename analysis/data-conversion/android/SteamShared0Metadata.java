package com.openbonk.dataprep;

import java.io.*;
import java.nio.file.*;
import java.security.*;
import java.util.*;
import java.util.zip.*;
import static com.openbonk.dataprep.SteamExactTextures.*;

/** Restores local diagnostic font references and material values from Steam. */
public final class SteamShared0Metadata {
    private static final String PLAN="ff8050d91f5aa6f72adab1c360a719725e4620bb0aafe358ce8ba4917708edf0";
    private static final int LENGTH=132028896;
    private static final class Edit {int offset,value;}
    private static final class Range {int dst,size,filled;long linux,windows;byte[][] sourceHash=new byte[2][];byte[] targetHash,data;List<Edit>[] edits;}
    private static MessageDigest digest(){try{return MessageDigest.getInstance("SHA-256");}catch(NoSuchAlgorithmException e){throw new AssertionError(e);}}
    private static byte[] fileHash(RandomAccessFile f)throws IOException{MessageDigest h=digest();byte[] b=new byte[65536];f.seek(0);for(int n;(n=f.read(b))!=-1;)if(n>0)h.update(b,0,n);return h.digest();}
    private static void restoreRange(Range r,boolean windows,RandomAccessFile work)throws IOException{
        int platform=windows?1:0;if(!MessageDigest.isEqual(hash(r.data),r.sourceHash[platform]))throw new IOException("metadata source differs");
        for(Edit e:r.edits[platform]){if((r.data[e.offset]&255)==e.value)throw new IOException("reference edit is redundant");r.data[e.offset]=(byte)e.value;}
        if(!MessageDigest.isEqual(hash(r.data),r.targetHash))throw new IOException("metadata reconstruction differs");
        work.seek(r.dst);byte[] previous=new byte[65536];for(int offset=0;offset<r.size;){int n=Math.min(previous.length,r.size-offset);work.readFully(previous,0,n);for(int i=0;i<n;i++)if(previous[i]!=0&&previous[i]!=r.data[offset+i])throw new IOException("retained metadata differs");offset+=n;}
        work.seek(r.dst);work.write(r.data);r.data=null;
    }
    @SuppressWarnings("unchecked")
    public static void restore(File baseFile,File steamFile,File planFile,File output)throws IOException{
        byte[] planData=file(planFile,32768);if(!hex(hash(planData)).equals(PLAN))throw new IOException("incompatible metadata plan");
        Path dest=output.toPath().toAbsolutePath(),parent=dest.getParent();if(parent==null||!Files.isDirectory(parent)||Files.exists(dest,LinkOption.NOFOLLOW_LINKS))throw new IOException("output must be new");
        try(DataInputStream plan=new DataInputStream(new ByteArrayInputStream(planData))){
            if(!Arrays.equals(bytes(plan,8),new byte[]{'O','B','S','0','M','0','0','1'})||plan.readInt()!=LENGTH||plan.readInt()!=29)throw new IOException("invalid metadata plan");
            byte[] baseHash=bytes(plan,32),resultHash=bytes(plan,32);long linuxSize=plan.readLong();byte[] linuxHash=bytes(plan,32);long windowsSize=plan.readLong();byte[] windowsHash=bytes(plan,32);
            List<Range> ranges=new ArrayList<>();long last=0;
            for(int i=0;i<29;i++){
                Range r=new Range();r.dst=plan.readInt();r.size=plan.readInt();r.linux=plan.readLong();r.windows=plan.readLong();r.sourceHash[0]=bytes(plan,32);r.sourceHash[1]=bytes(plan,32);r.targetHash=bytes(plan,32);
                if(r.size<=0||r.size>12*1024*1024||r.dst<last||r.dst>LENGTH-r.size||r.linux<0||r.windows<0||r.linux>linuxSize-r.size||r.windows>windowsSize-r.size)throw new IOException("invalid metadata range");last=(long)r.dst+r.size;
                r.edits=(List<Edit>[])new List<?>[2];for(int platform=0;platform<2;platform++){int n=plan.readInt(),prior=-1;if(n<0||n>128)throw new IOException("invalid edit count");r.edits[platform]=new ArrayList<>();for(int j=0;j<n;j++){Edit e=new Edit();e.offset=plan.readInt();e.value=plan.readUnsignedByte();if(e.offset<=prior||e.offset>=r.size)throw new IOException("invalid edit offset");prior=e.offset;r.edits[platform].add(e);}}
                ranges.add(r);
            }
            if(plan.read()!=-1)throw new IOException("trailing metadata plan");
            if(Files.isSymbolicLink(baseFile.toPath())||!Files.isRegularFile(baseFile.toPath(),LinkOption.NOFOLLOW_LINKS)||baseFile.length()!=LENGTH)throw new IOException("invalid base asset");
            Path temp=Files.createTempFile(parent,".openbonk-metadata-",".tmp");boolean moved=false;
            try(RandomAccessFile work=new RandomAccessFile(temp.toFile(),"rw");InputStream base=Files.newInputStream(baseFile.toPath());ZipFile steam=new ZipFile(steamFile)){
                MessageDigest baseDigest=digest();byte[] buffer=new byte[65536];long copied=0;for(int n;(n=base.read(buffer))!=-1;){if(n==0)continue;if(n>LENGTH-copied)throw new IOException("base too large");work.write(buffer,0,n);baseDigest.update(buffer,0,n);copied+=n;}
                if(copied!=LENGTH||!MessageDigest.isEqual(baseDigest.digest(),baseHash))throw new IOException("base asset differs");
                Set<String> names=new HashSet<>();ZipEntry source=null;Enumeration<? extends ZipEntry> entries=steam.entries();
                while(entries.hasMoreElements()){ZipEntry e=entries.nextElement();String n=e.getName();if(names.size()>=20000||n.startsWith("/")||n.indexOf('\\')>=0||n.indexOf(':')>=0||!names.add(n))throw new IOException("unsafe Steam ZIP");for(String part:n.split("/"))if(part.isEmpty()||part.equals(".")||part.equals(".."))throw new IOException("unsafe Steam path");if(rootFor(n,"sharedassets0.assets")!=null){if(source!=null)throw new IOException("duplicate Steam asset");source=e;}}
                if(source==null||source.isDirectory())throw new IOException("missing Steam asset");boolean windows;if(source.getSize()==linuxSize)windows=false;else if(source.getSize()==windowsSize)windows=true;else throw new IOException("unsupported Steam asset");
                long size=windows?windowsSize:linuxSize,pos=0;MessageDigest sourceDigest=digest();
                try(InputStream in=steam.getInputStream(source)){for(int n;(n=in.read(buffer))!=-1;){if(n==0)continue;if(n>size-pos)throw new IOException("Steam asset too large");sourceDigest.update(buffer,0,n);
                    for(Range r:ranges){long start=windows?r.windows:r.linux,from=Math.max(pos,start),to=Math.min(pos+n,start+r.size);if(from<to){if(r.data==null)r.data=new byte[r.size];int count=(int)(to-from);System.arraycopy(buffer,(int)(from-pos),r.data,(int)(from-start),count);r.filled+=count;if(r.filled==r.size)restoreRange(r,windows,work);}}
                    pos+=n;}}
                if(pos!=size||!MessageDigest.isEqual(sourceDigest.digest(),windows?windowsHash:linuxHash))throw new IOException("Steam asset differs");
                for(Range r:ranges)if(r.filled!=r.size||r.data!=null)throw new IOException("incomplete metadata");
                if(work.length()!=LENGTH||!MessageDigest.isEqual(fileHash(work),resultHash))throw new IOException("reconstructed asset differs");work.getFD().sync();Files.move(temp,dest);moved=true;
            }finally{if(!moved)Files.deleteIfExists(temp);}
        }
    }
    private SteamShared0Metadata(){}
}
