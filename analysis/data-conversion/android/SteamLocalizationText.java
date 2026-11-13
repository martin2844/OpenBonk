package com.openbonk.dataprep;
import java.io.*;
import java.nio.charset.StandardCharsets;
import java.nio.file.*;
import java.util.*;
import java.util.zip.*;
import static com.openbonk.dataprep.SteamExactTextures.*;

/** Private diagnostic reconstruction of Steam-derived locale text bundles. */
public final class SteamLocalizationText {
    private static final String PLAN="99a68d678b9d6a0489e652310c0e6269ccf4ba45148de1ab9aa34e03a54ac715";
    private static final String COMPLETE_PLAN="7370ba9be776c156d5a12ae235bf7be8c11f23fc5ba3fc988a928acb807a6888",SHARED="localization-assets-shared_assets_all.bundle";
    private static byte[] inflate(byte[] data,int expected)throws IOException{
        Inflater z=new Inflater();z.setInput(data);byte[] out=new byte[expected];int done=0;
        try{while(!z.finished()&&done<out.length){int n=z.inflate(out,done,out.length-done);if(n==0)break;done+=n;}if(!z.finished()||done!=expected||z.getRemaining()!=0)throw new IOException("invalid blank locale");return out;}catch(DataFormatException e){throw new IOException("invalid blank locale",e);}finally{z.end();}
    }
    static byte[] lz4(byte[] source,int start,int expected)throws IOException{
        byte[] result=new byte[expected];int in=start,out=0;
        while(in<source.length){
            int token=source[in++]&255,lit=token>>>4;
            if(lit==15){int x;do{if(in>=source.length)throw new IOException("truncated LZ4 literal");x=source[in++]&255;if(lit>expected-x)throw new IOException("LZ4 literal overflow");lit+=x;}while(x==255);}
            if(lit>source.length-in||lit>expected-out)throw new IOException("invalid LZ4 literals");System.arraycopy(source,in,result,out,lit);in+=lit;out+=lit;
            if(in==source.length)break;
            if(source.length-in<2)throw new IOException("truncated LZ4 offset");int back=(source[in]&255)|((source[in+1]&255)<<8);in+=2;if(back==0||back>out)throw new IOException("invalid LZ4 offset");
            int len=(token&15)+4;if((token&15)==15){int x;do{if(in>=source.length)throw new IOException("truncated LZ4 match");x=source[in++]&255;if(len>expected-x)throw new IOException("LZ4 match overflow");len+=x;}while(x==255);}
            if(len>expected-out)throw new IOException("invalid LZ4 match");for(int i=0;i<len;i++)result[out+i]=result[out-back+i];out+=len;
        }
        if(out!=expected||in!=source.length)throw new IOException("invalid LZ4 output");return result;
    }
    private static int number(DataInputStream p,int max)throws IOException{int n=p.readInt();if(n<0||n>max)throw new IOException("invalid localization plan");return n;}
    public static void restore(File steamFile,File planFile,File output)throws IOException{
        byte[] rawPlan=file(planFile,8*1024*1024);String planHash=hex(hash(rawPlan));int expected=planHash.equals(PLAN)?17:planHash.equals(COMPLETE_PLAN)?18:0;if(expected==0)throw new IOException("incompatible localization plan");
        Path dest=output.toPath().toAbsolutePath(),parent=dest.getParent();if(parent==null||!Files.isDirectory(parent)||Files.exists(dest,LinkOption.NOFOLLOW_LINKS))throw new IOException("output must be new");
        Path temp=Files.createTempDirectory(parent,".openbonk-locale-");boolean moved=false;
        try(ZipFile zip=new ZipFile(steamFile);DataInputStream plan=new DataInputStream(new ByteArrayInputStream(rawPlan))){
            if(!Arrays.equals(bytes(plan,8),new byte[]{'O','B','L','O','C','0','0','1'})||plan.readInt()!=expected)throw new IOException("invalid localization plan");
            Map<String,ZipEntry> entries=new HashMap<>();Enumeration<? extends ZipEntry> enumeration=zip.entries();Set<String> all=new HashSet<>();String root=null;int platform=-1;
            while(enumeration.hasMoreElements()){
                ZipEntry e=enumeration.nextElement();String n=e.getName();if(all.size()>=20000||n.startsWith("/")||n.indexOf('\\')>=0||n.indexOf(':')>=0||!all.add(n))throw new IOException("unsafe Steam ZIP");
                for(String part:n.split("/"))if(part.isEmpty()||part.equals(".")||part.equals(".."))throw new IOException("unsafe Steam path");
                String[] suffixes={"StreamingAssets/aa/StandaloneLinux64/","StreamingAssets/aa/StandaloneWindows64/"};
                for(int k=0;k<2;k++){int pos=n.indexOf(suffixes[k]);if(pos<0)continue;String tail=n.substring(pos+suffixes[k].length());if((!tail.startsWith("localization-string-tables-")&&!tail.equals(SHARED))||tail.indexOf('/')>=0)continue;String prefix=rootFor(n,suffixes[k]+tail);if(prefix==null)continue;if(root!=null&&!root.equals(prefix)||platform!=-1&&platform!=k||entries.put(tail,e)!=null)throw new IOException("conflicting Steam localization roots");root=prefix;platform=k;}
            }
            if(platform<0)throw new IOException("missing Steam localization");Set<String> names=new HashSet<>();
            for(int i=0;i<expected;i++){
                int nameLength=number(plan,256);String name=new String(bytes(plan,nameLength),StandardCharsets.US_ASCII);if((!name.matches("localization-string-tables-[A-Za-z0-9_()\\-]+\\.bundle")&&!name.equals(SHARED))||!names.add(name))throw new IOException("invalid localization name");
                int size=number(plan,2*1024*1024),packed=number(plan,2*1024*1024),count=number(plan,10000);byte[] blankHash=bytes(plan,32),resultHash=bytes(plan,32),blank=inflate(bytes(plan,packed),size);if(!Arrays.equals(hash(blank),blankHash))throw new IOException("blank locale differs");byte[] header=bytes(plan,number(plan,8192));
                int[] sourceSize=new int[2],start=new int[2],outSize=new int[2];byte[][] sourceHash=new byte[2][],outHash=new byte[2][];
                for(int k=0;k<2;k++){sourceSize[k]=number(plan,2*1024*1024);start[k]=number(plan,2*1024*1024);outSize[k]=number(plan,2*1024*1024);sourceHash[k]=bytes(plan,32);outHash[k]=bytes(plan,32);if(start[k]>sourceSize[k])throw new IOException("invalid source block");}
                byte[] source=entry(zip,entries.get(name),2*1024*1024);if(source.length!=sourceSize[platform]||!Arrays.equals(hash(source),sourceHash[platform]))throw new IOException("Steam localization differs");
                byte[] decoded=lz4(source,start[platform],outSize[platform]);if(!Arrays.equals(hash(decoded),outHash[platform]))throw new IOException("Steam locale block differs");
                for(int j=0;j<count;j++){int dst=number(plan,size),len=number(plan,size),linux=number(plan,outSize[0]),windows=number(plan,outSize[1]);int from=platform==0?linux:windows;if(len==0||dst>size-len||from>decoded.length-len)throw new IOException("invalid locale range");for(int k=0;k<len;k++)if(blank[dst+k]!=0)throw new IOException("locale range not blank");System.arraycopy(decoded,from,blank,dst,len);}
                if(!Arrays.equals(hash(blank),resultHash))throw new IOException("restored locale differs");Files.write(temp.resolve(name),SteamUnityFsRepack.repack(header,blank),StandardOpenOption.CREATE_NEW);
            }
            if(plan.read()!=-1)throw new IOException("trailing locale data");Files.move(temp,dest);moved=true;
        }finally{if(!moved){try(DirectoryStream<Path> files=Files.newDirectoryStream(temp)){for(Path f:files)Files.deleteIfExists(f);}Files.deleteIfExists(temp);}}
    }
    private SteamLocalizationText(){}
}
