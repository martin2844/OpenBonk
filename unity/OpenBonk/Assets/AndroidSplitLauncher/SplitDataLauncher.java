package com.megabonk.launcher;

import android.app.Activity;
import android.app.AlertDialog;
import android.content.Intent;
import android.content.SharedPreferences;
import android.graphics.Color;
import android.graphics.Typeface;
import android.graphics.drawable.GradientDrawable;
import android.graphics.drawable.Drawable;
import android.graphics.drawable.StateListDrawable;
import android.content.res.ColorStateList;
import android.os.Bundle;
import android.view.Gravity;
import android.view.KeyEvent;
import android.view.View;
import android.view.WindowManager;
import android.widget.Button;
import android.widget.LinearLayout;
import android.widget.RadioButton;
import android.widget.RadioGroup;
import android.widget.TextView;
import android.widget.ImageView;
import android.widget.ScrollView;
import java.io.File;
import java.io.IOException;
import java.io.FileOutputStream;
import java.io.FileInputStream;
import java.io.InputStream;
import java.io.OutputStream;
import java.io.ByteArrayOutputStream;
import java.nio.charset.StandardCharsets;
import java.security.MessageDigest;
import java.util.Locale;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.Comparator;
import java.util.zip.ZipFile;
import org.json.JSONObject;
import org.json.JSONArray;
import java.util.HashSet;

/** Prototype launcher for the Android build's generated OBB, not PC data. */
public final class SplitDataLauncher extends Activity {
    private static final int PICK = 461;
    private static final int PICK_STEAM = 462;
    private static final int PICK_PROFILE = 463;
    private static final int PICK_MOD = 464;
    private static final int EXPORT_PROFILE_RECOVERY = 465;
    private static final int EXPORT_CURRENT_PROFILE = 466;
    private static final long MAX_BYTES = 512L * 1024 * 1024;
    private static final long MAX_STEAM_BYTES = 1024L * 1024 * 1024;
    private static final long MAX_BUNDLE_BYTES = MAX_BYTES + MAX_STEAM_BYTES + 1024L * 1024;
    private static final long MAX_PROFILE_BYTES = 16L * 1024 * 1024;
    // Activity recreation can overlap an existing background import.
    private static final Object DATA_LOCK = new Object();
    private static final LauncherImportGate IMPORT_GATE = new LauncherImportGate();
    private static final String LAUNCHER_PREFS = "openbonk_launcher";
    private static final String PENDING_CONTROL = "pending_touch_mode";
    private static final Object CONTROL_LOCK = new Object();
    private TextView message, modHint;
    private Button choose;
    private Button play;
    private Button chooseSteam;
    private Button chooseProfile;
    private Button exportCurrentProfile;
    private Button mods;
    private Button importModButton;
    private RadioGroup controls;
    private boolean es;
    private boolean dataReady;
    private boolean controlChanged;
    private int touchMode;
    private boolean swapMenuButtons;
    private String exportProfileId;
    private String exportCurrentAccount;
    private static boolean embeddedData() { return "EMBEDDED".equals(SplitExpectedData.SHA256); }

    public static void showImportResult(boolean success) {
        Activity activity = com.unity3d.player.UnityPlayer.currentActivity;
        if (activity == null) return;
        boolean spanish = Locale.getDefault().getLanguage().equals("es");
        String message = success ? (spanish ? "Textos y texturas importados" : "Text and textures imported") :
            (spanish ? "No se pudieron importar los textos y texturas. Volvé a abrir el juego para reintentar." : "Could not import text and textures. Reopen the game to retry.");
        activity.runOnUiThread(() -> android.widget.Toast.makeText(activity, message, android.widget.Toast.LENGTH_LONG).show());
    }

    public static void showProfileImportResult(String status) {
        Activity activity = com.unity3d.player.UnityPlayer.currentActivity;
        if (activity == null) return;
        boolean spanish = Locale.getDefault().getLanguage().equals("es");
        String message;
        if (status.equals("success")) message = spanish ? "Perfil importado; se conservó una copia anterior" : "Profile imported; previous profile backed up";
        else if (status.equals("interrupted")) message = spanish ? "Importación de perfil interrumpida: revisá el perfil antes de volver a intentarlo" : "Profile import was interrupted; check your profile before retrying";
        else message = spanish ? "No se pudo importar el perfil" : "Could not import the profile";
        activity.runOnUiThread(() -> android.widget.Toast.makeText(activity, message, android.widget.Toast.LENGTH_LONG).show());
    }

