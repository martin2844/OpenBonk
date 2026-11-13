package com.openbonk.dataprep;
import java.io.*;
import java.nio.file.*;
import java.security.*;
import java.util.*;
import java.util.zip.*;
import static com.openbonk.dataprep.SteamExactTextures.*;

/** Stages locally compiled shaders in an incomplete private diagnostic asset. */
public final class SteamShared1Shaders {
    private static final String PLAN="673f1003a80d65b951c96dcb09fa6401fe72d9c6c2f9cffb55658a6da34bd9d1";
    private static final int LENGTH=122379840;
    private static final class Row {int dst,size,lo,hi;byte[] sourceHash,targetHash,referenceHash;}
    private static MessageDigest digest(){try{return MessageDigest.getInstance("SHA-256");}catch(NoSuchAlgorithmException e){throw new AssertionError(e);}}
    private static byte[] fileHash(RandomAccessFile f)throws IOException{MessageDigest h=digest();byte[] b=new byte[65536];f.seek(0);for(int n;(n=f.read(b))!=-1;)if(n>0)h.update(b,0,n);return h.digest();}
    public static void restore(File baseFile,File packFile,File planFile,File output)throws IOException{
        byte[] planData=file(planFile,16384);if(!hex(hash(planData)).equals(PLAN))throw new IOException("incompatible shader plan");Path dest=output.toPath().toAbsolutePath(),parent=dest.getParent();if(parent==null||!Files.isDirectory(parent)||Files.exists(dest,LinkOption.NOFOLLOW_LINKS))throw new IOException("output must be new");
        try(DataInputStream p=new DataInputStream(new ByteArrayInputStream(planData))){
            if(!Arrays.equals(bytes(p,8),new byte[]{'O','B','S','1','S','0','0','1'})||p.readInt()!=LENGTH||p.readInt()!=55)throw new IOException("invalid shader plan");byte[] baseHash=bytes(p,32),resultHash=bytes(p,32),packHash=bytes(p,32);List<Row> rows=new ArrayList<>();long previous=0,total=0;
            for(int i=0;i<55;i++){Row r=new Row();r.dst=p.readInt();r.size=p.readInt();r.lo=p.readInt();r.hi=p.readInt();r.sourceHash=bytes(p,32);r.targetHash=bytes(p,32);r.referenceHash=bytes(p,32);if(r.dst<previous||r.size<=0||r.size>2*1024*1024||r.dst>LENGTH-r.size||r.lo<0||r.hi<r.lo||r.hi>r.size)throw new IOException("invalid shader range");previous=(long)r.dst+r.size;total+=r.size;rows.add(r);}
            if(p.read()!=-1||total>8*1024*1024)throw new IOException("invalid shader data");if(Files.isSymbolicLink(baseFile.toPath())||!Files.isRegularFile(baseFile.toPath(),LinkOption.NOFOLLOW_LINKS)||baseFile.length()!=LENGTH)throw new IOException("invalid base asset");
            if(!packFile.isFile()||packFile.length()>8*1024*1024||!MessageDigest.isEqual(hash(file(packFile,8*1024*1024)),packHash))throw new IOException("shader pack differs");
            Path temp=Files.createTempFile(parent,".openbonk-shared1-shaders-",".tmp");boolean moved=false;
            try(RandomAccessFile work=new RandomAccessFile(temp.toFile(),"rw");InputStream base=Files.newInputStream(baseFile.toPath());ZipFile pack=new ZipFile(packFile)){
                byte[] buffer=new byte[65536];MessageDigest bh=digest();long count=0;for(int n;(n=base.read(buffer))!=-1;){if(n==0)continue;if(n>LENGTH-count)throw new IOException("base too large");work.write(buffer,0,n);bh.update(buffer,0,n);count+=n;}if(count!=LENGTH||!MessageDigest.isEqual(bh.digest(),baseHash))throw new IOException("base asset differs");
                Set<String> names=new HashSet<>();Enumeration<? extends ZipEntry> all=pack.entries();while(all.hasMoreElements())if(!names.add(all.nextElement().getName()))throw new IOException("duplicate shader entry");if(names.size()!=55)throw new IOException("shader count differs");
                for(int i=0;i<rows.size();i++){
                    Row r=rows.get(i);String name=String.format(Locale.ROOT,"shader-%03d.bin",i);ZipEntry entry=pack.getEntry(name);byte[] value=entry(pack,entry,2*1024*1024);if(value.length!=r.size||!MessageDigest.isEqual(hash(value),r.sourceHash))throw new IOException("generated shader differs");
                    byte[] old=new byte[r.size];work.seek(r.dst);work.readFully(old);for(int j=0;j<r.lo;j++)if(old[j]!=0)throw new IOException("shader target not blank");for(int j=r.hi;j<r.size;j++)if(old[j]!=0)throw new IOException("shader target not blank");
                    byte[] ref=Arrays.copyOfRange(old,r.lo,r.hi);if(!MessageDigest.isEqual(hash(ref),r.referenceHash))throw new IOException("shader references differ");System.arraycopy(ref,0,value,r.lo,ref.length);if(!MessageDigest.isEqual(hash(value),r.targetHash))throw new IOException("reconstructed shader differs");work.seek(r.dst);work.write(value);
                }
                if(work.length()!=LENGTH||!MessageDigest.isEqual(fileHash(work),resultHash))throw new IOException("partial shared1 asset differs");work.getFD().sync();Files.move(temp,dest);moved=true;
            }finally{if(!moved)Files.deleteIfExists(temp);}
        }
    }
    private SteamShared1Shaders(){}
}
