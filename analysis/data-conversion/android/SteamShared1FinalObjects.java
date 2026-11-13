package com.openbonk.dataprep;
import java.io.*;
import java.nio.*;
import java.nio.charset.StandardCharsets;
import java.nio.file.*;
import java.security.*;
import java.util.*;
import java.util.zip.*;
import static com.openbonk.dataprep.SteamExactTextures.*;

/** Restores the remaining mesh and video object using private Android edits. */
public final class SteamShared1FinalObjects {
    private static final String PLAN="ff164a12cce569fa3f54ad427e7068ce6257fa863cefbbbee9ee8a05776fec9b";private static final int LENGTH=122379840;private static final String[] NAMES={"sharedassets1.assets","level1"};
    private static final class Edit{int pos,value;}
    private static final class Row{int dst,size,sourceSize,file,kind;Texture source;byte[][] sourceHash;byte[] targetHash,priorHash;List<Edit>[] edits;}
    private static MessageDigest digest(){try{return MessageDigest.getInstance("SHA-256");}catch(NoSuchAlgorithmException e){throw new AssertionError(e);}}
    private static byte[] fileHash(RandomAccessFile f)throws IOException{MessageDigest h=digest();byte[] b=new byte[65536];f.seek(0);for(int n;(n=f.read(b))!=-1;)if(n>0)h.update(b,0,n);return h.digest();}
    private static byte[] videoPath(byte[] raw,int expected)throws IOException{
        ByteBuffer b=ByteBuffer.wrap(raw).order(ByteOrder.LITTLE_ENDIAN);int n=b.getInt(0);if(n<0||n>256)throw new IOException("invalid video name");int at=4+((n+3)&~3);if(at>raw.length-4)throw new IOException("missing video path");int len=b.getInt(at),start=at+4;if(len<=0||len>512||start+len>raw.length)throw new IOException("invalid video path");int slash=-1;for(int i=0;i<len;i++)if(raw[start+i]=='/')slash=i;if(slash<0||slash==len-1)throw new IOException("invalid video filename");byte[] prefix="Assets/VideoClip/".getBytes(StandardCharsets.US_ASCII);int nameLen=len-slash-1,newLen=prefix.length+nameLen,oldEnd=start+((len+3)&~3),newEnd=start+((newLen+3)&~3);if(oldEnd>raw.length||newEnd+raw.length-oldEnd!=expected)throw new IOException("video path size differs");byte[] out=new byte[expected];System.arraycopy(raw,0,out,0,at);ByteBuffer.wrap(out).order(ByteOrder.LITTLE_ENDIAN).putInt(at,newLen);System.arraycopy(prefix,0,out,start,prefix.length);System.arraycopy(raw,start+slash+1,out,start+prefix.length,nameLen);System.arraycopy(raw,oldEnd,out,newEnd,raw.length-oldEnd);return out;
    }
    @SuppressWarnings("unchecked") public static void restore(File baseFile,File steamFile,File planFile,File output)throws IOException{
        byte[] planData=file(planFile,8192);if(!hex(hash(planData)).equals(PLAN))throw new IOException("incompatible final object plan");Path dest=output.toPath().toAbsolutePath(),parent=dest.getParent();if(parent==null||!Files.isDirectory(parent)||Files.exists(dest,LinkOption.NOFOLLOW_LINKS))throw new IOException("output must be new");
        try(DataInputStream p=new DataInputStream(new ByteArrayInputStream(planData))){
            if(!Arrays.equals(bytes(p,8),new byte[]{'O','B','S','1','F','0','0','1'})||p.readInt()!=LENGTH||p.readInt()!=2)throw new IOException("invalid final object plan");byte[] baseHash=bytes(p,32),resultHash=bytes(p,32);long[][] sizes=new long[2][2];byte[][][] hashes=new byte[2][2][];for(int k=0;k<2;k++)for(int i=0;i<2;i++){sizes[k][i]=p.readLong();hashes[k][i]=bytes(p,32);}
            List<Row> rows=new ArrayList<>();List<Texture> ranges=new ArrayList<>();long previous=0;
            for(int i=0;i<2;i++){Row r=new Row();r.dst=p.readInt();r.size=p.readInt();r.sourceSize=p.readInt();r.file=p.readInt();r.kind=p.readInt();Texture x=new Texture();r.source=x;x.linuxOffset=p.readLong();x.windowsOffset=p.readLong();r.sourceHash=new byte[][]{bytes(p,32),bytes(p,32)};r.targetHash=bytes(p,32);r.priorHash=bytes(p,32);x.size=r.sourceSize;x.linuxFile=x.windowsFile=r.file;
                if(r.dst<previous||r.size<=0||r.size>8192||r.dst>LENGTH-r.size||r.sourceSize<=0||r.sourceSize>8192||r.file<0||r.file>1||r.kind<0||r.kind>1||x.linuxOffset<0||x.windowsOffset<0||x.linuxOffset>sizes[0][r.file]-x.size||x.windowsOffset>sizes[1][r.file]-x.size)throw new IOException("invalid final object range");previous=(long)r.dst+r.size;r.edits=(List<Edit>[])new List<?>[2];
                for(int k=0;k<2;k++){int n=p.readInt(),prior=-1;if(n<0||n>16)throw new IOException("invalid final object edits");r.edits[k]=new ArrayList<>();for(int j=0;j<n;j++){Edit e=new Edit();e.pos=p.readInt();e.value=p.readUnsignedByte();if(e.pos<=prior||e.pos>=r.size)throw new IOException("invalid final object edit");prior=e.pos;r.edits[k].add(e);}}x.data=new byte[x.size];ranges.add(x);rows.add(r);}
            if(p.read()!=-1)throw new IOException("trailing final object plan");if(Files.isSymbolicLink(baseFile.toPath())||!Files.isRegularFile(baseFile.toPath(),LinkOption.NOFOLLOW_LINKS)||baseFile.length()!=LENGTH)throw new IOException("invalid base asset");Path temp=Files.createTempFile(parent,".openbonk-shared1-final-",".tmp");boolean moved=false;
            try(RandomAccessFile work=new RandomAccessFile(temp.toFile(),"rw");InputStream base=Files.newInputStream(baseFile.toPath());ZipFile steam=new ZipFile(steamFile)){
                byte[] buffer=new byte[65536];MessageDigest bh=digest();long count=0;for(int n;(n=base.read(buffer))!=-1;){if(n==0)continue;if(n>LENGTH-count)throw new IOException("base too large");work.write(buffer,0,n);bh.update(buffer,0,n);count+=n;}if(count!=LENGTH||!MessageDigest.isEqual(bh.digest(),baseHash))throw new IOException("base asset differs");
                ZipEntry[] entries=new ZipEntry[2];String root=null;Set<String> seen=new HashSet<>();Enumeration<? extends ZipEntry> all=steam.entries();while(all.hasMoreElements()){ZipEntry e=all.nextElement();String n=e.getName();if(seen.size()>=20000||n.startsWith("/")||n.indexOf('\\')>=0||n.indexOf(':')>=0||!seen.add(n))throw new IOException("unsafe Steam ZIP");for(String part:n.split("/"))if(part.isEmpty()||part.equals(".")||part.equals(".."))throw new IOException("unsafe Steam path");for(int i=0;i<2;i++){String r=rootFor(n,NAMES[i]);if(r!=null){if(entries[i]!=null||root!=null&&!root.equals(r))throw new IOException("conflicting Steam roots");entries[i]=e;root=r;}}}
                if(entries[0]==null||entries[1]==null)throw new IOException("missing Steam asset");boolean windows;if(entries[0].getSize()==sizes[0][0])windows=false;else if(entries[0].getSize()==sizes[1][0])windows=true;else throw new IOException("unsupported Steam asset");int platform=windows?1:0;for(int i=0;i<2;i++)collect(steam,entries[i],sizes[platform][i],hashes[platform][i],i,windows,ranges);
                for(Row r:rows){Texture x=r.source;if(x.filled!=x.size||!MessageDigest.isEqual(hash(x.data),r.sourceHash[platform]))throw new IOException("final source differs");byte[] value=r.kind==1?videoPath(x.data,r.size):x.data.clone();if(value.length!=r.size)throw new IOException("final object size differs");for(Edit e:r.edits[platform]){if((value[e.pos]&255)==e.value)throw new IOException("redundant final edit");value[e.pos]=(byte)e.value;}if(!MessageDigest.isEqual(hash(value),r.targetHash))throw new IOException("final object differs");byte[] old=new byte[r.size];work.seek(r.dst);work.readFully(old);if(!MessageDigest.isEqual(hash(old),r.priorHash))throw new IOException("retained object differs");work.seek(r.dst);work.write(value);}
                if(work.length()!=LENGTH||!MessageDigest.isEqual(fileHash(work),resultHash))throw new IOException("complete shared1 asset differs");work.getFD().sync();Files.move(temp,dest);moved=true;
            }finally{if(!moved)Files.deleteIfExists(temp);}
        }
    }
    private SteamShared1FinalObjects(){}
}