    @Override public void onCreate(Bundle state) {
        super.onCreate(state);
        exportProfileId = state == null ? null : state.getString("exportProfileId");
        exportCurrentAccount = state == null ? null : state.getString("exportCurrentAccount");
        es = Locale.getDefault().getLanguage().equals("es");
        swapMenuButtons = readControlSetting("swap_menu_ab") != 0;
        getWindow().getDecorView().setSystemUiVisibility(View.SYSTEM_UI_FLAG_FULLSCREEN | View.SYSTEM_UI_FLAG_HIDE_NAVIGATION | View.SYSTEM_UI_FLAG_IMMERSIVE_STICKY);
        LinearLayout layout = new LinearLayout(this);
        layout.setOrientation(LinearLayout.VERTICAL);
        layout.setPadding(dp(18), dp(10), dp(18), dp(10)); layout.setBackgroundColor(Color.rgb(25, 8, 39));
        LinearLayout header = new LinearLayout(this); header.setGravity(Gravity.CENTER_VERTICAL);
        TextView title = new TextView(this); title.setText("OPENBONK  ✦"); title.setTextColor(Color.rgb(255, 164, 35)); title.setTextSize(27); title.setTypeface(Typeface.DEFAULT, Typeface.BOLD);
        header.addView(title, new LinearLayout.LayoutParams(0, -2, 1));
        if (hasTextureNotices()) {
            Button notices = new Button(this); notices.setText(es ? "Avisos" : "Notices");
            notices.setOnClickListener(v -> showTextureNotices()); styleButton(notices, false);
            header.addView(notices, new LinearLayout.LayoutParams(dp(100), dp(37)));
        }
        layout.addView(header);
        LinearLayout body = new LinearLayout(this); body.setOrientation(LinearLayout.HORIZONTAL); body.setGravity(Gravity.CENTER_VERTICAL);
        layout.addView(body, new LinearLayout.LayoutParams(-1, 0, 1));
        LinearLayout art = new LinearLayout(this); art.setOrientation(LinearLayout.VERTICAL); art.setGravity(Gravity.CENTER);
        body.addView(art, new LinearLayout.LayoutParams(0, -1, 0.8f));
        ImageView mascot = new ImageView(this);
        int mascotId = getResources().getIdentifier("openbonk_mascot", "drawable", getPackageName());
        if (mascotId != 0) mascot.setImageResource(mascotId);
        else try (InputStream in = getAssets().open("openbonk-diagnostic/openbonk_mascot.png")) {
            mascot.setImageDrawable(Drawable.createFromStream(in, null));
        } catch (IOException ignored) { }
        mascot.setAdjustViewBounds(true); mascot.setScaleType(ImageView.ScaleType.FIT_CENTER);
        art.addView(mascot, new LinearLayout.LayoutParams(-1, 0, 1));
        TextView tagline = new TextView(this); tagline.setText(es ? "TU AVENTURA. TUS REGLAS." : "YOUR ADVENTURE. YOUR RULES."); tagline.setTextColor(Color.rgb(213, 245, 95)); tagline.setTextSize(10); tagline.setGravity(Gravity.CENTER); tagline.setTypeface(Typeface.DEFAULT, Typeface.BOLD);
        art.addView(tagline);
        LinearLayout left = new LinearLayout(this); left.setOrientation(LinearLayout.VERTICAL); left.setPadding(dp(8), 0, dp(5), 0);
        body.addView(left, new LinearLayout.LayoutParams(0, -2, 1.2f));
        LinearLayout right = new LinearLayout(this); right.setOrientation(LinearLayout.VERTICAL); right.setPadding(dp(5), 0, 0, 0);
        body.addView(right, new LinearLayout.LayoutParams(0, -2, 1f));
        LinearLayout dataCard = card(); left.addView(dataCard);
        LinearLayout dataHeading = new LinearLayout(this); dataHeading.setGravity(Gravity.CENTER_VERTICAL);
        dataHeading.addView(cardTitle(es ? "DATOS DEL JUEGO" : "GAME DATA"), new LinearLayout.LayoutParams(0, -2, 1));
        Button dataHelp = new Button(this); dataHelp.setText("?"); dataHelp.setContentDescription(es ? "Cómo preparar los datos del juego" : "How to prepare game data");
        dataHelp.setOnClickListener(v -> showGameDataHelp()); styleButton(dataHelp, false);
        dataHeading.addView(dataHelp, new LinearLayout.LayoutParams(dp(40), dp(32))); dataCard.addView(dataHeading);
        message = new TextView(this); message.setTextColor(Color.rgb(225, 205, 235)); message.setTextSize(12); message.setPadding(0, dp(3), 0, dp(4));
        message.setText(es ? "Comprobando datos…" : "Checking data…");
        dataCard.addView(message);
        SharedPreferences launcherPrefs = getSharedPreferences(LAUNCHER_PREFS, MODE_PRIVATE);
        controlChanged = launcherPrefs.contains(PENDING_CONTROL);
        touchMode = controlChanged ? launcherPrefs.getInt(PENDING_CONTROL, readTouchMode()) : readTouchMode();
        controls = new RadioGroup(this); controls.setOrientation(RadioGroup.HORIZONTAL); controls.setGravity(Gravity.CENTER);
        RadioButton controller = new RadioButton(this); controller.setId(View.generateViewId()); controller.setText(es ? "Mando" : "Controller");
        RadioButton touch = new RadioButton(this); touch.setId(View.generateViewId()); touch.setText(es ? "Táctil" : "Touch");
        ColorStateList tint = ColorStateList.valueOf(Color.rgb(238, 231, 244));
        controller.setTextColor(tint); controller.setButtonTintList(tint); touch.setTextColor(tint); touch.setButtonTintList(tint);
        controller.setTextSize(13); touch.setTextSize(13); styleControlOption(controller); styleControlOption(touch);
        controller.setPadding(dp(5), 0, dp(5), 0); touch.setPadding(dp(5), 0, dp(5), 0);
        controls.addView(controller); controls.addView(touch); controls.check(touchMode == 1 ? touch.getId() : controller.getId());
        controls.setOnCheckedChangeListener((group, checked) -> {
            touchMode = checked == touch.getId() ? 1 : 0;
            controlChanged = true;
            synchronized (CONTROL_LOCK) {
                if (!launcherPrefs.edit().putInt(PENDING_CONTROL, touchMode).commit())
                    message.setText(es ? "No se pudo guardar la selección de controles." : "Could not save the control selection.");
            }
        });
        choose = new Button(this); choose.setText(es ? "Seleccionar datos" : "Choose data");
        choose.setOnClickListener(v -> openPicker()); choose.setEnabled(false);
        styleButton(choose, false);
        if (embeddedData()) choose.setVisibility(View.GONE);
        chooseSteam = new Button(this); chooseSteam.setText(LauncherDiagnosticIntegration.available(this)
            ? (es ? "Importar juego de Steam" : "Import Steam game")
            : (es ? "Textos y texturas del ZIP" : "Text + textures from ZIP"));
        chooseSteam.setOnClickListener(v -> openSteamPicker()); chooseSteam.setEnabled(false);
        styleButton(chooseSteam, LauncherDiagnosticIntegration.available(this)); dataCard.addView(chooseSteam, buttonParams());
        dataCard.addView(choose, buttonParams());
        LinearLayout inputCard = card(); LinearLayout.LayoutParams inputParams = new LinearLayout.LayoutParams(-1, -2); inputParams.topMargin = dp(7); left.addView(inputCard, inputParams);
        inputCard.addView(cardTitle(es ? "CONTROLES" : "CONTROLS")); inputCard.addView(controls);
        chooseProfile = new Button(this); chooseProfile.setText(es ? "Importar perfil ZIP" : "Import profile ZIP");
        chooseProfile.setEnabled(false); chooseProfile.setOnClickListener(v -> confirmProfilePicker());
        styleButton(chooseProfile, false); inputCard.addView(chooseProfile, buttonParams());
        exportCurrentProfile = new Button(this); exportCurrentProfile.setText(es ? "Exportar progreso" : "Export progress");
        exportCurrentProfile.setEnabled(false); exportCurrentProfile.setOnClickListener(v -> chooseCurrentProfile());
        styleButton(exportCurrentProfile, false); inputCard.addView(exportCurrentProfile, buttonParams());
        LinearLayout modCard = card(); right.addView(modCard);
        modCard.addView(cardTitle("MODS  ✦"));
        modHint = new TextView(this); modHint.setText(es ? "Importá y elegí tus mods Lua" : "Import and choose your Lua mods"); modHint.setTextColor(Color.rgb(225, 205, 235)); modHint.setTextSize(12); modHint.setPadding(0, dp(3), 0, dp(5)); modCard.addView(modHint);
        importModButton = new Button(this); importModButton.setText(es ? "＋ Importar mod" : "＋ Import mod"); importModButton.setEnabled(false); importModButton.setOnClickListener(v -> openModPicker()); styleButton(importModButton, true); modCard.addView(importModButton, buttonParams());
        mods = new Button(this); mods.setText(es ? "Gestionar mods" : "Manage mods");
        mods.setEnabled(false); mods.setOnClickListener(v -> showMods());
        styleButton(mods, false); modCard.addView(mods, buttonParams());
        play = new Button(this); play.setText(es ? "▶   JUGAR" : "▶   PLAY"); play.setEnabled(false);
        play.setOnClickListener(v -> launchGame());
        stylePlay(play); LinearLayout.LayoutParams playParams = new LinearLayout.LayoutParams(-1, dp(58)); playParams.topMargin = dp(9); right.addView(play, playParams);
        ScrollView scroll = new ScrollView(this); scroll.setFillViewport(true); scroll.addView(layout); setContentView(scroll);
        checkData();
    }

    private void checkData() {
        if (IMPORT_GATE.isBusy()) {
            setImportBusy();
            message.setText(es ? "Importación en curso…" : "Import in progress…");
            watchImportProgress(IMPORT_GATE.generation());
        }
        new Thread(() -> {
            long generation = IMPORT_GATE.generation();
            try { IMPORT_GATE.awaitIdle(); }
            catch (InterruptedException e) { Thread.currentThread().interrupt(); return; }
            File existing = target();
            boolean available;
            boolean profileRecoveryFailed = false;
            boolean recoveryFailed = false;
            synchronized (DATA_LOCK) {
                boolean recoveryOk = true;
                try {
                    if (!embeddedData() && existing != null) {
                        LauncherDataBundle.recoverTransaction(existing, pendingSteam());
                        SplitFileRecovery.recover(existing, this::valid);
                    }
                    File modRoot = getExternalFilesDir(null);
                    if (modRoot == null) throw new IOException("mod storage unavailable");
                    if (LauncherModRecovery.recover(new File(modRoot, "Mods")) > 0) IMPORT_GATE.noteModRecovery();
                    recoverSteamZip();
                    migrateCachedProfile();
                    File profile = pendingProfile();
                    if (profile.exists() && !plausibleProfileZip(profile)) {
                        // Preserve an invalid active entry before attempting to
                        // restore the previous one; never overwrite either.
                        preserveDamagedProfile(profile);
                        IMPORT_GATE.noteProfileRecovery();
                    }
                    try { SplitFileRecovery.recover(pendingProfile(), this::plausibleProfileZip); }
                    catch (IOException e) {
                        // With no queued profile, a damaged backup cannot be
                        // applied by the game and must not block normal play.
                        // Other ambiguous states still require recovery first.
                        if (pendingProfile().exists()) throw e;
                        File backup = SplitFileRecovery.backupFor(pendingProfile());
                        if (backup.exists() && !plausibleProfileZip(backup)) {
                            try { preserveDamagedProfile(backup); IMPORT_GATE.noteProfileRecovery(); }
                            catch (IOException preserveError) { android.util.Log.w("MegabonkLauncher", "Profile preservation: " + preserveError.getMessage()); }
                        }
                        profileRecoveryFailed = true;
                        android.util.Log.w("MegabonkLauncher", "Profile recovery: " + e.getMessage());
                    }
                    try { LauncherDataBundle.cleanupPartials(existing, getCacheDir(), pendingSteam()); }
                    catch (IOException e) { android.util.Log.w("MegabonkLauncher", "Temporary data cleanup: " + e.getMessage()); }
                } catch (Exception e) { recoveryOk = false; recoveryFailed = true; android.util.Log.w("MegabonkLauncher", "Data recovery: " + e.getMessage()); }
                available = recoveryOk && (embeddedData() || (existing != null && valid(existing)));
            }
            final boolean profileNeedsAttention = profileRecoveryFailed;
            final boolean recoveryNeedsAttention = recoveryFailed;
            runOnUiThread(() -> {
                if (isFinishing() || isDestroyed()) return;
                // A picker can return after the check started. Its reservation
                // wins over this older result, even before its worker runs.
                if (IMPORT_GATE.isBusy() || IMPORT_GATE.generation() != generation) checkData();
                else {
                    setReady(available);
                    showSteamImportFailure();
                    showProfileImportFailure();
                    showModImportResult();
                    showModRecoveryNotice();
                    if (recoveryNeedsAttention) message.setText(es
                            ? "No se pudo completar la recuperación de datos, perfil o mods. Cerrá y volvé a abrir OpenBonk; no borres los archivos."
                            : "Could not complete data, profile or mod recovery. Close and reopen OpenBonk; do not delete the files.");
                    else if (IMPORT_GATE.hasProfileRecoveryNotice()) message.setText(pendingProfile().isFile()
                            ? (es ? "Se apartó un perfil que no se pudo validar. Revisá el perfil preparado antes de jugar." : "A profile that could not be validated was set aside. Check the queued profile before playing.")
                            : (es ? "Se apartó un perfil que no se pudo validar. Podés volver a importar tu perfil." : "A profile that could not be validated was set aside. You can import your profile again."));
                    else if (profileNeedsAttention) message.setText(es
                            ? "No se pudo recuperar un perfil anterior. Se conservaron los archivos; podés seguir jugando."
                            : "Could not recover an earlier profile. The files were kept; you can still play.");
                }
            });
        }, "megabonk-data-check").start();
    }

