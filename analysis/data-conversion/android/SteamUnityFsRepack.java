package com.openbonk.dataprep;
import java.io.*;
import java.nio.*;
import java.nio.charset.StandardCharsets;
import java.util.*;

/** Repackages a pinned single-block UnityFS template without compression. */
public final class SteamUnityFsRepack {
    private static int align(int n){return (n+15)&~15;}
    private static int cstring(byte[] b,int p,String expected)throws IOException{
        int start=p;while(p<b.length&&b[p]!=0)p++;if(p==b.length||!new String(b,start,p-start,StandardCharsets.US_ASCII).equals(expected))throw new IOException("invalid UnityFS header");return p+1;
    }
    public static byte[] repack(byte[] header,byte[] raw)throws IOException{
        if(header.length>8192||raw.length>2*1024*1024)throw new IOException("UnityFS input too large");
        int p=cstring(header,0,"UnityFS");if(header.length-p<4||ByteBuffer.wrap(header,p,4).getInt()!=8)throw new IOException("unsupported UnityFS version");p+=4;p=cstring(header,p,"5.x.x");p=cstring(header,p,"2023.2.22f1");int totalAt=p;
        if(header.length-p<20)throw new IOException("truncated UnityFS header");ByteBuffer b=ByteBuffer.wrap(header).order(ByteOrder.BIG_ENDIAN);int ci=b.getInt(p+8),ui=b.getInt(p+12),flags=b.getInt(p+16),infoAt=align(p+20);
        if(ci<=0||ui<=0||ui>8192||flags!=0x243||infoAt>header.length-ci||align(infoAt+ci)!=header.length)throw new IOException("unsupported UnityFS layout");
        byte[] compressedInfo=Arrays.copyOfRange(header,infoAt,infoAt+ci),info=SteamLocalizationText.lz4(compressedInfo,0,ui);ByteBuffer ib=ByteBuffer.wrap(info).order(ByteOrder.BIG_ENDIAN);
        if(ui<34||ib.getInt(16)!=1||ib.getInt(20)!=raw.length||ib.getShort(28)!=3)throw new IOException("unsupported UnityFS data block");
        byte[] data=raw;ib.putInt(24,data.length);ib.putShort(28,(short)0);byte[] infoData=info;int dataAt=align(infoAt+infoData.length);long total=(long)dataAt+data.length;if(total>Integer.MAX_VALUE)throw new IOException("UnityFS size overflow");
        byte[] result=new byte[(int)total];System.arraycopy(header,0,result,0,infoAt);ByteBuffer rb=ByteBuffer.wrap(result).order(ByteOrder.BIG_ENDIAN);rb.putLong(totalAt,total);rb.putInt(totalAt+8,infoData.length);rb.putInt(totalAt+12,info.length);rb.putInt(totalAt+16,0x240);System.arraycopy(infoData,0,result,infoAt,infoData.length);System.arraycopy(data,0,result,dataAt,data.length);return result;
    }
    private SteamUnityFsRepack(){}
}
