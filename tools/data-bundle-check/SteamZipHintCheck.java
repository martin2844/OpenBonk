package com.megabonk.launcher;

import java.io.*;
import java.nio.file.Files;
import java.util.zip.*;

/** The hint must never substitute for the full Steam archive validator. */
public final class SteamZipHintCheck {
    private static void zip(File file, String... names) throws IOException {
        try (ZipOutputStream out = new ZipOutputStream(new FileOutputStream(file))) {
            for (String name : names) { out.putNextEntry(new ZipEntry(name)); out.write(1); out.closeEntry(); }
        }
    }
    private static void require(boolean value) { if (!value) throw new AssertionError(); }
    public static void main(String[] args) throws Exception {
        File dir=Files.createTempDirectory("openbonk-hint-").toFile(); File f=new File(dir,"test.zip");
        try {
            zip(f,"Megabonk_Data/sharedassets1.assets","Megabonk_Data/sharedassets1.assets.resS");
            require(OriginalGameDataValidator.looksLikeSteamZip(f));
            require(!OriginalGameDataValidator.valid(f));
            zip(f,"copy/Megabonk_Data/sharedassets1.assets","copy/Megabonk_Data/sharedassets1.assets.resS");
            require(OriginalGameDataValidator.looksLikeSteamZip(f));
            zip(f,"openbonk-data.obb","steam-data.zip");
            require(!OriginalGameDataValidator.looksLikeSteamZip(f));
            zip(f,"Megabonk_Data/sharedassets1.assets");
            require(!OriginalGameDataValidator.looksLikeSteamZip(f));
            try (FileOutputStream out=new FileOutputStream(f)) {out.write(1);}
            require(!OriginalGameDataValidator.looksLikeSteamZip(f));
            System.out.println("PASS: raw Steam hint, incomplete/invalid data and prepared bundle classification");
        } finally { f.delete(); dir.delete(); }
    }
}