    private void runImport(Runnable work, String name) {
        try {
            new Thread(() -> {
                try { work.run(); }
                finally { IMPORT_GATE.finish(); }
            }, name).start();
        } catch (RuntimeException | Error e) {
            IMPORT_GATE.finish();
            throw e;
        }
    }

    private int dp(float value) { return (int)(value * getResources().getDisplayMetrics().density + 0.5f); }
    private GradientDrawable shape(int color, int stroke) {
        GradientDrawable result = new GradientDrawable(); result.setColor(color); result.setCornerRadius(dp(12)); result.setStroke(dp(2), stroke); return result;
    }
    private LinearLayout card() {
        LinearLayout result = new LinearLayout(this); result.setOrientation(LinearLayout.VERTICAL); result.setPadding(dp(10), dp(8), dp(10), dp(8));
        result.setBackground(shape(Color.rgb(57, 21, 73), Color.rgb(126, 52, 147))); return result;
    }
    private TextView cardTitle(String text) {
        TextView result = new TextView(this); result.setText(text); result.setTextColor(Color.rgb(255, 247, 234)); result.setTypeface(Typeface.DEFAULT, Typeface.BOLD); result.setTextSize(16); return result;
    }
    private LinearLayout.LayoutParams buttonParams() { LinearLayout.LayoutParams result = new LinearLayout.LayoutParams(-1, dp(37)); result.topMargin = dp(4); return result; }
    private ColorStateList buttonText(int active) {
        return new ColorStateList(new int[][]{new int[]{-android.R.attr.state_enabled}, new int[]{}},
            new int[]{Color.rgb(162, 143, 166), active});
    }
    private void styleButton(Button button, boolean accent) {
        int bg = accent ? Color.rgb(190, 245, 35) : Color.rgb(91, 38, 110);
        button.setTextColor(buttonText(accent ? Color.rgb(31, 8, 43) : Color.WHITE)); button.setTextSize(12); button.setTypeface(Typeface.DEFAULT, Typeface.BOLD); button.setAllCaps(false); button.setMinHeight(0); button.setMinimumHeight(0); button.setPadding(dp(5), 0, dp(5), 0);
        StateListDrawable states = new StateListDrawable(); states.addState(new int[]{-android.R.attr.state_enabled}, shape(Color.rgb(58, 43, 63), Color.rgb(96, 76, 101))); states.addState(new int[]{android.R.attr.state_focused}, shape(accent ? Color.rgb(255, 249, 205) : bg, Color.rgb(205, 255, 42))); states.addState(new int[]{}, shape(bg, accent ? Color.rgb(220, 255, 96) : Color.rgb(138, 66, 154))); button.setBackground(states);
    }
    private void stylePlay(Button button) {
        styleButton(button, true); button.setTextSize(22); button.setTextColor(buttonText(Color.rgb(42, 9, 48)));
        StateListDrawable states = new StateListDrawable(); states.addState(new int[]{-android.R.attr.state_enabled}, shape(Color.rgb(58, 43, 63), Color.rgb(96, 76, 101))); states.addState(new int[]{android.R.attr.state_focused}, shape(Color.rgb(255, 169, 20), Color.rgb(219, 255, 50))); states.addState(new int[]{}, shape(Color.rgb(255, 169, 20), Color.rgb(255, 217, 105))); button.setBackground(states);
    }

    private void styleControlOption(RadioButton option) {
        StateListDrawable states = new StateListDrawable();
        states.addState(new int[]{android.R.attr.state_focused}, shape(Color.rgb(75, 28, 92), Color.rgb(205, 255, 42)));
        states.addState(new int[]{}, new android.graphics.drawable.ColorDrawable(Color.TRANSPARENT));
        option.setBackground(states);
    }

    @Override protected void onSaveInstanceState(Bundle state) {
        state.putInt("touchMode", touchMode); state.putBoolean("controlChanged", controlChanged);
        state.putString("exportProfileId", exportProfileId);
        state.putString("exportCurrentAccount", exportCurrentAccount);
        super.onSaveInstanceState(state);
    }

    private int readTouchMode() { return readControlSetting("touch_controls") != 0 ? 1 : 0; }

    private int readControlSetting(String key) {
        Integer value = readControlSetting(this, key);
        return value == null ? 0 : value;
    }

    private static Integer readControlSetting(Activity activity, String key) {
        try {
            File root = activity.getExternalFilesDir(null);
            if (root == null) return null;
            File config = new File(root, "Saves/LocalDir/config.json");
            if (!config.isFile() || config.length() > 262144) return null;
            try (InputStream in = new FileInputStream(config); ByteArrayOutputStream bytes = new ByteArrayOutputStream()) {
                byte[] buffer = new byte[4096]; int read;
                while ((read = in.read(buffer)) != -1) { if (bytes.size() + read > 262144) return null; bytes.write(buffer, 0, read); }
                return new JSONObject(new String(bytes.toByteArray(), StandardCharsets.UTF_8)).getJSONObject("cfControlSettings").getInt(key);
            }
        } catch (Exception ignored) { return null; }
    }

    // Unity calls this after SaveConfig. Keep the choice if saving failed or a
    // newer choice arrived, so a subsequent launch can retry it.
    public static boolean acknowledgeControlChoice(int choice) {
        if (choice != 0 && choice != 1) return false;
        Activity activity = com.unity3d.player.UnityPlayer.currentActivity;
        if (activity == null) return false;
        synchronized (CONTROL_LOCK) {
            SharedPreferences prefs = activity.getSharedPreferences(LAUNCHER_PREFS, MODE_PRIVATE);
            Integer saved = readControlSetting(activity, "touch_controls");
            if (!prefs.contains(PENDING_CONTROL) || prefs.getInt(PENDING_CONTROL, -1) != choice || saved == null || saved != choice) return false;
            return prefs.edit().remove(PENDING_CONTROL).commit();
        }
    }

    private KeyEvent swappedKey(KeyEvent event) {
        if (!swapMenuButtons) return null;
        int code = event.getKeyCode();
        if (code != KeyEvent.KEYCODE_BUTTON_A && code != KeyEvent.KEYCODE_BUTTON_B) return null;
        return new KeyEvent(event.getDownTime(), event.getEventTime(), event.getAction(),
            code == KeyEvent.KEYCODE_BUTTON_B ? KeyEvent.KEYCODE_DPAD_CENTER : KeyEvent.KEYCODE_BACK,
            event.getRepeatCount(), event.getMetaState(), event.getDeviceId(), event.getScanCode(), event.getFlags(), event.getSource());
    }

    @Override public boolean dispatchKeyEvent(KeyEvent event) {
        KeyEvent mapped = swappedKey(event);
        return super.dispatchKeyEvent(mapped == null ? event : mapped);
    }

    private void showDialog(AlertDialog dialog) {
        dialog.setOnKeyListener((d, code, event) -> {
            KeyEvent mapped = swappedKey(event);
            return mapped != null && dialog.dispatchKeyEvent(mapped);
        });
        dialog.show();
    }

    private static final String NOTICE_DIR = "openbonk-diagnostic/notices";
    private boolean hasTextureNotices() {
        try { String[] files = getAssets().list(NOTICE_DIR); return files != null && files.length > 0; }
        catch (IOException e) { return false; }
    }

    private void showTextureNotices() {
        try {
            String[] files = getAssets().list(NOTICE_DIR);
            if (files == null || files.length == 0) throw new IOException("No notices found");
            Arrays.sort(files);
            showDialog(new AlertDialog.Builder(this).setTitle(es ? "Avisos de terceros" : "Third-party notices")
                .setItems(files, (dialog, which) -> showTextureNotice(files[which]))
                .setNegativeButton(es ? "Cerrar" : "Close", null).create());
        } catch (IOException e) {
            showDialog(new AlertDialog.Builder(this).setMessage(es ? "No se pudieron leer los avisos." : "Could not read the notices.")
                .setPositiveButton("OK", null).create());
        }
    }

    private void showTextureNotice(String name) {
        try (InputStream in = getAssets().open(NOTICE_DIR + "/" + name); ByteArrayOutputStream bytes = new ByteArrayOutputStream()) {
            byte[] buffer = new byte[4096]; int read;
            while ((read = in.read(buffer)) != -1) {
                if (bytes.size() + read > 128 * 1024) throw new IOException("Notice too large");
                bytes.write(buffer, 0, read);
            }
            showDialog(new AlertDialog.Builder(this).setTitle(name).setMessage(new String(bytes.toByteArray(), StandardCharsets.UTF_8))
                .setNegativeButton(es ? "Volver" : "Back", (d, w) -> showTextureNotices()).create());
        } catch (IOException e) {
            showDialog(new AlertDialog.Builder(this).setMessage(es ? "No se pudo leer el aviso." : "Could not read the notice.")
                .setPositiveButton("OK", (d, w) -> showTextureNotices()).create());
        }
    }

