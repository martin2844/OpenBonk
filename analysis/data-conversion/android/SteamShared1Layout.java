package com.openbonk.dataprep;
import java.io.*;
import java.nio.*;
import java.nio.file.*;
import java.security.*;
import java.util.*;
import java.util.zip.*;
import static com.openbonk.dataprep.SteamExactTextures.*;

/** Rebuilds the resized object table and blank texture placeholders for shared1. */
public final class SteamShared1Layout {
    private static final String PLAN="0142daaaf1e07c3a9aa90dac01382f8fba6cce74983f669376b01fce1fff6ed0";
    private static final int BASE_SIZE=112604540,OUT_SIZE=122379840,DATA_OFFSET=194384,TABLE=6972,OBJECTS=6368,SPLIT=1048576;
    private static final class Resize{int oldSize,newSize;}
    private static MessageDigest digest(){try{return MessageDigest.getInstance("SHA-256");}catch(NoSuchAlgorithmException e){throw new AssertionError(e);}}
    private static byte[] fileHash(RandomAccessFile f)throws IOException{MessageDigest h=digest();byte[] b=new byte[65536];f.seek(0);for(int n;(n=f.read(b))!=-1;)if(n>0)h.update(b,0,n);return h.digest();}
    public static void restore(File baseObb,File planFile,File output)throws IOException{
        byte[] rawPlan=file(planFile,32768);if(!hex(hash(rawPlan)).equals(PLAN))throw new IOException("incompatible layout plan");Path dest=output.toPath().toAbsolutePath(),parent=dest.getParent();if(parent==null||!Files.isDirectory(parent)||Files.exists(dest,LinkOption.NOFOLLOW_LINKS))throw new IOException("output must be new");
        try(DataInputStream plan=new DataInputStream(new ByteArrayInputStream(rawPlan))){
            if(!Arrays.equals(bytes(plan,8),new byte[]{'O','B','S','1','L','0','0','1'})||plan.readInt()!=BASE_SIZE||plan.readInt()!=OUT_SIZE||plan.readInt()!=295)throw new IOException("invalid layout plan");byte[] baseHash=bytes(plan,32),outHash=bytes(plan,32);Map<Long,Resize> resized=new HashMap<>();long previous=0;
            for(int i=0;i<295;i++){long id=plan.readLong();Resize r=new Resize();r.oldSize=plan.readInt();r.newSize=plan.readInt();if(id<=previous||id>OBJECTS||r.oldSize<=0||r.newSize<=r.oldSize||r.newSize>8*1024*1024||resized.put(id,r)!=null)throw new IOException("invalid resize");previous=id;}
            if(plan.read()!=-1)throw new IOException("trailing layout plan");
            Path source=Files.createTempFile(parent,".openbonk-shared1-source-",".tmp"),result=null;boolean moved=false;
            try{
                result=Files.createTempFile(parent,".openbonk-shared1-layout-",".tmp");byte[] buffer=new byte[65536];MessageDigest baseDigest=digest();long total=0;
                try(ZipFile zip=new ZipFile(baseObb);OutputStream out=Files.newOutputStream(source)){
                    Set<String> seen=new HashSet<>();Enumeration<? extends ZipEntry> entries=zip.entries();while(entries.hasMoreElements())if(!seen.add(entries.nextElement().getName()))throw new IOException("duplicate base entry");
                    for(int i=0;i<108;i++){ZipEntry e=zip.getEntry("assets/bin/Data/sharedassets1.assets.split"+i);int size=Math.min(SPLIT,BASE_SIZE-i*SPLIT),count=0;if(e==null||e.isDirectory()||e.getSize()!=size)throw new IOException("invalid base split");try(InputStream in=zip.getInputStream(e)){for(int n;(n=in.read(buffer))!=-1;){if(n==0)continue;if(n>size-count)throw new IOException("base split too large");out.write(buffer,0,n);baseDigest.update(buffer,0,n);count+=n;}}if(count!=size)throw new IOException("base split truncated");total+=count;}
                }
                if(total!=BASE_SIZE||!MessageDigest.isEqual(baseDigest.digest(),baseHash))throw new IOException("base asset differs");
                try(RandomAccessFile in=new RandomAccessFile(source.toFile(),"r");RandomAccessFile out=new RandomAccessFile(result.toFile(),"rw")){
                    byte[] metadata=new byte[DATA_OFFSET];in.readFully(metadata);ByteBuffer little=ByteBuffer.wrap(metadata).order(ByteOrder.LITTLE_ENDIAN),big=ByteBuffer.wrap(metadata).order(ByteOrder.BIG_ENDIAN);
                    if(big.getInt(8)!=22||big.getLong(24)!=BASE_SIZE||big.getLong(32)!=DATA_OFFSET)throw new IOException("unexpected serialized header");big.putLong(24,OUT_SIZE);
                    long oldPos=0,newPos=0;int changed=0;
                    for(int i=0;i<OBJECTS;i++){int t=TABLE+i*24;long id=little.getLong(t),oldOffset=little.getLong(t+8);int oldSize=little.getInt(t+16);if(id!=i+1L||oldOffset!=oldPos||oldSize<=0||oldOffset>BASE_SIZE-DATA_OFFSET-oldSize)throw new IOException("invalid object table");Resize r=resized.get(id);int newSize=oldSize;if(r!=null){if(r.oldSize!=oldSize)throw new IOException("resize source differs");newSize=r.newSize;changed++;}little.putLong(t+8,newPos);little.putInt(t+16,newSize);oldPos=(oldPos+oldSize+7)&~7L;newPos=(newPos+newSize+7)&~7L;}
                    if(changed!=295||DATA_OFFSET+newPos!=OUT_SIZE||oldPos<BASE_SIZE-DATA_OFFSET||oldPos>BASE_SIZE-DATA_OFFSET+7)throw new IOException("unexpected layout size");out.write(metadata);
                    oldPos=0;newPos=0;byte[] zeros=new byte[65536];
                    for(int i=0;i<OBJECTS;i++){
                        int t=TABLE+i*24;long id=little.getLong(t);Resize r=resized.get(id);int newSize=little.getInt(t+16),oldSize=r==null?newSize:r.oldSize;in.seek(DATA_OFFSET+oldPos);int left=oldSize;
                        while(left>0){int n=Math.min(left,buffer.length);in.readFully(buffer,0,n);if(r!=null){for(int j=0;j<n;j++)if(buffer[j]!=0)throw new IOException("resized texture is not blank");}else out.write(buffer,0,n);left-=n;}
                        if(r!=null){left=newSize;while(left>0){int n=Math.min(left,zeros.length);out.write(zeros,0,n);left-=n;}}
                        int padding=(8-(newSize&7))&7;out.write(zeros,0,padding);oldPos=(oldPos+oldSize+7)&~7L;newPos=(newPos+newSize+7)&~7L;
                    }
                    if(out.length()!=OUT_SIZE||!MessageDigest.isEqual(fileHash(out),outHash))throw new IOException("reconstructed layout differs");out.getFD().sync();
                }
                Files.move(result,dest);moved=true;
            }finally{Files.deleteIfExists(source);if(result!=null&&!moved)Files.deleteIfExists(result);}
        }
    }
    private SteamShared1Layout(){}
}
