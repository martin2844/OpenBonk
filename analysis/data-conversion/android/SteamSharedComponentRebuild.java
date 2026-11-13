package com.openbonk.dataprep;

import java.io.*;
import java.nio.charset.StandardCharsets;
import java.nio.file.*;
import java.util.*;
import java.util.zip.*;
import static com.openbonk.dataprep.SteamExactTextures.*;

/** Reconstructs pinned, three-way matching scene positions from Steam data. */
public final class SteamSharedComponentRebuild {
    public static final String SCRUBBED="d1cd0e69e2690cb7e289962c5646a4aa3ef3b9b1a60e1838c4e4edf8bbb83431";
    private static final String PLAN="16ba7e7f6192831f5ba201b5347896f16f14f9dc31352107e8fd5a43132ccf0b";
    private static final String[] NAMES={"sharedassets1.assets","sharedassets2.assets"};
    private static final int[][] OFFSETS={{1691102,1745438,1784161},{2390280,2448395,2484971,2519385}};
    private static final int[][] PACKED={{54225,38612,140049},{58005,35084,34304,24745}};
    private static final int[][] SIZES={{1048576,1048576,1048576},{1048576,1048576,1048576,258200}};

    private static byte[] inflate(byte[] input,int offset,int length,int size)throws IOException {
        Inflater z=new Inflater(true);
        try {
            z.setInput(input,offset,length);byte[] out=new byte[size];int pos=0;
            while(!z.finished()&&pos<size){int n=z.inflate(out,pos,size-pos);if(n==0)throw new IOException("cannot inflate scene");pos+=n;}
            if(pos!=size||!z.finished())throw new IOException("scene length differs");return out;
        }catch(DataFormatException e){throw new IOException("invalid scene template",e);}finally{z.end();}
    }
    private static byte[] source(ZipFile zip,String name)throws IOException {
        ZipEntry selected=null;Set<String> seen=new HashSet<>();Enumeration<? extends ZipEntry> all=zip.entries();
        while(all.hasMoreElements()){
            ZipEntry e=all.nextElement();String n=e.getName();
            if(seen.size()>=20000||n.startsWith("/")||n.indexOf('\\')>=0||n.indexOf(':')>=0||!seen.add(n))throw new IOException("unsafe Steam ZIP");
            for(String part:n.split("/"))if(part.isEmpty()||part.equals(".")||part.equals(".."))throw new IOException("unsafe Steam path");
            if(rootFor(n,name)!=null){if(selected!=null)throw new IOException("duplicate scene");selected=e;}
        }
        return entry(zip,selected,64*1024*1024);
    }
    private static void expect(DataInputStream in,byte[] expected)throws IOException {
        byte[] actual=new byte[expected.length];in.readFully(actual);
        if(!Arrays.equals(actual,expected))throw new IOException("scene plan header differs");
    }
    public static void restore(File template,File steam,File planFile,File output)throws IOException {
        Path dest=output.toPath().toAbsolutePath(),parent=dest.getParent();
        if(parent==null||!Files.isDirectory(parent)||Files.exists(dest,LinkOption.NOFOLLOW_LINKS))throw new IOException("output must be new");
        byte[] base=file(template,4*1024*1024),plan=file(planFile,2*1024*1024);
        if(!hex(hash(base)).equals(SCRUBBED)||!hex(hash(plan)).equals(PLAN))throw new IOException("scene inputs differ");
        try(DataInputStream in=new DataInputStream(new ByteArrayInputStream(plan));ZipFile zip=new ZipFile(steam)){
            expect(in,"OBSCN001".getBytes(StandardCharsets.US_ASCII));
            if(!hex(read32(in)).equals(SteamSceneComponentRebuild.SCRUBBED)||!hex(read32(in)).equals(SCRUBBED))throw new IOException("scene plan hashes differ");
            int rows=0,changed=0;
            for(int s=0;s<NAMES.length;s++){
                int total=0;for(int n:SIZES[s])total+=n;byte[] content=new byte[total];int cursor=0;
                for(int i=0;i<SIZES[s].length;i++){byte[] part=inflate(base,OFFSETS[s][i],PACKED[s][i],SIZES[s][i]);System.arraycopy(part,0,content,cursor,part.length);cursor+=part.length;}
                int count=in.readInt();byte[] linuxHash=read32(in),windowsHash=read32(in),original=source(zip,NAMES[s]),sourceHash=hash(original);
                boolean linux=Arrays.equals(sourceHash,linuxHash),windows=Arrays.equals(sourceHash,windowsHash);
                if(count<0||count>20000||(!linux&&!windows))throw new IOException("scene source differs");
                for(int r=0;r<count;r++){
                    int pos=in.readInt()-(s==0?104:18)*1048576,lo=in.readInt(),wo=in.readInt(),size=in.readInt(),off=linux?lo:wo;
                    if(size<=0||size>1048576||pos<0||pos>content.length-size||off<0||off>original.length-size)throw new IOException("invalid scene range");
                    byte[] bits=new byte[(size+7)/8];in.readFully(bits);
                    for(int i=0;i<size;i++)if((bits[i/8]&(1<<(i%8)))!=0){
                        if(content[pos+i]!=0||original[off+i]==0)throw new IOException("invalid scene byte");
                        content[pos+i]=original[off+i];changed++;
                    }
                    rows++;
                }
                cursor=0;
                for(int i=0;i<SIZES[s].length;i++){
                    Deflater z=new SteamStableDeflater(6);ByteArrayOutputStream packed=new ByteArrayOutputStream();
                    try{z.setInput(content,cursor,SIZES[s][i]);z.finish();byte[] b=new byte[4096];while(!z.finished()){
                        int n=z.deflate(b);if(n==0)throw new IOException("cannot compress scene");packed.write(b,0,n);
                        if(packed.size()>PACKED[s][i])throw new IOException("scene stream differs");
                    }}finally{z.end();}
                    byte[] value=packed.toByteArray();Arrays.fill(base,OFFSETS[s][i],OFFSETS[s][i]+PACKED[s][i],(byte)0);
                    System.arraycopy(value,0,base,OFFSETS[s][i],value.length);cursor+=SIZES[s][i];
                }
            }
            if(in.read()!=-1||rows!=5887||changed!=309464||!hex(hash(base)).equals(SteamSceneComponentRebuild.SCRUBBED))throw new IOException("restored scene base differs");
        }
        Path temp=Files.createTempFile(parent,".openbonk-scene-base-",".tmp");boolean moved=false;
        try{Files.write(temp,base);Files.move(temp,dest);moved=true;}finally{if(!moved)Files.deleteIfExists(temp);}
    }
    private static byte[] read32(DataInputStream in)throws IOException{byte[] b=new byte[32];in.readFully(b);return b;}
    private SteamSharedComponentRebuild(){}
}