    private void showMods() {
        try {
            File root = getExternalFilesDir(null);
            if (root == null) throw new Exception("storage unavailable");
            File modsRoot = new File(root, "Mods");
            if (LauncherModRecovery.recover(modsRoot) > 0) { IMPORT_GATE.noteModRecovery(); showModRecoveryNotice(); }
            File[] folders = modsRoot.listFiles(File::isDirectory);
            if (folders == null) folders = new File[0];
            Arrays.sort(folders, Comparator.comparing(File::getName));
            ArrayList<String> ids = new ArrayList<>();
            ArrayList<Boolean> defaults = new ArrayList<>();
            ArrayList<Boolean> usesLegacyPreference = new ArrayList<>();
            for (int i = 0; i < folders.length; i++) {
                File file = new File(folders[i], "manifest.json");
                if (!file.isFile() || file.length() > 65536) continue;
                try (InputStream in = new FileInputStream(file); ByteArrayOutputStream bytes = new ByteArrayOutputStream()) {
                    byte[] buffer = new byte[4096]; int read;
                    while ((read = in.read(buffer)) != -1) {
                        if (bytes.size() + read > 65536) throw new Exception("manifest too large");
                        bytes.write(buffer, 0, read);
                    }
                    JSONObject manifest = new JSONObject(new String(bytes.toByteArray(), StandardCharsets.UTF_8));
                    String id = manifest.optString("id", "");
                    if (!id.equals(folders[i].getName()) || !(validModHooks(manifest) || (manifest.optInt("api") == 3 && "content".equals(manifest.optString("kind"))))) continue;
                    boolean content = manifest.optInt("api") == 3 && "content".equals(manifest.optString("kind"));
                    ids.add(id); defaults.add(!content && manifest.optBoolean("enabled", false)); usesLegacyPreference.add(!content);
                } catch (Exception ignored) { }
            }
            if (ids.isEmpty()) {
                showDialog(new AlertDialog.Builder(this).setTitle("Mods").setMessage(es ? "No hay mods disponibles." : "No mods available.").setNegativeButton(es ? "Cerrar" : "Close", null).setPositiveButton(es ? "Importar / restaurar" : "Import / restore", (d, w) -> showModActions(modsRoot)).create());
                return;
            }
            SharedPreferences prefs = getSharedPreferences("megabonk_mod_state", MODE_PRIVATE);
            // Legacy Unity choices are migrated when the game reads them; the
            // launcher also reads the old Android preference encoding on upgrade.
            SharedPreferences legacy = getSharedPreferences(getPackageName() + ".v2.playerprefs", MODE_PRIVATE);
            boolean[] selected = new boolean[ids.size()];
            boolean[] original = new boolean[ids.size()];
            for (int i = 0; i < ids.size(); i++) {
                String oldKey = "megabonk.lua.enabled." + android.util.Base64.encodeToString(ids.get(i).getBytes(StandardCharsets.UTF_8), android.util.Base64.NO_WRAP);
                oldKey = android.net.Uri.encode(oldKey);
                boolean initial = usesLegacyPreference.get(i) && legacy.contains(oldKey) ? legacy.getInt(oldKey, 0) != 0 : defaults.get(i);
                selected[i] = original[i] = prefs.getBoolean(ids.get(i), initial);
            }
            AlertDialog modDialog = new AlertDialog.Builder(this).setTitle("Mods")
                .setMultiChoiceItems(ids.toArray(new String[0]), selected, (dialog, which, checked) -> selected[which] = checked)
                .setNeutralButton(es ? "Importar / restaurar" : "Import / restore", (dialog, which) -> showModActions(modsRoot))
                .setNegativeButton(es ? "Cancelar" : "Cancel", null)
                .setPositiveButton(es ? "Guardar" : "Save", (dialog, which) -> {
                    SharedPreferences.Editor editor = prefs.edit(); boolean changed = false;
                    for (int i = 0; i < ids.size(); i++) if (selected[i] != original[i]) { editor.putBoolean(ids.get(i), selected[i]); changed = true; }
                    if (changed) {
                        if (!editor.commit()) showDialog(new AlertDialog.Builder(this).setMessage(es ? "No se pudieron guardar los mods." : "Could not save mod choices.").setPositiveButton("OK", null).create());
                        else {
                            IMPORT_GATE.clearModResult();
                            modHint.setText(es ? "Selección de mods guardada." : "Mod choices saved.");
                        }
                    }
                }).create();
            showDialog(modDialog);
        } catch (Exception e) {
            showDialog(new AlertDialog.Builder(this).setMessage((es ? "No se pudieron leer los mods: " : "Could not read mods: ") + e.getMessage()).setPositiveButton("OK", null).create());
        }
    }

    private void showModActions(File modsRoot) {
        ArrayList<String> available;
        try { available = LauncherModRecovery.previousIds(modsRoot); }
        catch (IOException e) {
            showDialog(new AlertDialog.Builder(this).setTitle("Mods").setMessage(es ? "No se pudo leer el historial de mods." : "Could not read mod history.").setPositiveButton("OK", null).create());
            return;
        }
        if (available.isEmpty()) { openModPicker(); return; }
        String[] options = { es ? "Importar ZIP" : "Import ZIP", es ? "Restaurar versión anterior" : "Restore previous version" };
        showDialog(new AlertDialog.Builder(this).setTitle("Mods").setItems(options, (dialog, which) -> {
            if (which == 0) { openModPicker(); return; }
            showDialog(new AlertDialog.Builder(this).setTitle(es ? "Restaurar versión anterior" : "Restore previous version")
                .setItems(available.toArray(new String[0]), (picker, index) -> {
                    String id = available.get(index);
                    restoreMod(modsRoot, id);
                }).setNegativeButton(es ? "Cancelar" : "Cancel", null).create());
        }).setNegativeButton(es ? "Cancelar" : "Cancel", null).create());
    }

    static boolean validModHooks(JSONObject manifest) {
        int api = manifest.optInt("api", 0);
        if (manifest.has("refresh") && !manifest.isNull("refresh") &&
            (!(manifest.opt("refresh") instanceof String) || api != 2 || !"level_up".equals(manifest.optString("refresh")))) return false;
        if (api == 1) return hookStat(manifest.optString("hook", "")) != null;
        JSONArray hooks = manifest.optJSONArray("hooks");
        if (api != 2 || hooks == null || hooks.length() < 1 || hooks.length() > 112) return false;
        HashSet<String> seen = new HashSet<>();
        for (int i = 0; i < hooks.length(); i++) {
            String hook = hooks.optString(i, "");
            String stat = hookStat(hook);
            if (stat == null || !seen.add(stat + (hook.startsWith("stat_add:") ? ":add" : ":mul"))) return false;
        }
        return true;
    }

    private static final HashSet<String> LUA_STATS = new HashSet<>(Arrays.asList(
        "MaxHealth", "HealthRegen", "Shield", "Thorns", "Armor", "Evasion", "Evolve", "DamageReductionMultiplier", "DamageCooldownMultiplier",
        "SizeMultiplier", "DurationMultiplier", "ProjectileSpeedMultiplier", "DamageMultiplier", "EffectDurationMultiplier", "AttackSpeed",
        "Projectiles", "Lifesteal", "CritChance", "CritDamage", "FireDamage", "IceDamage", "LightningDamage", "EliteDamageMultiplier",
        "KnockbackMultiplier", "MoveSpeedMultiplier", "JumpHeight", "FallDamageReduction", "Slam", "PickupRange", "Luck", "GoldIncreaseMultiplier",
        "XpIncreaseMultiplier", "ChestIncreaseMultiplier", "ChestPriceMultiplier", "ShopPriceReduction", "Holiness", "Wickedness", "Difficulty",
        "EliteSpawnIncrease", "PowerupBoostMultiplier", "PowerupChance", "BurnChance", "FreezeChance", "WeaponBurstCooldown", "ProjectileBounces",
        "ExtraJumps", "Overheal", "HealingMultiplier", "SilverIncreaseMultiplier", "EnemyAmountMultiplier", "EnemySizeMultiplier", "EnemySpeedMultiplier",
        "EnemyHpMultiplier", "EnemyDamageMultiplier", "EnemyScalingMultiplier", "PoisonDamageMultiplier"));

    private static String hookStat(String hook) {
        switch (hook) {
            case "move_speed_multiplier": return "MoveSpeedMultiplier";
            case "damage_multiplier": return "DamageMultiplier";
            case "attack_speed_multiplier": return "AttackSpeed";
            case "xp_multiplier": return "XpIncreaseMultiplier";
            case "gold_multiplier": return "GoldIncreaseMultiplier";
            case "pickup_range_multiplier": return "PickupRange";
            default:
                if (hook.startsWith("stat_add:") || hook.startsWith("stat_mul:")) {
                    String stat = hook.substring(9);
                    return LUA_STATS.contains(stat) ? stat : null;
                }
                return null;
        }
    }

