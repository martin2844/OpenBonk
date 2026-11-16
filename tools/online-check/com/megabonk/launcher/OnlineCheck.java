package com.megabonk.launcher;
import java.io.File;
import org.json.JSONObject;

public final class OnlineCheck {
    public static void main(String[] args) throws Exception {
        File zip = new File(args[0]);
        LauncherOnline.checkModId(zip, "pikachu");
        try { LauncherOnline.checkModId(zip, "wrong-id"); throw new AssertionError("wrong mod ID accepted"); }
        catch (java.io.IOException expected) { }
        File result = File.createTempFile("openbonk-online-check", ".zip");
        try {
            JSONObject item = new JSONObject().put("size", 411262).put("sha256", "200bcd95347c695b75f464aa0f824d8d2e2fb21d069fb0c713eb5d6d67bc611c")
                .put("url", "https://github.com/martin2844/OpenBonk/releases/download/v0.1.0-beta/Pikachu-mod.zip");
            LauncherOnline.download(item, result, 1024 * 1024); LauncherOnline.checkModId(result, "pikachu");
            item.put("sha256", "0000000000000000000000000000000000000000000000000000000000000000");
            try { LauncherOnline.download(item, result, 1024 * 1024); throw new AssertionError("bad checksum accepted"); }
            catch (java.io.IOException expected) { if (result.exists()) throw new AssertionError("invalid download retained"); }
            item.put("url", "https://example.com/a");
            try { LauncherOnline.download(item, result, 1024 * 1024); throw new AssertionError("untrusted host accepted"); }
            catch (java.io.IOException expected) { }
            System.out.println("Online download, integrity and mod ID checks passed");
        } finally { result.delete(); }
    }
}
