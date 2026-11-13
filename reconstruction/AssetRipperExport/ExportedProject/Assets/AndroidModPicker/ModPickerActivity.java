package com.megabonk.modpicker;

import android.app.Activity;
import android.content.Intent;
import android.net.Uri;
import android.os.Bundle;
import com.unity3d.player.UnityPlayer;
import java.io.File;
import java.io.FileOutputStream;
import java.io.InputStream;

public final class ModPickerActivity extends Activity {
    private static final int PICK_ZIP = 918;
    private static final int LIMIT = 1024 * 1024;

    @Override protected void onCreate(Bundle state) {
        super.onCreate(state);
        if (state != null) return;
        try {
            Intent pick = new Intent(Intent.ACTION_OPEN_DOCUMENT);
            pick.addCategory(Intent.CATEGORY_OPENABLE);
            pick.setType("*/*");
            pick.putExtra(Intent.EXTRA_MIME_TYPES, new String[]{"application/zip", "application/x-zip-compressed", "application/octet-stream"});
            pick.addFlags(Intent.FLAG_GRANT_READ_URI_PERMISSION);
            startActivityForResult(pick, PICK_ZIP);
        } catch (Exception e) { finishWith("error:selector unavailable"); }
    }

    @Override protected void onActivityResult(int request, int result, Intent data) {
        super.onActivityResult(request, result, data);
        if (request != PICK_ZIP) return;
        if (result != RESULT_OK || data == null || data.getData() == null) { finishWith("cancel"); return; }
        File partial = new File(getCacheDir(), "megabonk-mod-import.partial");
        File complete = new File(getCacheDir(), "megabonk-mod-import.zip");
        partial.delete();
        complete.delete();
        try {
            Uri uri = data.getData();
            try (InputStream input = getContentResolver().openInputStream(uri);
                 FileOutputStream output = new FileOutputStream(partial)) {
                if (input == null) throw new IllegalStateException("empty document");
                byte[] buffer = new byte[8192];
                int read, total = 0;
                while ((read = input.read(buffer)) != -1) {
                    total += read;
                    if (total > LIMIT) throw new IllegalStateException("ZIP exceeds 1 MiB");
                    output.write(buffer, 0, read);
                }
            }
            if (!partial.renameTo(complete)) throw new IllegalStateException("cannot stage ZIP");
            finishWith("ok");
        } catch (Exception e) {
            partial.delete(); complete.delete();
            finishWith("error:" + e.getClass().getSimpleName());
        }
    }

    private void finishWith(String result) {
        UnityPlayer.UnitySendMessage("AndroidModImportReceiver", "OnPickerResult", result);
        finish();
    }
}