    private void setReady(boolean ready) {
        getWindow().clearFlags(WindowManager.LayoutParams.FLAG_KEEP_SCREEN_ON);
        dataReady = ready;
        if (!ready) {
            if (pendingSteam().isFile() && LauncherDiagnosticIntegration.available(this)) message.setText(es
                ? "No se encontraron los datos preparados. Volvé a importar tu ZIP de Steam."
                : "Prepared game data was not found. Import your Steam ZIP again.");
            else if (pendingSteam().isFile()) message.setText(es
                ? "ZIP de Steam preparado. Falta seleccionar OpenBonk-data.zip preparado en la PC o el OBB de esta versión."
                : "Steam ZIP queued. Still needed: OpenBonk-data.zip prepared on a PC or this build's OBB.");
            else if (LauncherDiagnosticIntegration.available(this)) message.setText(es
                ? "Seleccioná el ZIP de tu copia de Steam para preparar los datos del juego."
                : "Choose the ZIP of your Steam copy to prepare the game data.");
            else message.setText(es ? "Seleccioná OpenBonk-data.zip preparado en la PC o el archivo Android (.obb) de esta versión." : "Choose OpenBonk-data.zip prepared on a PC or this build's Android data file (.obb).");
        }
        else {
            String status = embeddedData() ? (es ? "Datos incluidos" : "Data included") : (es ? "Datos listos" : "Data ready");
            if (pendingSteam().isFile()) status += es ? " · textos y texturas preparados" : " · text and textures queued";
            if (pendingProfile().isFile()) status += es ? " · perfil preparado" : " · profile queued";
            message.setText(status);
        }
        if (!ready && pendingProfile().isFile()) message.append(es
            ? "\nPerfil preparado; se aplicará al iniciar el juego."
            : "\nProfile queued; it will be applied when the game starts.");
        choose.setText(LauncherDiagnosticIntegration.available(this)
            ? (es ? "Importar datos Android preparados" : "Import prepared Android data")
            : (ready ? (es ? "Cambiar datos" : "Change data") : (es ? "Seleccionar datos" : "Choose data")));
        choose.setEnabled(true); chooseSteam.setEnabled(true); chooseProfile.setEnabled(true); exportCurrentProfile.setEnabled(true); play.setEnabled(ready);
        mods.setEnabled(true); importModButton.setEnabled(true);
        (ready ? play : LauncherDiagnosticIntegration.available(this) ? chooseSteam : choose).requestFocus();
    }

    private void setImportBusy() {
        getWindow().addFlags(WindowManager.LayoutParams.FLAG_KEEP_SCREEN_ON);
        choose.setEnabled(false); chooseSteam.setEnabled(false); chooseProfile.setEnabled(false); exportCurrentProfile.setEnabled(false);
        play.setEnabled(false); mods.setEnabled(false); importModButton.setEnabled(false);
    }

    private void showSteamImportFailure() {
        String failure = IMPORT_GATE.steamFailure();
        if (failure == null) return;
        message.setText((es ? "No se pudo preparar el ZIP: " : "Could not prepare ZIP: ") + failure);
        chooseSteam.requestFocus();
    }

    private void showProfileImportFailure() {
        String failure = IMPORT_GATE.profileFailure();
        if (failure == null) return;
        message.setText((es ? "No se pudo preparar el perfil: " : "Could not prepare profile: ") + failure);
        chooseProfile.requestFocus();
    }

    private void showModImportResult() {
        String failure = IMPORT_GATE.modFailure(), installed = IMPORT_GATE.modImported();
        boolean restoration = IMPORT_GATE.modRestoration();
        if (failure != null) {
            modHint.setText((restoration ? (es ? "No se pudo restaurar el mod: " : "Could not restore mod: ")
                    : (es ? "No se pudo importar el mod: " : "Could not import mod: ")) + failure);
            importModButton.requestFocus();
        } else if (installed != null) {
            modHint.setText((restoration ? (es ? "Versión anterior restaurada y desactivada: " : "Previous version restored and disabled: ")
                    : (es ? "Mod importado o actualizado y desactivado: " : "Mod imported or updated and disabled: ")) + installed);
            mods.requestFocus();
        }
    }

    private void showModRecoveryNotice() {
        if (!IMPORT_GATE.hasModRecoveryNotice()) return;
        modHint.setText(es ? "Se recuperaron o apartaron mods dañados. Revisá tus mods."
                           : "Damaged mods were recovered or set aside. Check your mods.");
    }

    private String progressText(String stage) {
        if (stage == null) return es ? "Importación en curso…" : "Import in progress…";
        if (es) return stage + "…";
        switch (stage) {
            case "Verificando archivos del ZIP": return "Checking ZIP files…";
            case "Preparando recursos originales": return "Preparing original assets…";
            case "Preparando mapas y audio": return "Preparing maps and audio…";
            case "Preparando recursos principales": return "Preparing core assets…";
            case "Preparando texturas y efectos": return "Preparing textures and effects…";
            case "Preparando idiomas": return "Preparing languages…";
            case "Preparando animaciones": return "Preparing animations…";
            case "Preparando shaders": return "Preparing shaders…";
            case "Preparando objetos y efectos": return "Preparing objects and effects…";
            case "Adaptando datos para el runtime": return "Adapting data for the runtime…";
            case "Preparando video": return "Preparing video…";
            case "Verificando datos": return "Verifying game data…";
            default: return "Preparing game data…";
        }
    }

    private void watchImportProgress(long generation) {
        message.postDelayed(new Runnable() {
            @Override public void run() {
                if (isFinishing() || isDestroyed() || !IMPORT_GATE.isBusy() || IMPORT_GATE.generation() != generation) return;
                message.setText(progressText(IMPORT_GATE.stage()));
                message.postDelayed(this, 500);
            }
        }, 500);
    }

    private File target() {
        try {
            File dir = getObbDir();
            if (dir == null) return null;
            long version = getPackageManager().getPackageInfo(getPackageName(), 0).versionCode;
            return new File(dir, "main." + version + "." + getPackageName() + ".obb");
        } catch (Exception e) { return null; }
    }

    private boolean valid(File file) {
        if (!file.isFile() || file.length() <= 0 || file.length() > MAX_BYTES) return false;
        try (ZipFile zip = new ZipFile(file)) {
            if (!(zip.getEntry("assets/bin/Data/level1.split0") != null &&
                   zip.getEntry("assets/bin/Data/level2.split0") != null &&
                   zip.getEntry("assets/aa/Android/localization-assets-shared_assets_all.bundle") != null)) return false;
            MessageDigest digest = MessageDigest.getInstance("SHA-256");
            try (InputStream in = new FileInputStream(file)) {
                byte[] buffer = new byte[65536]; int count;
                while ((count = in.read(buffer)) != -1) digest.update(buffer, 0, count);
            }
            StringBuilder result = new StringBuilder(64);
            for (byte b : digest.digest()) result.append(String.format(Locale.ROOT, "%02x", b & 0xff));
            String actual = result.toString();
            return actual.equals(SplitExpectedData.SHA256) || actual.equals(SplitExpectedData.LEGACY_SHA256) || actual.equals(SplitExpectedData.SECOND_LEGACY_SHA256) || actual.equals(SplitExpectedData.THIRD_LEGACY_SHA256) || actual.equals(SplitExpectedData.FOURTH_LEGACY_SHA256) || actual.equals(SplitExpectedData.FIFTH_LEGACY_SHA256) || actual.equals(SplitExpectedData.SIXTH_LEGACY_SHA256);
        } catch (Exception e) { return false; }
    }

    private void openPicker() {
        try {
            Intent pick = new Intent(Intent.ACTION_OPEN_DOCUMENT);
            pick.addCategory(Intent.CATEGORY_OPENABLE); pick.setType("*/*");
            pick.addFlags(Intent.FLAG_GRANT_READ_URI_PERMISSION);
            startActivityForResult(pick, PICK);
        } catch (Exception e) { message.setText(es ? "No se pudo abrir el selector de archivos." : "Could not open the file picker."); }
    }

    private File pendingSteam() { return new File(getCacheDir(), "megabonk-original-texts.pending.zip"); }
    private void recoverSteamZip() throws java.io.IOException {
        File destination = pendingSteam();
        SplitFileRecovery.recover(destination, this::plausibleSteamZip);
        // Older launchers used a different backup name for this ZIP.
        SplitFileRecovery.recover(destination, new File(getCacheDir(), "megabonk-original-texts.previous"), this::plausibleSteamZip);
    }

    private void openSteamPicker() {
        try {
            Intent pick = new Intent(Intent.ACTION_OPEN_DOCUMENT);
            pick.addCategory(Intent.CATEGORY_OPENABLE); pick.setType("*/*");
            pick.addFlags(Intent.FLAG_GRANT_READ_URI_PERMISSION);
            startActivityForResult(pick, PICK_STEAM);
        } catch (Exception e) { message.setText(es ? "No se pudo abrir el selector de archivos." : "Could not open the file picker."); }
    }

