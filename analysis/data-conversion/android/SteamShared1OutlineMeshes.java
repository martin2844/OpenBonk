package com.openbonk.dataprep;
import java.io.*;
import java.nio.*;
import java.nio.file.*;
import java.security.*;
import java.util.*;
import java.util.zip.*;
import static com.openbonk.dataprep.SteamExactTextures.*;

/** Rebuilds two outline meshes by adding smoothed normals to Steam vertices. */
public final class SteamShared1OutlineMeshes {
    private static final String PLAN="4b6814d25440663f1d57700806db6781df8061d2f97aaacb1dc66ead4838cb08";private static final int LENGTH=122379840;
    private static final class Edit{int offset,value;}
    private static final class Row{int dst,size,sourceSize,vertex,count,filled;long[] sourceOffset=new long[2];byte[][] sourceHash=new byte[2][];byte[] targetHash,data;List<Edit>[] edits;}
    private static final class Key{final int x,y,z;Key(float a,float b,float c){x=Float.floatToIntBits(a==0?0:a);y=Float.floatToIntBits(b==0?0:b);z=Float.floatToIntBits(c==0?0:c);}public boolean equals(Object other){if(!(other instanceof Key))return false;Key k=(Key)other;return x==k.x&&y==k.y&&z==k.z;}public int hashCode(){return (x*31+y)*31+z;}}
    private static final class Norm{int count;float x,y,z;}
    private static MessageDigest digest(){try{return MessageDigest.getInstance("SHA-256");}catch(NoSuchAlgorithmException e){throw new AssertionError(e);}}
    private static byte[] fileHash(RandomAccessFile f)throws IOException{MessageDigest h=digest();byte[] b=new byte[65536];f.seek(0);for(int n;(n=f.read(b))!=-1;)if(n>0)h.update(b,0,n);return h.digest();}
    private static byte[] vertices(byte[] src,int offset,int count)throws IOException{
        ByteBuffer input=ByteBuffer.wrap(src).order(ByteOrder.LITTLE_ENDIAN);Map<Key,Norm> groups=new HashMap<>();Key[] keys=new Key[count];
        for(int i=0;i<count;i++){int p=offset+i*32;float x=input.getFloat(p),y=input.getFloat(p+4),z=input.getFloat(p+8),nx=input.getFloat(p+12),ny=input.getFloat(p+16),nz=input.getFloat(p+20);if(!Float.isFinite(x)||!Float.isFinite(y)||!Float.isFinite(z)||!Float.isFinite(nx)||!Float.isFinite(ny)||!Float.isFinite(nz))throw new IOException("nonfinite mesh vertex");Key key=new Key(x,y,z);keys[i]=key;Norm n=groups.get(key);if(n==null){n=new Norm();groups.put(key,n);}n.count++;n.x+=nx;n.y+=ny;n.z+=nz;}
        byte[] data=new byte[count*44];ByteBuffer out=ByteBuffer.wrap(data).order(ByteOrder.LITTLE_ENDIAN);
        for(int i=0;i<count;i++){int p=offset+i*32,q=i*44;System.arraycopy(src,p,data,q,32);Norm n=groups.get(keys[i]);float x,y,z;if(n.count==1){x=input.getFloat(p+12);y=input.getFloat(p+16);z=input.getFloat(p+20);}else{float len=(float)Math.sqrt((n.x*n.x+n.y*n.y)+n.z*n.z);if(len>1e-5f){x=n.x/len;y=n.y/len;z=n.z/len;}else{x=0;y=0;z=0;}}out.putFloat(q+32,x);out.putFloat(q+36,y);out.putFloat(q+40,z);}
        return data;
    }
    @SuppressWarnings("unchecked") public static void restore(File baseFile,File steamFile,File planFile,File output)throws IOException{
        byte[] planData=file(planFile,8192);if(!hex(hash(planData)).equals(PLAN))throw new IOException("incompatible outline plan");Path dest=output.toPath().toAbsolutePath(),parent=dest.getParent();if(parent==null||!Files.isDirectory(parent)||Files.exists(dest,LinkOption.NOFOLLOW_LINKS))throw new IOException("output must be new");
        try(DataInputStream p=new DataInputStream(new ByteArrayInputStream(planData))){
            if(!Arrays.equals(bytes(p,8),new byte[]{'O','B','S','1','O','0','0','1'})||p.readInt()!=LENGTH||p.readInt()!=2)throw new IOException("invalid outline plan");byte[] baseHash=bytes(p,32),resultHash=bytes(p,32);long[] sizes=new long[2];byte[][] hashes=new byte[2][];for(int k=0;k<2;k++){sizes[k]=p.readLong();hashes[k]=bytes(p,32);}
            List<Row> rows=new ArrayList<>();long previous=0;
            for(int i=0;i<2;i++){
                Row r=new Row();r.dst=p.readInt();r.size=p.readInt();r.sourceSize=p.readInt();r.vertex=p.readInt();r.count=p.readInt();r.sourceOffset[0]=p.readLong();r.sourceOffset[1]=p.readLong();r.sourceHash[0]=bytes(p,32);r.sourceHash[1]=bytes(p,32);r.targetHash=bytes(p,32);
                if(r.dst<previous||r.size<=0||r.size>2*1024*1024||r.dst>LENGTH-r.size||r.count<=0||r.count>10000||r.vertex<0||r.sourceSize<=0||r.vertex>r.sourceSize-r.count*32||r.size!=r.sourceSize+r.count*12||r.sourceOffset[0]<0||r.sourceOffset[1]<0||r.sourceOffset[0]>sizes[0]-r.sourceSize||r.sourceOffset[1]>sizes[1]-r.sourceSize)throw new IOException("invalid outline range");previous=(long)r.dst+r.size;r.edits=(List<Edit>[])new List<?>[2];
                for(int platform=0;platform<2;platform++){int n=p.readInt(),prior=-1;if(n<0||n>8)throw new IOException("invalid outline edits");r.edits[platform]=new ArrayList<>();for(int j=0;j<n;j++){Edit e=new Edit();e.offset=p.readInt();e.value=p.readUnsignedByte();if(e.offset<=prior||e.offset>=r.size||e.offset>=r.vertex&&e.offset<r.vertex+r.count*44)throw new IOException("invalid outline edit");prior=e.offset;r.edits[platform].add(e);}}r.data=new byte[r.sourceSize];rows.add(r);
            }
            if(p.read()!=-1)throw new IOException("trailing outline plan");if(Files.isSymbolicLink(baseFile.toPath())||!Files.isRegularFile(baseFile.toPath(),LinkOption.NOFOLLOW_LINKS)||baseFile.length()!=LENGTH)throw new IOException("invalid base asset");Path temp=Files.createTempFile(parent,".openbonk-shared1-outline-",".tmp");boolean moved=false;
            try(RandomAccessFile work=new RandomAccessFile(temp.toFile(),"rw");InputStream base=Files.newInputStream(baseFile.toPath());ZipFile steam=new ZipFile(steamFile)){
                byte[] buffer=new byte[65536];MessageDigest bh=digest();long copied=0;for(int n;(n=base.read(buffer))!=-1;){if(n==0)continue;if(n>LENGTH-copied)throw new IOException("base too large");work.write(buffer,0,n);bh.update(buffer,0,n);copied+=n;}if(copied!=LENGTH||!MessageDigest.isEqual(bh.digest(),baseHash))throw new IOException("base asset differs");
                Set<String> names=new HashSet<>();ZipEntry source=null;Enumeration<? extends ZipEntry> all=steam.entries();while(all.hasMoreElements()){ZipEntry e=all.nextElement();String n=e.getName();if(names.size()>=20000||n.startsWith("/")||n.indexOf('\\')>=0||n.indexOf(':')>=0||!names.add(n))throw new IOException("unsafe Steam ZIP");for(String part:n.split("/"))if(part.isEmpty()||part.equals(".")||part.equals(".."))throw new IOException("unsafe Steam path");if(rootFor(n,"sharedassets1.assets")!=null){if(source!=null)throw new IOException("duplicate Steam asset");source=e;}}
                if(source==null||source.isDirectory())throw new IOException("missing Steam asset");int platform;if(source.getSize()==sizes[0])platform=0;else if(source.getSize()==sizes[1])platform=1;else throw new IOException("unsupported Steam asset");MessageDigest sh=digest();long pos=0;
                try(InputStream in=steam.getInputStream(source)){for(int n;(n=in.read(buffer))!=-1;){if(n==0)continue;if(n>sizes[platform]-pos)throw new IOException("Steam asset too large");sh.update(buffer,0,n);for(Row r:rows){long start=r.sourceOffset[platform],from=Math.max(pos,start),to=Math.min(pos+n,start+r.sourceSize);if(from<to){int len=(int)(to-from);System.arraycopy(buffer,(int)(from-pos),r.data,(int)(from-start),len);r.filled+=len;}}pos+=n;}}
                if(pos!=sizes[platform]||!MessageDigest.isEqual(sh.digest(),hashes[platform]))throw new IOException("Steam asset differs");
                for(Row r:rows){if(r.filled!=r.sourceSize||!MessageDigest.isEqual(hash(r.data),r.sourceHash[platform]))throw new IOException("outline source differs");byte[] vertex=vertices(r.data,r.vertex,r.count),result=new byte[r.size];System.arraycopy(r.data,0,result,0,r.vertex);System.arraycopy(vertex,0,result,r.vertex,vertex.length);int tail=r.sourceSize-r.vertex-r.count*32;System.arraycopy(r.data,r.vertex+r.count*32,result,r.vertex+vertex.length,tail);
                    for(Edit e:r.edits[platform]){if((result[e.offset]&255)==e.value)throw new IOException("redundant outline edit");result[e.offset]=(byte)e.value;}if(!MessageDigest.isEqual(hash(result),r.targetHash))throw new IOException("outline mesh differs");work.seek(r.dst);int left=r.size;while(left>0){int n=Math.min(left,buffer.length);work.readFully(buffer,0,n);for(int j=0;j<n;j++)if(buffer[j]!=0)throw new IOException("outline target not blank");left-=n;}work.seek(r.dst);work.write(result);}
                if(work.length()!=LENGTH||!MessageDigest.isEqual(fileHash(work),resultHash))throw new IOException("partial shared1 asset differs");work.getFD().sync();Files.move(temp,dest);moved=true;
            }finally{if(!moved)Files.deleteIfExists(temp);}
        }
    }
    private SteamShared1OutlineMeshes(){}
}
