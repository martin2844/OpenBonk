package com.openbonk.dataprep;
import java.io.*;
import java.security.MessageDigest;
import java.util.*;
import java.util.zip.*;
import static com.openbonk.dataprep.SteamExactTextures.*;

/** Reconstructs three Unity-generated shader bodies from a private compiled pack. */
public final class SteamBuiltinShaderResources {
 private static final String PLAN="4016359642b21aa4f8689cab79b19b4ddc9c90b10362dd4d4072eaca9dca9bae",NAME="0000000000000000f000000000000000";
 public static Map<String,byte[]> restore(File partial,File packFile,File planFile)throws IOException{
  byte[] planData=file(planFile,8192);if(!hex(hash(planData)).equals(PLAN))throw new IOException("incompatible builtin shader plan");
  try(DataInputStream plan=new DataInputStream(new ByteArrayInputStream(planData))){
   if(!Arrays.equals(bytes(plan,8),new byte[]{'O','B','B','S','H','0','0','1'}))throw new IOException("invalid shader plan");int size=plan.readInt(),count=plan.readInt();if(size<=0||size>8*1024*1024||count!=3)throw new IOException("invalid shader size");byte[] before=bytes(plan,32),after=bytes(plan,32),packHash=bytes(plan,32);
   byte[] packed=file(packFile,8*1024*1024);if(!MessageDigest.isEqual(hash(packed),packHash))throw new IOException("shader pack differs");
   try(ZipFile archive=new ZipFile(partial);ZipFile pack=new ZipFile(packFile)){
    byte[] asset=entry(archive,archive.getEntry("assets/bin/Data/"+NAME),8*1024*1024);if(asset.length!=size||!MessageDigest.isEqual(hash(asset),before)||pack.size()!=3)throw new IOException("shader base differs");int last=0;
    for(int j=1;j<=3;j++){
     int id=plan.readInt(),off=plan.readInt(),length=plan.readInt();byte[] wanted=bytes(plan,32);if(id!=j||off<last||length<=0||off>size-length)throw new IOException("invalid shader range");last=off+length;byte[] value=entry(pack,pack.getEntry("shader-"+id+".bin"),8*1024*1024);if(value.length!=length||!MessageDigest.isEqual(hash(value),wanted))throw new IOException("compiled shader differs");for(int i=off;i<off+length;i++)if(asset[i]!=0)throw new IOException("shader destination not blank");System.arraycopy(value,0,asset,off,length);
    }
    if(plan.read()!=-1||!MessageDigest.isEqual(hash(asset),after))throw new IOException("restored builtin shaders differ");return Collections.singletonMap(NAME,asset);
   }
  }
 }
 private SteamBuiltinShaderResources(){}
}
