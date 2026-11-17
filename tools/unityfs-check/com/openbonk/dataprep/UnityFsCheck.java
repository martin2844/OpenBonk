package com.openbonk.dataprep;
import java.nio.file.Files;
import java.nio.file.Paths;
import java.security.MessageDigest;
import java.util.Arrays;

public final class UnityFsCheck {
    private static void reject(byte[] data) throws Exception {
        try { SteamUnityFs.readSingleCab(data); throw new AssertionError("accepted malformed bundle"); }
        catch (java.io.IOException expected) { }
    }
    public static void main(String[] args) throws Exception {
        if (args.length == 0) {
            if (!Arrays.equals(SteamUnityFs.decompress(new byte[]{0x32, 'a','b','c', 3,0},9,2),"abcabcabc".getBytes("UTF-8"))) throw new AssertionError("overlap decode");
            for (byte[] bad : new byte[][] {{}, {0,0,0}, {0x10, 'a', 0,0}, {(byte)0xf0}}) {
                try { SteamUnityFs.decompress(bad, 20, 2); throw new AssertionError("accepted malformed block"); }
                catch (java.io.IOException expected) { }
            }
            reject(new byte[64]);
            System.out.println("PASS: LZ4 overlap and malformed blocks"); return;
        }
        for (String path: args) {
            byte[] src=Files.readAllBytes(Paths.get(path)), output=SteamUnityFs.readSingleCab(src);
            reject(Arrays.copyOf(src, src.length - 1));
            byte[] damaged = src.clone(); damaged[0] ^= 1; reject(damaged);
            byte[] digest=MessageDigest.getInstance("SHA-256").digest(output);
            StringBuilder hex=new StringBuilder(); for(byte b:digest) hex.append(String.format("%02x",b & 255));
            System.out.println(path+" "+output.length+" "+hex);
        }
    }
}
