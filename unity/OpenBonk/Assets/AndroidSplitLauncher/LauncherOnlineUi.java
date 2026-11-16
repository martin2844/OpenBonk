package com.megabonk.launcher;

import android.app.AlertDialog;
import android.content.Intent;
import android.net.Uri;
import android.os.Build;
import android.provider.Settings;
import android.text.Editable;
import android.text.TextWatcher;
import android.widget.ArrayAdapter;
import android.widget.EditText;
import android.widget.LinearLayout;
import android.widget.ListView;
import java.io.File;
import java.io.IOException;
import java.util.ArrayList;
import java.util.Locale;
import org.json.JSONArray;
import org.json.JSONObject;

final class LauncherOnlineUi {
    private static final java.util.concurrent.atomic.AtomicBoolean DOWNLOADING = new java.util.concurrent.atomic.AtomicBoolean();
    private static boolean alive(SplitDataLauncher a) { return !a.isFinishing() && !a.isDestroyed(); }
    private static void error(SplitDataLauncher a, boolean es, Exception e) {
        a.runOnUiThread(() -> { if (alive(a)) new AlertDialog.Builder(a).setTitle(es ? "No se pudo completar" : "Could not complete")
            .setMessage(LauncherImportError.describe(e, es)).setPositiveButton("OK", null).show(); });
    }
    static void browse(SplitDataLauncher a, boolean es) {
        new Thread(() -> {
            try {
                JSONArray entries = LauncherOnline.metadata(LauncherOnline.CATALOG).getJSONArray("mods");
                if (entries.length() > 500) throw new IOException("catalog too large");
                a.runOnUiThread(() -> { if (alive(a)) catalog(a, es, entries); });
            } catch (Exception e) { error(a, es, e); }
        }, "openbonk-mod-catalog").start();
    }
    private static void catalog(SplitDataLauncher a, boolean es, JSONArray entries) {
        LinearLayout panel = new LinearLayout(a); panel.setOrientation(LinearLayout.VERTICAL);
        EditText search = new EditText(a); search.setSingleLine(true); search.setHint(es ? "Buscar mods" : "Search mods"); panel.addView(search);
        ListView list = new ListView(a); panel.addView(list, new LinearLayout.LayoutParams(-1, (int)(230 * a.getResources().getDisplayMetrics().density)));
        ArrayList<JSONObject> visible = new ArrayList<>();
        ArrayAdapter<String> adapter = new ArrayAdapter<>(a, android.R.layout.simple_list_item_1); list.setAdapter(adapter);
        Runnable filter = () -> {
            String query = search.getText().toString().toLowerCase(Locale.ROOT); visible.clear(); adapter.clear();
            for (int i = 0; i < entries.length(); i++) {
                JSONObject item = entries.optJSONObject(i); if (item == null) continue;
                String name = item.optString("name"), description = item.optString("description");
                if ((name + " " + description).toLowerCase(Locale.ROOT).contains(query)) { visible.add(item); adapter.add(name + "  ·  " + item.optString("version")); }
            }
        };
        search.addTextChangedListener(new TextWatcher() {
            public void beforeTextChanged(CharSequence s, int start, int count, int after) { }
            public void onTextChanged(CharSequence s, int start, int before, int count) { filter.run(); }
            public void afterTextChanged(Editable e) { }
        });
        filter.run(); AlertDialog dialog = new AlertDialog.Builder(a).setTitle(es ? "Catálogo de mods" : "Mod catalog").setView(panel).setNegativeButton(es ? "Cerrar" : "Close", null).create();
        list.setOnItemClickListener((parent, view, position, id) -> {
            JSONObject item = visible.get(position);
            new AlertDialog.Builder(a).setTitle(item.optString("name"))
                .setMessage(item.optString("description") + "\n\n" + (es ? "El mod se importará desactivado." : "The mod will be imported disabled."))
                .setNegativeButton(es ? "Cancelar" : "Cancel", null).setPositiveButton(es ? "Importar" : "Import", (d, w) -> { dialog.dismiss(); a.importOnlineMod(item); }).show();
        });
        dialog.show();
    }
    static void checkUpdate(SplitDataLauncher a, boolean es) { checkUpdate(a, es, false); }
    static void checkUpdate(SplitDataLauncher a, boolean es, boolean automatic) {
        new Thread(() -> {
            try {
                JSONObject info = LauncherOnline.metadata(LauncherOnline.UPDATES);
                long version = info.getLong("version_code");
                long installed = a.getPackageManager().getPackageInfo(a.getPackageName(), 0).versionCode;
                a.runOnUiThread(() -> { if (!alive(a)) return;
                    if (version <= installed && !automatic) new AlertDialog.Builder(a).setMessage(es ? "Ya tenés la última versión publicada." : "You have the latest published version.").setPositiveButton("OK", null).show();
                    else if (version > installed) new AlertDialog.Builder(a).setTitle(es ? "Actualización disponible" : "Update available")
                        .setMessage(info.optString("tag") + "\n\n" + (es ? "Android te pedirá confirmar la instalación." : "Android will ask you to confirm installation."))
                        .setNegativeButton(es ? "Cancelar" : "Cancel", null).setPositiveButton(es ? "Descargar" : "Download", (d, w) -> downloadUpdate(a, es, info)).show();
                });
            } catch (Exception e) { if (!automatic) error(a, es, e); }
        }, "openbonk-update-check").start();
    }
    private static void downloadUpdate(SplitDataLauncher a, boolean es, JSONObject info) {
        if (Build.VERSION.SDK_INT >= 26 && !a.getPackageManager().canRequestPackageInstalls()) {
            new AlertDialog.Builder(a).setMessage(es ? "Permití que OpenBonk instale actualizaciones y volvé a buscar la actualización." : "Allow OpenBonk to install updates, then check again.")
                .setNegativeButton(es ? "Cancelar" : "Cancel", null).setPositiveButton(es ? "Abrir ajustes" : "Open settings", (d, w) -> {
                    try { a.startActivity(new Intent(Settings.ACTION_MANAGE_UNKNOWN_APP_SOURCES, Uri.parse("package:" + a.getPackageName()))); } catch (Exception e) { error(a, es, e); }
                }).show(); return;
        }
        if (!DOWNLOADING.compareAndSet(false, true)) return;
        new Thread(() -> {
            File temp = null;
            try {
                temp = File.createTempFile("openbonk-update-", ".apk", a.getCacheDir());
                LauncherOnline.download(info, temp, 512L * 1024 * 1024); LauncherOnline.checkApk(a, temp, info.getLong("version_code"));
                File dest = new File(a.getCacheDir(), "openbonk-update.apk");
                if (!temp.renameTo(dest)) throw new IOException("cannot prepare update"); temp = null;
                a.runOnUiThread(() -> { if (!alive(a)) return;
                    Intent install = new Intent(Intent.ACTION_VIEW); install.setDataAndType(Uri.parse("content://" + a.getPackageName() + ".openbonk.updates/update.apk"), "application/vnd.android.package-archive");
                    install.addFlags(Intent.FLAG_GRANT_READ_URI_PERMISSION);
                    try { a.startActivity(install); } catch (Exception e) { error(a, es, e); }
                });
            } catch (Exception e) { error(a, es, e); } finally { if (temp != null) temp.delete(); DOWNLOADING.set(false); }
        }, "openbonk-update-download").start();
    }
}
