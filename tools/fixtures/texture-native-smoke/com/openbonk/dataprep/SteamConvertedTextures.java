package com.openbonk.dataprep;
import java.security.MessageDigest;
public class SteamConvertedTextures {
  static native byte[] convertMip(byte[] source, int width, int height, int sourceFormat, int targetFormat);
  static native byte[] convertNormalMip(byte[] source, int width, int height, int targetFormat);
  private static String hash(byte[] v) throws Exception { if(v==null)return "null"; StringBuilder s=new StringBuilder(); for(byte b:MessageDigest.getInstance("SHA-256").digest(v))s.append(String.format("%02x",b&255)); return s.toString(); }
  public static void main(String[] a) throws Exception {
    System.load(a[0]);
    int[] fmts={3,4,10,12}; int[] lengths={192,256,32,64}; int[] targets={4,34,45,47};
    for(int i=0;i<fmts.length;i++) { byte[] input=new byte[lengths[i]]; for(int j=0;j<input.length;j++)input[j]=(byte)(j*37+19); for(int target:targets)System.out.println(fmts[i]+":"+target+":"+hash(convertMip(input,8,8,fmts[i],target))); }
    byte[] blocks=new byte[64];for(int j=0;j<blocks.length;j++)blocks[j]=(byte)(j*31+7); for(int target:targets)System.out.println("normal:"+target+":"+hash(convertNormalMip(blocks,8,8,target)));
  }
}
