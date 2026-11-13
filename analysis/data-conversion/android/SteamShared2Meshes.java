package com.openbonk.dataprep;
import java.io.*;
import java.nio.*;
import java.nio.charset.StandardCharsets;
import java.security.MessageDigest;
import java.util.*;
import java.util.zip.*;
import static com.openbonk.dataprep.SteamExactTextures.*;

/** Rebuilds meshes and optionally materials in a still-incomplete second-map asset. */
public final class SteamShared2Meshes {
    private static final String BASE="ca36181d234fe22d0d565bd2bc199d7fab1be4e880e3276083b43c071f66716d";
    private static boolean compatibleBaseHash(String hash){return hash.equals(BASE)||hash.equals("f0bb1f860804e25687085bca64a4554fc292bd03129eb0bb2303a277da52e003");}
    private static final String PLAN="c68c6d012d869cdb18c66e6459785ea22ebe3aad0868d53dd7d088c070bb26e3";
    private static final String PLAN2="3b9cc7da7ea192105c5d3ae314359502b0fdb18c539316a76a3fd7f336b8bd27";
    private static final String PLAN3="fd12c1106e6d57e4f8d975ea1e1ece9e05a2ebc71d8aa6dbc4cc4ce1537fa216";
    private static final String PLAN4="e7c5414a3d8a28440c7f1438170e92faa62a2cd743db400101e32af882eefae7";
    private static final String PLAN5="cef8c12884ba52eb430a2f4c86ffc8b20843b777a8db358ba56f7a636e4b958e";
    private static final String PLAN6="d16e57e7a0d55a207107e60c3e17143f3cb2a17f332760ad8112278cc95c2e9e";
    private static final String PLAN7="cc51021757ac84bdae29a34af3b3583c6eb0aa39ad2be18529c2a353d591d69b";
    private static final String[] NAMES={"sharedassets2.assets","sharedassets2.assets.resS","level1","level1.resS","level2","level2.resS"};
    private static final long[] LINUX={5824160L,10434976L,2101944L,116112L,10417064L,74576L},WINDOWS={5503192L,10434976L,2101944L,116112L,10417064L,74576L};
    public static byte[] restorePartial(File baseFile,File steamFile,File planFile)throws IOException{
        if(!baseFile.isFile()||!compatibleBaseHash(hex(hash(file(baseFile,16*1024*1024)))))throw new IOException("incompatible base");
        byte[] planBytes=file(planFile,1024*1024);String planHash=hex(hash(planBytes));boolean generated=planHash.equals(PLAN7),streamed=generated||planHash.equals(PLAN6),derived=streamed||planHash.equals(PLAN5),metadata=derived||planHash.equals(PLAN4),audio=metadata||planHash.equals(PLAN3),material=planHash.equals(PLAN2);if(!audio&&!material&&!planHash.equals(PLAN))throw new IOException("incompatible mesh plan");
        if(!steamFile.isFile()||steamFile.length()<=0||steamFile.length()>1024L*1024*1024)throw new IOException("invalid Steam ZIP");
        try(ZipFile base=new ZipFile(baseFile);ZipFile steam=new ZipFile(steamFile);DataInputStream plan=new DataInputStream(new ByteArrayInputStream(planBytes))){
            int count=generated?1949:streamed?1792:derived?1625:metadata?821:audio?728:material?604:502;if(!Arrays.equals(bytes(plan,8),new byte[]{'O','B','S','M','2','0','0',(byte)(generated?'7':streamed?'6':derived?'5':metadata?'4':audio?'3':material?'2':'1')})||plan.readInt()!=count)throw new IOException("invalid mesh plan");
            byte[][] lh=new byte[6][],wh=new byte[6][];for(int i=0;i<6;i++)lh[i]=bytes(plan,32);for(int i=0;i<6;i++)wh[i]=bytes(plan,32);
            int size=plan.readInt();byte[] baseHash=bytes(plan,32),resultHash=bytes(plan,32),finalHash=bytes(plan,32);
            if(size!=22278296||MessageDigest.isEqual(resultHash,finalHash))throw new IOException("unexpected partial asset");
            List<Texture> ranges=new ArrayList<>();long previous=0,total=0;
            for(int i=0;i<count;i++){
                Texture t=new Texture();t.target=plan.readInt();t.size=plan.readInt();t.linuxFile=plan.readInt();t.linuxOffset=plan.readLong();t.windowsFile=plan.readInt();t.windowsOffset=plan.readLong();t.payloadHash=bytes(plan,32);
                if(t.target<previous||t.size<=0||t.size>8*1024*1024||t.target>size-t.size||t.linuxFile<0||t.linuxFile>=6||t.windowsFile<0||t.windowsFile>=6||t.linuxOffset<0||t.windowsOffset<0||t.linuxOffset>LINUX[t.linuxFile]-t.size||t.windowsOffset>WINDOWS[t.windowsFile]-t.size)throw new IOException("invalid mesh range");
                previous=(long)t.target+t.size;total+=t.size;if(total>16*1024*1024)throw new IOException("mesh data limit exceeded");t.data=new byte[t.size];ranges.add(t);
            }
            List<long[]> audioRows=new ArrayList<>();if(audio){if(plan.readInt()!=124)throw new IOException("invalid audio metadata count");for(int i=0;i<124;i++){int offset=plan.readInt(),length=plan.readInt();long oldOffset=plan.readLong(),oldSize=plan.readLong(),newOffset=plan.readLong(),newSize=plan.readLong();if(offset<0||length<80||offset>size-length||oldOffset<0||oldSize<=0||newOffset<0||newSize<=0||newOffset>5365152-newSize)throw new IOException("invalid audio metadata");audioRows.add(new long[]{offset,length,oldOffset,oldSize,newOffset,newSize});}}
            List<Integer> flags=new ArrayList<>();if(derived){int n=plan.readInt();if(n<0||n>502)throw new IOException("invalid mesh flag count");Set<Integer> unique=new HashSet<>();for(int i=0;i<n;i++){int where=plan.readInt();if(where<0||where>=size||!unique.add(where))throw new IOException("invalid mesh flag");flags.add(where);}}
            List<int[]> vertexSizes=new ArrayList<>();if(streamed){int n=plan.readInt();if(n!=24)throw new IOException("invalid streamed mesh count");Set<Integer> unique=new HashSet<>();for(int i=0;i<n;i++){int where=plan.readInt(),length=plan.readInt();if(where<0||where>size-4||length<=0||length>8*1024*1024||!unique.add(where))throw new IOException("invalid streamed mesh size");vertexSizes.add(new int[]{where,length});}}
            List<int[]> generatedNames=new ArrayList<>();if(generated){int n=plan.readInt();if(n!=42)throw new IOException("invalid generated mesh name count");Set<Integer> unique=new HashSet<>();for(int i=0;i<n;i++){int where=plan.readInt(),id=plan.readInt();if(where<0||where>size-32||!unique.add(where))throw new IOException("invalid generated mesh name");generatedNames.add(new int[]{where,id});}}
            if(plan.read()!=-1)throw new IOException("trailing mesh plan data");ZipEntry[] source=new ZipEntry[6];String common=null;Set<String> names=new HashSet<>();Enumeration<? extends ZipEntry> entries=steam.entries();
            while(entries.hasMoreElements()){
                ZipEntry e=entries.nextElement();String n=e.getName();if(names.size()>=20000||n.startsWith("/")||n.indexOf('\\')>=0||n.indexOf(':')>=0||!names.add(n))throw new IOException("unsafe ZIP entry");
                for(String part:n.split("/"))if(part.isEmpty()||part.equals(".")||part.equals(".."))throw new IOException("unsafe ZIP path");
                for(int i=0;i<6;i++){String root=rootFor(n,NAMES[i]);if(root!=null){if(source[i]!=null||common!=null&&!common.equals(root))throw new IOException("conflicting roots");common=root;source[i]=e;}}
            }
            if(source[0]==null)throw new IOException("missing Steam asset");boolean windows;if(source[0].getSize()==WINDOWS[0])windows=true;else if(source[0].getSize()==LINUX[0])windows=false;else throw new IOException("unsupported Steam asset");
            for(int i=0;i<6;i++)collect(steam,source[i],(windows?WINDOWS:LINUX)[i],(windows?wh:lh)[i],i,windows,ranges);
            byte[] asset=new byte[size];int position=0;
            for(int i=0;i<22;i++){byte[] part=entry(base,base.getEntry("assets/bin/Data/sharedassets2.assets.split"+i),1048576);if(part.length==0||part.length>size-position)throw new IOException("invalid asset split");System.arraycopy(part,0,asset,position,part.length);position+=part.length;}
            if(position!=size||!MessageDigest.isEqual(hash(asset),baseHash))throw new IOException("base asset differs");
            for(Texture t:ranges){if(t.filled!=t.size||!MessageDigest.isEqual(hash(t.data),t.payloadHash))throw new IOException("mesh payload differs");for(int i=t.target;i<t.target+t.size;i++)if(asset[i]!=0)throw new IOException("mesh target not blank");System.arraycopy(t.data,0,asset,t.target,t.size);}
            ByteBuffer buffer=ByteBuffer.wrap(asset).order(ByteOrder.LITTLE_ENDIAN);for(long[] r:audioRows){int offset=(int)r[0],length=(int)r[1];if(asset[offset+length-50]!=1||buffer.getLong(offset+length-20)!=r[2]||buffer.getLong(offset+length-12)!=r[3])throw new IOException("audio metadata differs");asset[offset+length-50]=0;buffer.putLong(offset+length-20,r[4]);buffer.putLong(offset+length-12,r[5]);}
            for(int where:flags){if(asset[where]!=0)throw new IOException("mesh flag not blank");asset[where]=1;}
            for(int[] r:vertexSizes){if(buffer.getInt(r[0])!=0)throw new IOException("mesh vertex length not blank");buffer.putInt(r[0],r[1]);}
            for(int[] r:generatedNames){byte[] name=("pb_Mesh"+r[1]).getBytes(StandardCharsets.US_ASCII);int padded=(name.length+3)&~3;if(name.length>24)throw new IOException("mesh name too long");for(int i=0;i<4+padded;i++)if(asset[r[0]+i]!=0)throw new IOException("mesh name not blank");buffer.putInt(r[0],name.length);System.arraycopy(name,0,asset,r[0]+4,name.length);}
            if(!MessageDigest.isEqual(hash(asset),resultHash))throw new IOException("partial mesh asset differs");return asset;
        }
    }
    private SteamShared2Meshes(){}
}