    private void showGameDataHelp() {
        String instructions = embeddedData() ? (es
            ? "Esta compilación ya incluye datos del juego. Podés seleccionar un ZIP de Steam compatible para importar textos y texturas adicionales."
            : "This build already includes game data. You can select a compatible Steam ZIP to import additional text and textures.") : LauncherDiagnosticIntegration.available(this) ? (es
            ? "En tu PC, abrí la carpeta de Megabonk desde Steam y comprimí la carpeta Megabonk_Data en un archivo ZIP. Copiá el ZIP a este dispositivo y elegilo en «Importar juego de Steam».\n\nSe necesita una copia y versión compatibles. La preparación puede tardar y necesita espacio libre adicional. El ZIP contiene archivos del juego; mantenelo para uso personal."
            : "On your PC, open the Megabonk folder from Steam and compress the Megabonk_Data folder into a ZIP file. Copy the ZIP to this device and choose it under ‘Import Steam game’.\n\nA compatible game copy and version are required. Preparation can take time and needs extra free space. The ZIP contains game files; keep it for personal use.") : (es
            ? "Esta versión necesita un paquete Android preparado en la PC. Seleccionalo con «Seleccionar datos». También podés elegir el ZIP de Steam para importar textos y texturas.\n\nSe necesita una copia y versión compatibles; mantené los archivos del juego para uso personal."
            : "This build needs an Android data package prepared on a PC. Select it using ‘Choose data’. You can also choose a Steam ZIP to import text and textures.\n\nA compatible game copy and version are required; keep game files for personal use.");
        showDialog(new AlertDialog.Builder(this).setTitle(es ? "Preparar datos del juego" : "Prepare game data").setMessage(instructions).setPositiveButton("OK", null).create());
    }

    private void openModPicker() {
        try {
            Intent pick = new Intent(Intent.ACTION_OPEN_DOCUMENT);
            // Some document providers report ZIP files as octet-stream or use
            // a provider-specific MIME type. Validate the contents after selection.
            pick.addCategory(Intent.CATEGORY_OPENABLE); pick.setType("*/*");
            pick.addFlags(Intent.FLAG_GRANT_READ_URI_PERMISSION);
            if (android.os.Build.VERSION.SDK_INT >= 26) {
                android.net.Uri downloads = android.provider.DocumentsContract.buildDocumentUri("com.android.externalstorage.documents", "primary:Download");
                pick.putExtra(android.provider.DocumentsContract.EXTRA_INITIAL_URI, downloads);
            }
            startActivityForResult(pick, PICK_MOD);
        } catch (Exception e) { modHint.setText(es ? "No se pudo abrir el selector de archivos." : "Could not open the file picker."); }
    }

    private void importMod(android.net.Uri uri) {
        setImportBusy();
        final long generation = IMPORT_GATE.generation();
        modHint.setText(es ? "Importando mod…" : "Importing mod…");
        runImport(() -> {
            String id = null, error = null;
            synchronized (DATA_LOCK) {
            try (InputStream in = getContentResolver().openInputStream(uri)) {
                if (in == null) throw new Exception("cannot open selected file");
                File root = getExternalFilesDir(null);
                if (root == null) throw new Exception("storage unavailable");
                id = LauncherModImporter.install(in, new File(root, "Mods"), getSharedPreferences("megabonk_mod_state", MODE_PRIVATE));
            } catch (Exception e) { error = LauncherImportError.describe(e, es); }
            IMPORT_GATE.modResult(id, error, false);
            }
            final String installed = id, failure = error;
            runOnUiThread(() -> {
                if (isFinishing() || isDestroyed() || IMPORT_GATE.generation() != generation) return;
                setReady(dataReady);
                showModImportResult();
                showDialog(new AlertDialog.Builder(this).setTitle("Mods").setMessage(failure == null ? (es ? "Importado o actualizado y desactivado: " : "Imported or updated and disabled: ") + installed : (es ? "No se pudo importar: " : "Import failed: ") + failure).setPositiveButton("OK", (d, w) -> showMods()).create());
            });
        }, "openbonk-mod-import");
    }

    private void restoreMod(File modsRoot, String id) {
        if (!IMPORT_GATE.begin()) { modHint.setText(es ? "Ya hay una importación en curso." : "An import is already in progress."); return; }
        setImportBusy();
        final long generation = IMPORT_GATE.generation();
        modHint.setText(es ? "Restaurando mod…" : "Restoring mod…");
        runImport(() -> {
            String error = null;
            synchronized (DATA_LOCK) {
                try { LauncherModRecovery.restorePrevious(modsRoot, id, getSharedPreferences("megabonk_mod_state", MODE_PRIVATE)); }
                catch (Exception e) { error = LauncherImportError.describe(e, es); }
                IMPORT_GATE.modResult(error == null ? id : null, error, true);
            }
            final String failure = error;
            runOnUiThread(() -> {
                if (isFinishing() || isDestroyed() || IMPORT_GATE.generation() != generation) return;
                setReady(dataReady);
                showModImportResult();
                String result = failure == null ? (es ? "Versión anterior restaurada y desactivada: " : "Previous version restored and disabled: ") + id
                                                : (es ? "No se pudo restaurar: " : "Could not restore: ") + failure;
                showDialog(new AlertDialog.Builder(this).setTitle("Mods").setMessage(result).setPositiveButton("OK", (d, w) -> showMods()).create());
            });
        }, "openbonk-mod-restore");
    }

    private boolean plausibleSteamZip(File file) {
        return OriginalGameDataValidator.valid(file);
    }

    // Called under DATA_LOCK. Stage on the destination filesystem so that a
    // valid pending ZIP can be replaced with an atomic rename.
    private void stageSteamZip(InputStream input) throws Exception {
        File staged = new File(getCacheDir(), "megabonk-original-texts.partial");
        try {
            LauncherSteamZipInstaller.install(input, staged, pendingSteam(),
                new File(getCacheDir(), "megabonk-original-texts.previous"),
                this::plausibleSteamZip, MAX_STEAM_BYTES);
        } catch (IOException e) {
            if ("incompatible ZIP".equals(e.getMessage())) throw new IOException(es
                ? "ZIP incompleto, dañado o de una versión no compatible"
                : "ZIP incomplete, damaged or from an unsupported version", e);
            throw e;
        }
    }

    private void prepareSteamZip(android.net.Uri uri) {
        setImportBusy();
        final long generation = IMPORT_GATE.generation();
        boolean diagnostic = LauncherDiagnosticIntegration.available(this);
        message.setText(diagnostic ? (es ? "Preparando datos del juego…" : "Preparing game data…")
                                  : (es ? "Preparando ZIP de textos y texturas…" : "Preparing text and textures ZIP…"));
        if (diagnostic) watchImportProgress(IMPORT_GATE.generation());
        runImport(() -> {
            String error = null;
            synchronized (DATA_LOCK) {
            try (InputStream in = getContentResolver().openInputStream(uri)) {
                if (in == null) throw new Exception("cannot open selected file");
                if (diagnostic) {
                    File destination = target();
                    if (destination == null) throw new IOException("data storage unavailable");
                    LauncherPreparedSteamImport.install(in, getCacheDir(), destination, pendingSteam(),
                        this::plausibleSteamZip, this::valid, MAX_STEAM_BYTES,
                        LauncherDiagnosticIntegration.create(this, IMPORT_GATE::stage), staged -> {
                            OriginalGameDataValidator.Problem problem = OriginalGameDataValidator.problem(staged);
                            if (problem == null) return null;
                            return (problem.missing ? (es ? "Falta el archivo " : "Missing file ") :
                                (es ? "Archivo incompatible o dañado: " : "Unsupported or damaged file: ")) + problem.file;
                        });
                } else stageSteamZip(in);
            } catch (Exception e) {
                error = LauncherImportError.describe(e, es);
                IMPORT_GATE.steamFailure(error);
            }
            }
            final String failure = error;
            runOnUiThread(() -> {
                if (isFinishing() || isDestroyed() || IMPORT_GATE.generation() != generation) return;
                if (failure == null && diagnostic) checkData();
                else if (failure == null) setReady(dataReady);
                else { setReady(dataReady); showSteamImportFailure(); }
            });
        }, "megabonk-steam-zip");
    }

