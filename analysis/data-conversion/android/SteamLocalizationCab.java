package com.openbonk.dataprep;

import java.io.IOException;
import java.nio.ByteBuffer;
import java.nio.ByteOrder;
import java.security.MessageDigest;
import java.security.NoSuchAlgorithmException;
import java.util.Arrays;

/** Experimental exact-fragment localization reconstruction; not launcher-integrated. */
public final class SteamLocalizationCab {
    private static final int LIMIT = 32 * 1024 * 1024;
    private SteamLocalizationCab() {}
    private static byte[] hash(byte[] data) {
        try { return MessageDigest.getInstance("SHA-256").digest(data); }
        catch (NoSuchAlgorithmException e) { throw new AssertionError(e); }
    }
    private static void require(boolean ok) throws IOException { if (!ok) throw new IOException("Invalid localization plan or input"); }
    private static byte[] read(ByteBuffer p, int n) throws IOException {
        require(n >= 0 && n <= p.remaining()); byte[] out=new byte[n]; p.get(out); return out;
    }
    private static int integer(ByteBuffer p) throws IOException { require(p.remaining() >= 4); return p.getInt(); }
    public static byte[] restore(byte[] template, byte[] source, byte[] plan, byte[] trustedPlanHash) throws IOException {
        require(template != null && source != null && plan != null && trustedPlanHash != null && trustedPlanHash.length == 32 && template.length <= LIMIT && source.length <= LIMIT && plan.length <= 1024*1024);
        require(MessageDigest.isEqual(trustedPlanHash,hash(plan)));
        ByteBuffer p = ByteBuffer.wrap(plan).order(ByteOrder.BIG_ENDIAN);
        require(Arrays.equals(read(p,4),new byte[]{'O','B','L','1'}));
        byte[] sourceHash=read(p,32), templateHash=read(p,32), outputHash=read(p,32);
        require(MessageDigest.isEqual(sourceHash,hash(source)) && MessageDigest.isEqual(templateHash,hash(template)));
        int count=integer(p); require(count>0 && count<=10000 && p.remaining()==count*44);
        byte[] cab=SteamUnityFs.readSingleCab(source), result=template.clone(); int previous=0;
        for (int n=0;n<count;n++) {
            int target=integer(p), sourceOffset=integer(p), length=integer(p); byte[] expected=read(p,32);
            require(target>=previous && sourceOffset>=0 && length>0 && target<=result.length-length && sourceOffset<=cab.length-length);
            for (int x=target;x<target+length;x++) require(result[x]==0);
            MessageDigest md;
            try { md=MessageDigest.getInstance("SHA-256"); } catch (NoSuchAlgorithmException e) { throw new AssertionError(e); }
            md.update(cab,sourceOffset,length); require(MessageDigest.isEqual(expected,md.digest()));
            System.arraycopy(cab,sourceOffset,result,target,length); previous=target+length;
        }
        require(MessageDigest.isEqual(outputHash,hash(result)));
        return result;
    }
}
