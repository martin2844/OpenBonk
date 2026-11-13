package com.openbonk.dataprep;

import java.io.File;
import java.io.IOException;
import java.nio.file.Files;
import java.security.MessageDigest;
import java.security.NoSuchAlgorithmException;
import java.util.Map;

/** Recovers 25 direct animations or 108 using a separate private patch plan. */
public final class SteamAnimationResources {
    private static final String PLAN_SHA="c44f6ec24a439fe9d2ab86cdeaf888f64024c95977764fef15a1d1353fc2d241";
    private static final String PRIVATE_PLAN_SHA="c6344331782f79bcc2c26dfb10ecfc942ef435c6e31bd237a5bca49c9b8aeaec";
    private static final String BANDIT_DIRECT_PLAN_SHA="4c7f8b9ac3561896589ceae08e074a6e6a0f0216e9347fcfc8e05ef52d62da0a";
    private static final String KNIGHT_DIRECT_PLAN_SHA="5234be766f35adcef52b6b8114d0273f0be1f5a60d3e89d78e9e90a14e12ba89";
    private static final String BATCH_DIRECT_PLAN_SHA="4c31e0fa6c7c9b95bfbbccac7cc2786e6ed5e5d4d6faa59ddaf4b0a4f6528cfb";
    private static final String BATCH2_DIRECT_PLAN_SHA="c744c59fa5d60e6f73285e8277c7615db8fd38201298d6466ca7d4bde47a04a7";
    private static final String BATCH3_DIRECT_PLAN_SHA="857857af2a38b4452112d58c8e157f2448a5e8b58838159ef93b762676d64a33";
    private static final String SHARED_DIRECT_PLAN_SHA="72b2f5bbff0e255c2fa14af35d73b22a2c81f65bb5725cc92c00e09cffd3a975";
    private static int check(File plan)throws IOException{
        if(!plan.isFile()||plan.length()<=0||plan.length()>1024*1024)throw new IOException("invalid animation plan");
        try{
            byte[] hash=MessageDigest.getInstance("SHA-256").digest(Files.readAllBytes(plan.toPath()));
            char[] digits="0123456789abcdef".toCharArray(),text=new char[hash.length*2];
            for(int i=0;i<hash.length;i++){text[2*i]=digits[(hash[i]&255)>>>4];text[2*i+1]=digits[hash[i]&15];}
            String value=new String(text);if(value.equals(PLAN_SHA))return 25;if(value.equals(PRIVATE_PLAN_SHA)||value.equals(BANDIT_DIRECT_PLAN_SHA)||value.equals(KNIGHT_DIRECT_PLAN_SHA)||value.equals(BATCH_DIRECT_PLAN_SHA)||value.equals(BATCH2_DIRECT_PLAN_SHA)||value.equals(BATCH3_DIRECT_PLAN_SHA)||value.equals(SHARED_DIRECT_PLAN_SHA))return 108;throw new IOException("incompatible animation plan");
        }catch(NoSuchAlgorithmException e){throw new AssertionError(e);}
    }
    public static Map<String,byte[]> restore(File base,File steam,File plan)throws IOException{
        int expected=check(plan);Map<String,byte[]> result=SteamExactTextures.restoreAnimations(base,steam,plan,expected==108);
        if(result.size()!=expected)throw new IOException("incomplete animation set");return result;
    }
    private SteamAnimationResources(){}
}
