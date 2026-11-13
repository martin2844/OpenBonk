package com.openbonk.dataprep;
import java.io.*;
import java.security.MessageDigest;
import java.util.*;
import java.util.zip.*;
import static com.openbonk.dataprep.SteamExactTextures.*;

/** Restores two animations in the still-incomplete second-map asset. */
public final class SteamShared2Animations {
    private static final String PLAN="05157455a6d4ebd79b41c728e18690bab1af3b1753fc13ccc2ca809d499f1540";
    public static byte[] restorePartial(byte[] base,File steamFile,File planFile)throws IOException{
        if(base==null||base.length!=22278296)throw new IOException("invalid base asset");byte[] planBytes=file(planFile,1024*1024);
        if(!hex(hash(planBytes)).equals(PLAN))throw new IOException("incompatible animation plan");
        if(!steamFile.isFile()||steamFile.length()<=0||steamFile.length()>1024L*1024*1024)throw new IOException("invalid Steam ZIP");
        try(ZipFile zip=new ZipFile(steamFile);DataInputStream plan=new DataInputStream(new ByteArrayInputStream(planBytes))){
            if(!Arrays.equals(bytes(plan,8),new byte[]{'O','B','S','A','2','0','0','1'})||plan.readInt()!=2)throw new IOException("invalid animation plan");
            byte[] lh=bytes(plan,32),wh=bytes(plan,32);int size=plan.readInt();byte[] baseHash=bytes(plan,32),resultHash=bytes(plan,32),targetHash=bytes(plan,32);
            if(size!=base.length||!MessageDigest.isEqual(hash(base),baseHash)||MessageDigest.isEqual(resultHash,targetHash))throw new IOException("incompatible partial asset");
            List<Texture> ranges=new ArrayList<>();long previous=0;
            for(int i=0;i<2;i++){
                Texture t=new Texture();t.target=plan.readInt();t.size=plan.readInt();t.linuxOffset=plan.readLong();t.windowsOffset=plan.readLong();t.payloadHash=bytes(plan,32);t.linuxFile=t.windowsFile=0;
                if(t.target<previous||t.size<=0||t.size>1024*1024||t.target>size-t.size||t.linuxOffset<0||t.windowsOffset<0||t.linuxOffset>5824160L-t.size||t.windowsOffset>5503192L-t.size)throw new IOException("invalid animation range");
                previous=(long)t.target+t.size;t.data=new byte[t.size];ranges.add(t);
            }
            if(plan.read()!=-1)throw new IOException("trailing animation plan data");ZipEntry source=null;Set<String> names=new HashSet<>();Enumeration<? extends ZipEntry> entries=zip.entries();
            while(entries.hasMoreElements()){
                ZipEntry e=entries.nextElement();String n=e.getName();if(names.size()>=20000||n.startsWith("/")||n.indexOf('\\')>=0||n.indexOf(':')>=0||!names.add(n))throw new IOException("unsafe ZIP entry");
                for(String part:n.split("/"))if(part.isEmpty()||part.equals(".")||part.equals(".."))throw new IOException("unsafe ZIP path");
                if(rootFor(n,"sharedassets2.assets")!=null){if(source!=null)throw new IOException("multiple Steam assets");source=e;}
            }
            if(source==null)throw new IOException("missing Steam asset");boolean windows;if(source.getSize()==5503192L)windows=true;else if(source.getSize()==5824160L)windows=false;else throw new IOException("unsupported Steam asset");
            collect(zip,source,windows?5503192L:5824160L,windows?wh:lh,0,windows,ranges);byte[] result=base.clone();
            for(Texture t:ranges){if(t.filled!=t.size||!MessageDigest.isEqual(hash(t.data),t.payloadHash))throw new IOException("animation source differs");for(int i=0;i<t.size;i++)if(result[t.target+i]!=0&&result[t.target+i]!=t.data[i])throw new IOException("retained animation metadata differs");System.arraycopy(t.data,0,result,t.target,t.size);}
            if(!MessageDigest.isEqual(hash(result),resultHash))throw new IOException("animation result differs");return result;
        }
    }
    private SteamShared2Animations(){}
}