    private File pendingProfile() { return new File(getFilesDir(), "megabonk-profile.pending.zip"); }
    private void migrateCachedProfile() throws IOException {
        File legacy = new File(getCacheDir(), "megabonk-profile.pending.zip");
        File legacyBackup = SplitFileRecovery.backupFor(legacy);
        if (legacy.exists() && !plausibleProfileZip(legacy)) {
            preserveDamagedProfile(legacy);
            IMPORT_GATE.noteProfileRecovery();
        }
        if (legacyBackup.exists() && !plausibleProfileZip(legacyBackup)) {
            preserveDamagedProfile(legacyBackup);
            IMPORT_GATE.noteProfileRecovery();
        }
        SplitFileRecovery.recover(legacy, this::plausibleProfileZip);
        if (!legacy.exists()) return;
        File destination = pendingProfile();
        if (destination.exists() || SplitFileRecovery.backupFor(destination).exists()) {
            // Both may be valid independent requests. Preserve the older cache
            // copy for export instead of overwriting either one.
            preserveDamagedProfile(legacy);
            IMPORT_GATE.noteProfileRecovery();
        } else if (!legacy.renameTo(destination)) {
            throw new IOException("Could not move the queued profile to persistent storage");
        }
    }
    private void preserveDamagedProfile(File backup) throws IOException {
        File root = new File(getFilesDir(), "profile-recovery");
        if (!root.isDirectory() && !root.mkdirs()) throw new IOException("profile recovery directory unavailable");
        File folder = new File(root, java.util.UUID.randomUUID().toString());
        if (!folder.mkdir()) throw new IOException("profile recovery destination unavailable");
        if (!backup.renameTo(new File(folder, "profile.zip"))) {
            folder.delete();
            throw new IOException("could not preserve profile backup");
        }
    }
    private void chooseCurrentProfile() {
        try {
            ArrayList<String> accounts = LauncherProfileExport.accounts(getExternalFilesDir(null));
            if (accounts.isEmpty()) {
                showDialog(new AlertDialog.Builder(this).setMessage(es ? "Todavía no hay un progreso completo para exportar." : "There is no complete progress to export yet.").setPositiveButton("OK", null).create());
                return;
            }
            String[] labels = new String[accounts.size()];
            for (int i = 0; i < accounts.size(); i++) {
                String id = accounts.get(i);
                labels[i] = (es ? "Perfil terminado en " : "Profile ending in ") + id.substring(Math.max(0, id.length() - 4));
            }
            showDialog(new AlertDialog.Builder(this)
                .setTitle(es ? "Exportar progreso" : "Export progress")
                .setMessage(es ? "Cerrá el juego completamente antes de exportar. El ZIP es privado y contiene progreso, no controles ni mods." : "Close the game completely before exporting. The ZIP is private and contains progress, not controls or mods.")
                .setPositiveButton(es ? "Continuar" : "Continue", (dialog, ignored) -> showDialog(new AlertDialog.Builder(this)
                .setTitle(es ? "Elegí un perfil" : "Choose a profile")
                .setItems(labels, (choice, which) -> {
                    exportCurrentAccount = accounts.get(which);
                    try {
                        Intent pick = new Intent(Intent.ACTION_CREATE_DOCUMENT).addCategory(Intent.CATEGORY_OPENABLE).setType("application/zip");
                        pick.putExtra(Intent.EXTRA_TITLE, "OpenBonk-progress.zip");
                        startActivityForResult(pick, EXPORT_CURRENT_PROFILE);
                    } catch (Exception e) { exportCurrentAccount = null; message.setText(es ? "No se pudo abrir el selector de archivos." : "Could not open the file picker."); }
                }).setNegativeButton(es ? "Cancelar" : "Cancel", null).create()))
                .setNegativeButton(es ? "Cancelar" : "Cancel", null).create());
        } catch (IOException e) { message.setText(es ? "No se pudieron leer los perfiles." : "Could not read profiles."); }
    }

    private void exportCurrentProfile(android.net.Uri uri, String account) {
        setImportBusy(); message.setText(es ? "Exportando progreso…" : "Exporting progress…");
        final long generation = IMPORT_GATE.generation();
        runImport(() -> {
            String error = null;
            synchronized (DATA_LOCK) {
                File staged = null;
                try {
                    staged = File.createTempFile("openbonk-profile-export-", ".zip", getCacheDir());
                    LauncherProfileExport.stage(getExternalFilesDir(null), account, staged);
                    try (InputStream in = new FileInputStream(staged); OutputStream out = getContentResolver().openOutputStream(uri, "wt")) {
                        if (out == null) throw new IOException("cannot open destination");
                        byte[] buffer = new byte[65536];
                        for (int n; (n = in.read(buffer)) != -1;) if (n > 0) out.write(buffer, 0, n);
                        out.flush();
                    }
                } catch (Exception e) { error = LauncherImportError.describe(e, es); }
                finally { if (staged != null) staged.delete(); }
            }
            final String failure = error;
            runOnUiThread(() -> {
                if (isFinishing() || isDestroyed() || IMPORT_GATE.generation() != generation) return;
                setReady(dataReady);
                message.setText(failure == null ? (es ? "Progreso exportado. Guardá el ZIP en privado." : "Progress exported. Keep the ZIP private.") :
                    (es ? "No se pudo exportar; el destino puede estar incompleto: " : "Export failed; the destination may be incomplete: ") + failure);
            });
        }, "openbonk-current-profile-export");
    }

    private void confirmProfilePicker() {
        String text = es ? "Importar reemplaza el progreso de esta instalación y conserva una copia anterior. No cambia tus controles. Se aplica al iniciar el juego completamente; si sigue abierto, cerralo y volvé a abrirlo." :
            "Import replaces this installation's progress and keeps a backup. Your controls are preserved. It applies when the game starts fresh; if it is still open, fully close and reopen it.";
        AlertDialog.Builder builder = new AlertDialog.Builder(this).setTitle(es ? "Importar perfil" : "Import profile").setMessage(text)
            .setNegativeButton(es ? "Cancelar" : "Cancel", null).setPositiveButton(es ? "Elegir ZIP" : "Choose ZIP", (dialog, which) -> {
                try { Intent pick = new Intent(Intent.ACTION_OPEN_DOCUMENT); pick.addCategory(Intent.CATEGORY_OPENABLE); pick.setType("*/*"); pick.addFlags(Intent.FLAG_GRANT_READ_URI_PERMISSION); startActivityForResult(pick, PICK_PROFILE); }
                catch (Exception e) { message.setText(es ? "No se pudo abrir el selector de archivos." : "Could not open the file picker."); }
            });
        if (!recoveredProfileFiles().isEmpty()) {
            builder.setMessage(text + (es ? " Los archivos apartados pueden estar dañados; exportarlos no los restaura." : " Set-aside files may be damaged; exporting them does not restore them."));
            builder.setNeutralButton(es ? "Archivos apartados" : "Set-aside files", (d, w) -> showRecoveredProfiles());
        }
        showDialog(builder.create());
    }

    private ArrayList<File> recoveredProfileFiles() {
        ArrayList<File> files = new ArrayList<>();
        File root = new File(getFilesDir(), "profile-recovery");
        File[] folders = root.listFiles();
        if (folders == null) return files;
        for (File folder : folders) {
            try {
                String id = folder.getName();
                if (!java.util.UUID.fromString(id).toString().equals(id)) continue;
                File file = new File(folder, "profile.zip");
                if (!folder.getCanonicalFile().getParentFile().equals(root.getCanonicalFile()) ||
                    !file.getCanonicalFile().getParentFile().equals(folder.getCanonicalFile())) continue;
                if (file.isFile()) files.add(file);
            } catch (Exception ignored) { }
        }
        files.sort((a, b) -> Long.compare(b.lastModified(), a.lastModified()));
        return files;
    }

    private void showRecoveredProfiles() {
        ArrayList<File> files = recoveredProfileFiles();
        if (files.isEmpty()) {
            showDialog(new AlertDialog.Builder(this).setMessage(es ? "No hay archivos de perfil disponibles para exportar." : "No profile files are available to export.").setPositiveButton("OK", null).create());
            return;
        }
        String[] labels = new String[files.size()];
        java.text.DateFormat format = java.text.DateFormat.getDateTimeInstance();
        for (int i = 0; i < files.size(); i++) {
            File file = files.get(i);
            labels[i] = format.format(new java.util.Date(file.lastModified())) + " · " + file.getParentFile().getName().substring(0, 8);
        }
        showDialog(new AlertDialog.Builder(this).setTitle(es ? "Exportar archivo apartado" : "Export set-aside file")
            .setItems(labels, (d, which) -> {
                exportProfileId = files.get(which).getParentFile().getName();
                try {
                    Intent pick = new Intent(Intent.ACTION_CREATE_DOCUMENT).addCategory(Intent.CATEGORY_OPENABLE).setType("application/zip");
                    pick.putExtra(Intent.EXTRA_TITLE, "OpenBonk-profile-recovery-" + exportProfileId.substring(0, 8) + ".zip");
                    startActivityForResult(pick, EXPORT_PROFILE_RECOVERY);
                } catch (Exception e) { exportProfileId = null; message.setText(es ? "No se pudo abrir el selector de archivos." : "Could not open the file picker."); }
            }).setNegativeButton(es ? "Cancelar" : "Cancel", null).create());
    }

