package com.openbonk.dataprep;
import java.io.*;
import java.security.MessageDigest;
import java.util.*;
import java.util.zip.*;
import static com.openbonk.dataprep.SteamExactTextures.*;

/** Experimental reconstruction of the second shared Steam audio resource. */
public final class SteamShared2AudioResource {
    private static final String PLAN="4c63e54947ed2e00f8c0fb7c4c5462f05ad866a11850794fb490ecf994f7f323";
    private static final String PLAN4="65100412ab3b73986323bf802c1dc7fd2dc00a879b9cea18633b16c5ec0142f4";
    private static final class Clip{long target,source;int size;byte[] hash;}
    public static byte[] restore(File steamFile,File planFile)throws IOException{
        return restoreShared(steamFile,planFile,false);
    }
    static byte[] restoreShared(File steamFile,File planFile,boolean fourth)throws IOException{
        byte[] data=file(planFile,1024*1024);if(!hex(hash(data)).equals(fourth?PLAN4:PLAN))throw new IOException("incompatible shared audio plan");
        if(!steamFile.isFile()||steamFile.length()<=0||steamFile.length()>1024L*1024*1024)throw new IOException("invalid Steam ZIP");
        try(ZipFile zip=new ZipFile(steamFile);DataInputStream plan=new DataInputStream(new ByteArrayInputStream(data))){
            int clipCount=fourth?6:124;
            if(!Arrays.equals(bytes(plan,8),new byte[]{'O','B','S','S',(byte)(fourth?'C':'B'),'0','0','1'})||plan.readInt()!=clipCount)throw new IOException("invalid shared audio plan");
            byte[] la=bytes(plan,32),lr=bytes(plan,32),wa=bytes(plan,32),wr=bytes(plan,32);int size=plan.readInt();byte[] wanted=bytes(plan,32);
            if(size!=(fourth?1780640:5365152))throw new IOException("invalid resource size");
            List<Clip> clips=new ArrayList<>();long last=0;
            for(int i=0;i<clipCount;i++){
                Clip c=new Clip();c.target=plan.readLong();c.source=plan.readLong();c.size=plan.readInt();c.hash=bytes(plan,32);
                if(c.target<last||c.target-last>=32||c.size<4||c.size>size||c.target>size-c.size||c.source<0||c.source>size-c.size)throw new IOException("invalid audio range");last=c.target+c.size;clips.add(c);
            }
            if(last!=size||plan.read()!=-1)throw new IOException("invalid audio layout");
            ZipEntry asset=null,resource=null;String common=null;Set<String> seen=new HashSet<>();Enumeration<? extends ZipEntry> entries=zip.entries();
            while(entries.hasMoreElements()){
                ZipEntry e=entries.nextElement();String n=e.getName();if(seen.size()>=20000||n.startsWith("/")||n.indexOf('\\')>=0||n.indexOf(':')>=0||!seen.add(n))throw new IOException("unsafe ZIP entry");
                for(String part:n.split("/"))if(part.isEmpty()||part.equals(".")||part.equals(".."))throw new IOException("unsafe ZIP path");
                String ar=rootFor(n,fourth?"sharedassets4.assets":"sharedassets2.assets"),rr=rootFor(n,fourth?"sharedassets4.resource":"sharedassets2.resource"),root=ar!=null?ar:rr;
                if(root!=null){if(common!=null&&!common.equals(root))throw new IOException("conflicting roots");common=root;if(ar!=null){if(asset!=null)throw new IOException("duplicate asset");asset=e;}else{if(resource!=null)throw new IOException("duplicate resource");resource=e;}}
            }
            if(asset==null||resource==null)throw new IOException("missing Steam source");boolean windows;
            if(asset.getSize()==(fourth?693876L:5503192L))windows=true;else if(asset.getSize()==(fourth?693868L:5824160L))windows=false;else throw new IOException("unsupported Steam asset");
            if(!MessageDigest.isEqual(SteamSharedAudioResource.hashEntry(zip,asset,asset.getSize()),windows?wa:la))throw new IOException("Steam asset differs");
            byte[] source=entry(zip,resource,8*1024*1024);if(source.length!=size||!MessageDigest.isEqual(hash(source),windows?wr:lr))throw new IOException("Steam resource differs");
            byte[] result=new byte[size];
            for(Clip c:clips){int s=(int)c.source,d=(int)c.target;if(source[s]!='F'||source[s+1]!='S'||source[s+2]!='B'||source[s+3]!='5')throw new IOException("invalid FSB");byte[] clip=Arrays.copyOfRange(source,s,s+c.size);if(!MessageDigest.isEqual(hash(clip),c.hash))throw new IOException("audio clip differs");System.arraycopy(clip,0,result,d,c.size);}
            if(!MessageDigest.isEqual(hash(result),wanted))throw new IOException("reconstructed resource differs");return result;
        }
    }
    private SteamShared2AudioResource(){}
}