    private void exportRecoveredProfile(android.net.Uri uri, String id) {
        setImportBusy();
        message.setText(es ? "Exportando perfil…" : "Exporting profile…");
        final long generation = IMPORT_GATE.generation();
        runImport(() -> {
            String error = null;
            synchronized (DATA_LOCK) {
                try {
                    File source = null;
                    for (File file : recoveredProfileFiles()) if (file.getParentFile().getName().equals(id)) { source = file; break; }
                    if (source == null) throw new IOException("profile file unavailable");
                    try (InputStream in = new FileInputStream(source); OutputStream out = getContentResolver().openOutputStream(uri, "wt")) {
                        if (out == null) throw new IOException("cannot open destination");
                        byte[] buffer = new byte[65536];
                        for (int n; (n = in.read(buffer)) != -1;) if (n > 0) out.write(buffer, 0, n);
                        out.flush();
                    }
                } catch (Exception e) { error = LauncherImportError.describe(e, es); }
            }
            final String failure = error;
            runOnUiThread(() -> {
                if (isFinishing() || isDestroyed() || IMPORT_GATE.generation() != generation) return;
                setReady(dataReady);
                message.setText(failure == null ? (es ? "Archivo exportado. El original apartado se conservó." : "File exported. The set-aside original was kept.") :
                    (es ? "No se pudo exportar; el destino puede estar incompleto: " : "Export failed; the destination may be incomplete: ") + failure);
            });
        }, "openbonk-profile-export");
    }
    private boolean plausibleProfileZip(File file) {
        return LauncherProfileZipValidator.valid(file);
    }
    private void prepareProfileZip(android.net.Uri uri) {
        setImportBusy();
        final long generation = IMPORT_GATE.generation();
        message.setText(es ? "Preparando perfil…" : "Preparing profile…");
        runImport(() -> {
            File destination = pendingProfile(); File staged = new File(getFilesDir(), "megabonk-profile.partial");
            File backup = SplitFileRecovery.backupFor(destination); String error = null;
            synchronized (DATA_LOCK) {
            try {
                if (staged.exists() && !staged.delete()) throw new Exception("temporary file unavailable");
                try (InputStream in = getContentResolver().openInputStream(uri); FileOutputStream out = new FileOutputStream(staged)) {
                    if (in == null) throw new Exception("cannot open selected file");
                    byte[] buffer = new byte[65536]; long total = 0; int n;
                    while ((n = in.read(buffer)) != -1) { total += n; if (total > MAX_PROFILE_BYTES) throw new Exception("ZIP too large"); out.write(buffer, 0, n); }
                    // Do not publish a queued profile until its contents have been flushed.
                    out.getFD().sync();
                }
                if (!plausibleProfileZip(staged)) throw new Exception(es ? "el ZIP de perfil está incompleto, dañado o no es compatible" : "profile ZIP is incomplete, damaged or unsupported");
                SplitFileRecovery.recover(destination, this::plausibleProfileZip);
                if (backup.exists()) throw new Exception("previous ZIP backup exists");
                boolean hadOld = destination.exists();
                if (hadOld && !destination.renameTo(backup)) throw new Exception("cannot preserve previous ZIP");
                if (!staged.renameTo(destination)) { if (hadOld) backup.renameTo(destination); throw new Exception("cannot prepare ZIP"); }
                if (hadOld) backup.delete();
            } catch (Exception e) {
                error = LauncherImportError.describe(e, es);
                IMPORT_GATE.profileFailure(error);
                staged.delete();
            }
            }
            final String failure = error;
            runOnUiThread(() -> {
                if (isFinishing() || isDestroyed() || IMPORT_GATE.generation() != generation) return;
                if (failure == null) setReady(dataReady);
                else { setReady(dataReady); showProfileImportFailure(); }
            });
        }, "megabonk-profile-zip");
    }

    @Override protected void onActivityResult(int request, int result, Intent data) {
        super.onActivityResult(request, result, data);
        if (request == EXPORT_CURRENT_PROFILE) {
            String account = exportCurrentAccount; exportCurrentAccount = null;
            if (result == RESULT_OK && data != null && data.getData() != null && account != null) {
                if (IMPORT_GATE.begin()) exportCurrentProfile(data.getData(), account);
                else message.setText(es ? "Ya hay una importación en curso." : "An import is already in progress.");
            }
            return;
        }
        if (request == EXPORT_PROFILE_RECOVERY) {
            String id = exportProfileId; exportProfileId = null;
            if (result == RESULT_OK && data != null && data.getData() != null && id != null) {
                if (IMPORT_GATE.begin()) exportRecoveredProfile(data.getData(), id);
                else message.setText(es ? "Ya hay una importación en curso." : "An import is already in progress.");
            }
            return;
        }
        if (result != RESULT_OK || data == null || data.getData() == null) return;
        if (request != PICK && request != PICK_STEAM && request != PICK_PROFILE && request != PICK_MOD) return;
        if (!IMPORT_GATE.begin()) {
            message.setText(es ? "Ya hay una importación en curso." : "An import is already in progress.");
            return;
        }
        if (request == PICK_STEAM) { prepareSteamZip(data.getData()); return; }
        if (request == PICK_PROFILE) { prepareProfileZip(data.getData()); return; }
        if (request == PICK_MOD) { importMod(data.getData()); return; }
        setImportBusy();
        message.setText(es ? "Preparando datos…" : "Preparing data…");
        final android.net.Uri uri = data.getData();
        runImport(() -> {
            String error = null; boolean steamOnly = false;
            File destination = target();
            File staged = destination == null ? null : new File(destination.getParentFile(), destination.getName() + ".partial");
            File backup = destination == null ? null : new File(destination.getParentFile(), destination.getName() + ".previous");
            synchronized (DATA_LOCK) {
            try {
                if (destination == null || (!destination.getParentFile().isDirectory() && !destination.getParentFile().mkdirs())) throw new Exception("data directory unavailable");
                if (staged.exists() && !staged.delete()) throw new Exception("cannot clear temporary file");
                try (InputStream in = getContentResolver().openInputStream(uri); FileOutputStream out = new FileOutputStream(staged)) {
                    if (in == null) throw new Exception("cannot open selected file");
                    byte[] buffer = new byte[65536]; long total = 0; int count;
                    while ((count = in.read(buffer)) != -1) { total += count; if (total > MAX_BUNDLE_BYTES) throw new Exception("file too large"); out.write(buffer, 0, count); }
                }
                if (LauncherDataBundle.looksLikeBundle(staged)) {
                    LauncherDataBundle.install(staged, destination, pendingSteam(), this::valid, this::plausibleSteamZip);
                    if (!staged.delete()) android.util.Log.w("MegabonkLauncher", "Could not clear imported bundle");
                } else if (OriginalGameDataValidator.looksLikeSteamZip(staged)) {
                    try (InputStream in = new FileInputStream(staged)) { stageSteamZip(in); }
                    if (!staged.delete()) android.util.Log.w("MegabonkLauncher", "Could not clear selected ZIP copy");
                    steamOnly = true;
                } else {
                    if (!valid(staged)) {
                        throw new Exception(es ? "el archivo no es compatible con esta versión" : "not a compatible Android data file");
                    }
                    SplitFileRecovery.recover(destination, this::valid);
                    if (backup.exists()) throw new Exception("previous data backup exists");
                    boolean hadOld = destination.exists();
                    if (hadOld && !destination.renameTo(backup)) throw new Exception("cannot preserve previous data");
                    if (!staged.renameTo(destination)) { if (hadOld) backup.renameTo(destination); throw new Exception("cannot install data"); }
                    if (hadOld) backup.delete();
                }
            } catch (Exception e) { error = LauncherImportError.describe(e, es); if (staged != null) staged.delete(); }
            }
            final String finalError = error; final boolean selectedSteam = steamOnly;
            runOnUiThread(() -> {
                if (isFinishing() || isDestroyed()) return;
                if (finalError == null && selectedSteam) {
                    setReady(dataReady);
                    message.setText(dataReady
                        ? (es ? "ZIP de Steam preparado: textos y texturas se importarán al jugar." : "Steam ZIP queued: text and textures will import when you play.")
                        : (es ? "ZIP de Steam preparado. Falta seleccionar OpenBonk-data.zip preparado en la PC o el OBB de esta versión." : "Steam ZIP queued. Still needed: OpenBonk-data.zip prepared on a PC or this build's OBB."));
                } else if (finalError == null) setReady(true);
                else { setReady(dataReady); message.setText((es ? "No se pudieron preparar los datos: " : "Could not prepare data: ") + finalError); choose.requestFocus(); }
            });
        }, "megabonk-data-import");
    }

    private void launchGame() {
        if (controlChanged) {
            synchronized (CONTROL_LOCK) {
                if (!getSharedPreferences(LAUNCHER_PREFS, MODE_PRIVATE).edit().putInt(PENDING_CONTROL, touchMode).commit()) {
                    message.setText(es ? "No se pudo guardar la selección de controles." : "Could not save the control selection.");
                    return;
                }
            }
        }
        Intent game = new Intent(); game.setClassName(getPackageName(), "com.unity3d.player.UnityPlayerGameActivity");
        game.putExtra("openbonk_split_launcher", true);
        if (controlChanged) game.putExtra("megabonk_launcher_touch_mode", touchMode);
        if (pendingSteam().isFile()) game.putExtra("megabonk_import_original_texts", true);
        startActivity(game);
        finish();
    }
}
