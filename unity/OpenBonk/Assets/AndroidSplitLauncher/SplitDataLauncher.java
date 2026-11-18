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
    private Button onlineMods, updateButton;
    private Button downloadUpdateButton;
    private TextView gameDataStatus, updateStatus, releaseStatus;
    private LinearLayout releaseList;
    private int updateRequest;
    private LinearLayout modCard, catalogCard;
    private View touchSettingsPanel;
    private RadioGroup controls;
    private boolean es;
    private boolean dataReady;
    private boolean controlChanged;
    private int touchMode;
    private boolean swapMenuButtons;
    private String exportProfileId;
    private String exportCurrentAccount;
    private static boolean embeddedData() { return "EMBEDDED".equals(SplitExpectedData.SHA256); }

    public static void registerGameDataGeneration(String generation) {
        Activity activity = com.unity3d.player.UnityPlayer.currentActivity;
        if (activity != null && generation != null && generation.matches("g-[0-9a-f]{32}"))
            activity.getSharedPreferences(LAUNCHER_PREFS, MODE_PRIVATE).edit().putString("game_zip_generation", generation).apply();
    }
    public static void showImportResult(boolean success) { showImportResult(success, ""); }
    public static void showImportResult(boolean success, String generation) {
        Activity activity = com.unity3d.player.UnityPlayer.currentActivity;
        if (activity == null) return;
        boolean spanish = Locale.getDefault().getLanguage().equals("es");
        if (success) registerGameDataGeneration(generation);
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
        layout.addView(header);
        LinearLayout tabs = new LinearLayout(this); tabs.setOrientation(LinearLayout.HORIZONTAL);
        layout.addView(tabs, new LinearLayout.LayoutParams(-1, dp(45)));
        LinearLayout body = new LinearLayout(this); body.setOrientation(LinearLayout.HORIZONTAL); body.setGravity(Gravity.TOP);
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
        LinearLayout.LayoutParams contentParams = new LinearLayout.LayoutParams(0, -2, 1.2f); contentParams.topMargin = dp(24); body.addView(left, contentParams);
        LinearLayout right = new LinearLayout(this); right.setOrientation(LinearLayout.VERTICAL); right.setPadding(dp(5), 0, 0, 0);
        LinearLayout.LayoutParams actionParams = new LinearLayout.LayoutParams(0, -2, 1f); actionParams.topMargin = dp(24); body.addView(right, actionParams);
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
            if (touchSettingsPanel != null) touchSettingsPanel.setVisibility(touchMode == 1 ? View.VISIBLE : View.GONE);
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
        chooseSteam = new Button(this); chooseSteam.setText(es ? "ZIP de archivos de Megabonk" : "Megabonk game files ZIP");
        chooseSteam.setOnClickListener(v -> openSteamPicker()); chooseSteam.setEnabled(false);
        styleButton(chooseSteam, LauncherDiagnosticIntegration.available(this)); dataCard.addView(chooseSteam, buttonParams());
        dataCard.addView(choose, buttonParams());
        LinearLayout inputCard = card(); LinearLayout.LayoutParams inputParams = new LinearLayout.LayoutParams(-1, -2); inputParams.topMargin = dp(7); left.addView(inputCard, inputParams);
        inputCard.addView(cardTitle(es ? "CONTROLES" : "CONTROLS")); inputCard.addView(controls);
        TextView controlHint = new TextView(this); controlHint.setTextColor(Color.rgb(225, 205, 235)); controlHint.setTextSize(12);
        controlHint.setText(es ? "Elegí el modo de control para la partida." : "Choose the control mode for your run.");
        inputCard.addView(controlHint);
        touchSettingsPanel = LauncherTouchPanel.create(this, es); inputCard.addView(touchSettingsPanel);
        touchSettingsPanel.setVisibility(touchMode == 1 ? View.VISIBLE : View.GONE);
        chooseProfile = new Button(this); chooseProfile.setText(es ? "Importar perfil ZIP" : "Import profile ZIP");
        chooseProfile.setEnabled(false); chooseProfile.setOnClickListener(v -> confirmProfilePicker());
        styleButton(chooseProfile, false); dataCard.addView(chooseProfile, buttonParams());
        exportCurrentProfile = new Button(this); exportCurrentProfile.setText(es ? "Exportar perfil" : "Export profile");
        exportCurrentProfile.setEnabled(false); exportCurrentProfile.setOnClickListener(v -> chooseCurrentProfile());
        styleButton(exportCurrentProfile, false); dataCard.addView(exportCurrentProfile, buttonParams());
        LinearLayout modPage = new LinearLayout(this); modPage.setOrientation(LinearLayout.VERTICAL); left.addView(modPage);
        modCard = card(); modPage.addView(modCard);
        catalogCard = card(); modPage.addView(catalogCard); catalogCard.setVisibility(View.GONE);
        modCard.addView(cardTitle("MODS  ✦"));
        modHint = new TextView(this); modHint.setText(es ? "Importá y elegí tus mods Lua" : "Import and choose your Lua mods"); modHint.setTextColor(Color.rgb(225, 205, 235)); modHint.setTextSize(12); modHint.setPadding(0, dp(3), 0, dp(5)); modCard.addView(modHint);
        importModButton = new Button(this); importModButton.setText(es ? "＋ Importar mod" : "＋ Import mod"); importModButton.setEnabled(false); importModButton.setOnClickListener(v -> openModPicker()); styleButton(importModButton, true); modCard.addView(importModButton, buttonParams());
        onlineMods = new Button(this); onlineMods.setText(es ? "Buscar mods" : "Browse mods");
        onlineMods.setOnClickListener(v -> LauncherOnlineUi.browse(this, es)); styleButton(onlineMods, false); modCard.addView(onlineMods, buttonParams());
        mods = new Button(this); mods.setText(es ? "Gestionar mods" : "Manage mods");
        mods.setEnabled(false); mods.setOnClickListener(v -> showMods());
        styleButton(mods, false); modCard.addView(mods, buttonParams());
        LinearLayout settingsCard = card(); left.addView(settingsCard);
        settingsCard.addView(cardTitle(es ? "Configuración general" : "General settings"));
        TextView settingsHint = new TextView(this); settingsHint.setTextColor(Color.rgb(225, 205, 235)); settingsHint.setTextSize(12);
        settingsHint.setText(es ? "Preferencias generales del launcher." : "General launcher preferences."); settingsCard.addView(settingsHint);
        TextView resolutionLabel = new TextView(this); resolutionLabel.setTextColor(Color.WHITE); resolutionLabel.setTextSize(13);
        resolutionLabel.setText(es ? "Resolución del juego" : "Game resolution"); settingsCard.addView(resolutionLabel);
        RadioGroup resolution = new RadioGroup(this); resolution.setOrientation(RadioGroup.HORIZONTAL);
        RadioGroup resolutionPresets = new RadioGroup(this); resolutionPresets.setOrientation(RadioGroup.HORIZONTAL);
        String[] resolutionNames = es ? new String[] { "Juego", "Nativa", "75%", "50%" } : new String[] { "Game", "Native", "75%", "50%" };
        int[] resolutionValues = {0, 100, 75, 50}; int savedResolution = launcherPrefs.getInt("render_resolution_percent", 0);
        for (int i = 0; i < resolutionValues.length; i++) {
            final int value = resolutionValues[i]; RadioButton option = new RadioButton(this); option.setId(View.generateViewId()); option.setText(resolutionNames[i]);
            option.setTextColor(Color.WHITE); option.setTextSize(12); option.setButtonTintList(ColorStateList.valueOf(Color.rgb(213, 245, 95))); styleControlOption(option);
            resolution.addView(option, new RadioGroup.LayoutParams(0, -2, 1)); if (value == savedResolution) resolution.check(option.getId());
            option.setOnClickListener(v -> { resolutionPresets.clearCheck(); launcherPrefs.edit().putInt("render_resolution_percent", value).apply(); });
        }
        settingsCard.addView(resolution);
        int[] presetHeights = {540, 720, 1080, 1440};
        for (int height : presetHeights) {
            RadioButton option = new RadioButton(this); option.setId(View.generateViewId()); option.setText(height + "p");
            option.setTextColor(Color.WHITE); option.setTextSize(12); option.setButtonTintList(ColorStateList.valueOf(Color.rgb(213, 245, 95))); styleControlOption(option);
            resolutionPresets.addView(option, new RadioGroup.LayoutParams(0, -2, 1));
            if (savedResolution == -height) resolutionPresets.check(option.getId());
            option.setOnClickListener(v -> { resolution.clearCheck(); launcherPrefs.edit().putInt("render_resolution_percent", -height).apply(); });
        }
        if (resolution.getCheckedRadioButtonId() == -1 && resolutionPresets.getCheckedRadioButtonId() == -1 && !(savedResolution <= -240 && savedResolution >= -2160))
            resolution.check(((RadioButton)resolution.getChildAt(0)).getId());
        settingsCard.addView(resolutionPresets);
        LinearLayout customRow = new LinearLayout(this); customRow.setOrientation(LinearLayout.HORIZONTAL); customRow.setGravity(Gravity.CENTER_VERTICAL);
        android.widget.EditText customHeight = new android.widget.EditText(this); customHeight.setSingleLine(true); customHeight.setInputType(android.text.InputType.TYPE_CLASS_NUMBER);
        customHeight.setTextColor(Color.WHITE); customHeight.setHintTextColor(Color.LTGRAY); customHeight.setTextSize(13);
        customHeight.setHint(es ? "Altura personalizada (240–2160)" : "Custom height (240–2160)");
        if (savedResolution <= -240 && savedResolution >= -2160) customHeight.setText(Integer.toString(-savedResolution));
        customRow.addView(customHeight, new LinearLayout.LayoutParams(0, -2, 1));
        Button applyHeight = new Button(this); applyHeight.setText(es ? "Aplicar" : "Apply"); styleButton(applyHeight, false);
        applyHeight.setOnClickListener(v -> {
            try {
                int height = Integer.parseInt(customHeight.getText().toString());
                if (height < 240 || height > 2160) throw new NumberFormatException();
                resolution.clearCheck(); resolutionPresets.clearCheck();
                launcherPrefs.edit().putInt("render_resolution_percent", -height).apply();
                customHeight.clearFocus();
            } catch (NumberFormatException invalid) {
                customHeight.setError(es ? "Usá un valor entre 240 y 2160" : "Enter a value from 240 to 2160");
            }
        });
        customRow.addView(applyHeight, new LinearLayout.LayoutParams(-2, -2)); settingsCard.addView(customRow);
        TextView resolutionHint = new TextView(this); resolutionHint.setTextColor(Color.rgb(225, 205, 235)); resolutionHint.setTextSize(12);
        resolutionHint.setText(es ? "Se aplica al iniciar y conserva la proporción de la pantalla. Juego usa el ajuste del menú del juego." : "Applies on launch and keeps the screen's aspect ratio. Game uses the in-game setting."); settingsCard.addView(resolutionHint);
        android.widget.CheckBox autoUpdates = new android.widget.CheckBox(this); autoUpdates.setTextColor(Color.WHITE);
        autoUpdates.setText(es ? "Buscar actualizaciones automáticamente" : "Check for updates automatically");
        autoUpdates.setChecked(launcherPrefs.getBoolean("auto_update_checks", true));
        autoUpdates.setOnCheckedChangeListener((v, checked) -> launcherPrefs.edit().putBoolean("auto_update_checks", checked).apply()); settingsCard.addView(autoUpdates);
        TextView moreSettings = new TextView(this); moreSettings.setTextColor(Color.rgb(225, 205, 235)); moreSettings.setTextSize(12);
        moreSettings.setText(es ? "Las opciones del juego están en su menú de configuración." : "Game options are available in the in-game settings menu."); settingsCard.addView(moreSettings);
        LinearLayout updateCard = card(); left.addView(updateCard); updateCard.addView(cardTitle(es ? "Actualizaciones" : "Updates"));
        TextView installedVersion = new TextView(this); installedVersion.setTextColor(Color.WHITE); installedVersion.setTextSize(14);
        installedVersion.setText((es ? "Versión instalada: " : "Installed version: ") + installedVersion()); updateCard.addView(installedVersion);
        updateStatus = new TextView(this); updateStatus.setTextColor(Color.rgb(225, 205, 235)); updateStatus.setTextSize(13);
        updateStatus.setText(es ? "Última versión: sin comprobar" : "Latest version: not checked"); updateCard.addView(updateStatus);
        LinearLayout updateActions = new LinearLayout(this); updateActions.setOrientation(LinearLayout.HORIZONTAL);
        LinearLayout.LayoutParams updateActionsParams = new LinearLayout.LayoutParams(-1, dp(37)); updateActionsParams.topMargin = dp(6); updateCard.addView(updateActions, updateActionsParams);
        updateButton = new Button(this); updateButton.setText(es ? "Buscar actualización" : "Check for updates");
        updateButton.setOnClickListener(v -> loadUpdateOverview()); styleButton(updateButton, false); updateActions.addView(updateButton, new LinearLayout.LayoutParams(dp(175), -1));
        downloadUpdateButton = new Button(this); downloadUpdateButton.setText(es ? "Descargar actualización" : "Download update");
        styleButton(downloadUpdateButton, true); downloadUpdateButton.setVisibility(View.GONE); LinearLayout.LayoutParams downloadParams = new LinearLayout.LayoutParams(dp(175), -1); downloadParams.leftMargin = dp(6); updateActions.addView(downloadUpdateButton, downloadParams);
        TextView releaseHeading = cardTitle(es ? "Últimos cambios" : "Recent releases"); releaseHeading.setPadding(0, dp(12), 0, dp(2)); updateCard.addView(releaseHeading);
        releaseStatus = new TextView(this); releaseStatus.setTextColor(Color.rgb(225, 205, 235)); releaseStatus.setTextSize(12);
        releaseStatus.setText(es ? "Abrí esta sección para cargar los cambios." : "Open this section to load release notes."); updateCard.addView(releaseStatus);
        releaseList = new LinearLayout(this); releaseList.setOrientation(LinearLayout.VERTICAL); updateCard.addView(releaseList);
        LinearLayout helpCard = card(); left.addView(helpCard); helpCard.addView(cardTitle(es ? "Ayuda y soporte" : "Support & help"));
        TextView helpHint = new TextView(this); helpHint.setTextColor(Color.rgb(225, 205, 235)); helpHint.setTextSize(12);
        helpHint.setText(es ? "Compartí ideas o reportá problemas en GitHub. Necesitás una cuenta para enviar comentarios." : "Share ideas or report problems on GitHub. A GitHub account is needed to submit feedback."); helpCard.addView(helpHint);
        Button feedback = new Button(this); feedback.setText(es ? "Enviar comentarios" : "Send feedback");
        feedback.setOnClickListener(v -> {
            try { startActivity(new Intent(Intent.ACTION_VIEW, android.net.Uri.parse("https://github.com/martin2844/OpenBonk/issues/new/choose"))); }
            catch (Exception e) { new AlertDialog.Builder(this).setMessage(es ? "No se pudo abrir el navegador." : "Could not open the browser.").setPositiveButton("OK", null).show(); }
        });
        styleButton(feedback, true); helpCard.addView(feedback, buttonParams());
        if (hasTextureNotices()) {
            Button notices = new Button(this); notices.setText(es ? "Avisos" : "Notices");
            notices.setOnClickListener(v -> showTextureNotices()); styleButton(notices, false); helpCard.addView(notices, buttonParams());
        }
        LinearLayout[] pages = { dataCard, inputCard, modPage, settingsCard, updateCard, helpCard };
        String[] tabNames = es ? new String[] { "Juego", "Controles", "Mods", "Ajustes", "Actualizar", "Ayuda" } : new String[] { "Game", "Controls", "Mods", "Settings", "Updates", "Help" };
        Button[] tabButtons = new Button[pages.length];
        for (int i = 0; i < pages.length; i++) {
            final int index = i; Button tab = new Button(this); tab.setText(tabNames[i]); tabButtons[i] = tab;
            LinearLayout.LayoutParams tabParams = new LinearLayout.LayoutParams(0, dp(37), 1); tabParams.setMargins(dp(2), 0, dp(2), 0); tabs.addView(tab, tabParams);
            tab.setOnClickListener(v -> { art.setVisibility(index == 0 ? View.VISIBLE : View.GONE); right.setVisibility(index == 0 ? View.VISIBLE : View.GONE); contentParams.topMargin = dp(index == 0 ? 24 : 12); left.setLayoutParams(contentParams); for (int n = 0; n < pages.length; n++) { pages[n].setVisibility(n == index ? View.VISIBLE : View.GONE); styleButton(tabButtons[n], n == index); } if (index == 4 && updateRequest == 0) loadUpdateOverview(); });
        }
        for (int i = 0; i < pages.length; i++) { pages[i].setVisibility(i == 0 ? View.VISIBLE : View.GONE); styleButton(tabButtons[i], i == 0); }
        play = new Button(this); play.setText(es ? "▶   JUGAR" : "▶   PLAY"); play.setEnabled(false);
        play.setOnClickListener(v -> launchGame());
        stylePlay(play); LinearLayout.LayoutParams playParams = new LinearLayout.LayoutParams(-1, dp(58)); playParams.topMargin = dp(9); right.addView(play, playParams);
        LinearLayout gameStatusCard = card(); LinearLayout.LayoutParams statusParams = new LinearLayout.LayoutParams(-1, -2); statusParams.topMargin = dp(10); right.addView(gameStatusCard, statusParams);
        TextView gameStatusTitle = cardTitle(es ? "Estado" : "Status"); gameStatusCard.addView(gameStatusTitle);
        gameDataStatus = new TextView(this); gameDataStatus.setTextSize(13); gameDataStatus.setTextColor(Color.rgb(225, 205, 235));
        gameDataStatus.setText(es ? "◌  Comprobando archivos del juego…" : "◌  Checking game files…"); gameStatusCard.addView(gameDataStatus);
        TextView gameVersion = new TextView(this); gameVersion.setTextSize(13); gameVersion.setTextColor(Color.rgb(225, 205, 235));
        gameVersion.setText((es ? "Versión instalada: " : "Installed version: ") + installedVersion()); gameStatusCard.addView(gameVersion);
        ScrollView scroll = new ScrollView(this); scroll.setFillViewport(true); scroll.addView(layout); setContentView(scroll);
        checkData();
        long lastCheck = launcherPrefs.getLong("last_update_check", 0);
        if (launcherPrefs.getBoolean("auto_update_checks", true) && System.currentTimeMillis() - lastCheck >= 24L * 60 * 60 * 1000) {
            launcherPrefs.edit().putLong("last_update_check", System.currentTimeMillis()).apply();
            LauncherOnlineUi.checkUpdate(this, es, true);
        }
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
                        LauncherDataBundle.recoverTransactions(existing, legacyPendingSteam(), pendingSteam());
                        SplitFileRecovery.recover(existing, this::valid);
                        migrateCompatibleObb(existing);
                        LauncherObbMigration.migrate(this, existing, () -> runOnUiThread(() -> {
                            if (!isFinishing() && !isDestroyed()) message.setText(es ? "Descargando datos para actualizar…" : "Downloading update data…");
                        }));
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
                    try { LauncherDataBundle.cleanupPartials(existing, getCacheDir(), pendingSteam(), getFilesDir()); }
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

    int dp(float value) { return (int)(value * getResources().getDisplayMetrics().density + 0.5f); }
    private int modPageSize(int reservedDp, int rowDp) {
        int screenDp = (int)(getResources().getDisplayMetrics().heightPixels / getResources().getDisplayMetrics().density);
        return Math.max(1, Math.min(8, (screenDp - reservedDp) / rowDp));
    }
    private GradientDrawable shape(int color, int stroke) {
        GradientDrawable result = new GradientDrawable(); result.setColor(color); result.setCornerRadius(dp(12)); result.setStroke(dp(2), stroke); return result;
    }
    private LinearLayout card() {
        LinearLayout result = new LinearLayout(this); result.setOrientation(LinearLayout.VERTICAL); result.setPadding(dp(10), dp(8), dp(10), dp(8));
        result.setBackground(shape(Color.rgb(57, 21, 73), Color.rgb(126, 52, 147))); return result;
    }
    TextView cardTitle(String text) {
        TextView result = new TextView(this); result.setText(text); result.setTextColor(Color.rgb(255, 247, 234)); result.setTypeface(Typeface.DEFAULT, Typeface.BOLD); result.setTextSize(16); return result;
    }
    LinearLayout.LayoutParams buttonParams() { LinearLayout.LayoutParams result = new LinearLayout.LayoutParams(-1, dp(37)); result.topMargin = dp(4); return result; }
    private ColorStateList buttonText(int active) {
        return new ColorStateList(new int[][]{new int[]{-android.R.attr.state_enabled}, new int[]{}},
            new int[]{Color.rgb(162, 143, 166), active});
    }
    void styleButton(Button button, boolean accent) {
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

    @Override protected void onResume() {
        super.onResume();
        if (touchSettingsPanel != null && touchSettingsPanel.getParent() instanceof android.view.ViewGroup) {
            android.view.ViewGroup parent = (android.view.ViewGroup)touchSettingsPanel.getParent();
            int position = parent.indexOfChild(touchSettingsPanel); parent.removeView(touchSettingsPanel);
            touchSettingsPanel = LauncherTouchPanel.create(this, es); parent.addView(touchSettingsPanel, position);
            touchSettingsPanel.setVisibility(touchMode == 1 ? View.VISIBLE : View.GONE);
        }
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
            modCard.setVisibility(View.GONE); catalogCard.setVisibility(View.VISIBLE); catalogCard.removeAllViews();
            catalogCard.addView(cardTitle(es ? "Gestionar mods" : "Manage mods"));
            LinearLayout results = new LinearLayout(this); results.setOrientation(LinearLayout.VERTICAL); catalogCard.addView(results);
            LinearLayout navigation = new LinearLayout(this); navigation.setGravity(Gravity.CENTER_VERTICAL);
            Button previous = new Button(this); previous.setText("‹"); previous.setContentDescription(es ? "Página anterior" : "Previous page"); styleButton(previous, false);
            TextView pageLabel = new TextView(this); pageLabel.setTextColor(Color.WHITE); pageLabel.setGravity(Gravity.CENTER);
            Button next = new Button(this); next.setText("›"); next.setContentDescription(es ? "Página siguiente" : "Next page"); styleButton(next, false);
            navigation.addView(previous, new LinearLayout.LayoutParams(dp(48), dp(37))); navigation.addView(pageLabel, new LinearLayout.LayoutParams(0, dp(37), 1)); navigation.addView(next, new LinearLayout.LayoutParams(dp(48), dp(37))); catalogCard.addView(navigation);
            int[] page = {0}; int perPage = modPageSize(220, 43);
            Runnable render = () -> {
                results.removeAllViews(); int pages = Math.max(1, (ids.size() + perPage - 1) / perPage); page[0] = Math.min(page[0], pages - 1);
                int start = page[0] * perPage, end = Math.min(ids.size(), start + perPage);
                for (int i = start; i < end; i++) {
                    final int index = i; android.widget.CheckBox choice = new android.widget.CheckBox(this);
                    choice.setText(ids.get(i)); choice.setTextColor(Color.WHITE); choice.setTextSize(14); choice.setSingleLine(true); choice.setEllipsize(android.text.TextUtils.TruncateAt.END);
                    choice.setButtonTintList(ColorStateList.valueOf(Color.rgb(213, 245, 95))); choice.setChecked(selected[i]);
                    choice.setOnCheckedChangeListener((button, checked) -> selected[index] = checked);
                    results.addView(choice, new LinearLayout.LayoutParams(-1, dp(43)));
                }
                pageLabel.setText((page[0] + 1) + " / " + pages); previous.setEnabled(page[0] > 0); next.setEnabled(page[0] + 1 < pages);
            };
            previous.setOnClickListener(v -> { if (page[0] > 0) { page[0]--; render.run(); } });
            next.setOnClickListener(v -> { page[0]++; render.run(); }); render.run();
            LinearLayout actions = new LinearLayout(this); actions.setOrientation(LinearLayout.HORIZONTAL); catalogCard.addView(actions, buttonParams());
            Button back = new Button(this); back.setText(es ? "Cancelar" : "Cancel"); styleButton(back, false);
            back.setOnClickListener(v -> { catalogCard.setVisibility(View.GONE); modCard.setVisibility(View.VISIBLE); }); actions.addView(back, new LinearLayout.LayoutParams(0, -1, 1));
            Button restore = new Button(this); restore.setText(es ? "Importar / restaurar" : "Import / restore"); styleButton(restore, false);
            restore.setOnClickListener(v -> showModActions(modsRoot)); LinearLayout.LayoutParams restoreParams = new LinearLayout.LayoutParams(0, -1, 1); restoreParams.leftMargin = dp(5); actions.addView(restore, restoreParams);
            Button save = new Button(this); save.setText(es ? "Guardar" : "Save"); styleButton(save, true);
            save.setOnClickListener(v -> {
                    SharedPreferences.Editor editor = prefs.edit(); boolean changed = false;
                    for (int i = 0; i < ids.size(); i++) if (selected[i] != original[i]) { editor.putBoolean(ids.get(i), selected[i]); changed = true; }
                    if (changed) {
                        if (!editor.commit()) { showDialog(new AlertDialog.Builder(this).setMessage(es ? "No se pudieron guardar los mods." : "Could not save mod choices.").setPositiveButton("OK", null).create()); return; }
                        else {
                            IMPORT_GATE.clearModResult();
                            modHint.setText(es ? "Selección de mods guardada." : "Mod choices saved.");
                        }
                    }
                    catalogCard.setVisibility(View.GONE); modCard.setVisibility(View.VISIBLE);
                });
            LinearLayout.LayoutParams saveParams = new LinearLayout.LayoutParams(0, -1, 1); saveParams.leftMargin = dp(5); actions.addView(save, saveParams);
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

    static boolean validModHooks(JSONObject manifest) { return LauncherModHookValidation.validModHooks(manifest); }

    private boolean gameZipImported() {
        String generation = getSharedPreferences(LAUNCHER_PREFS, MODE_PRIVATE).getString("game_zip_generation", "");
        File root = getExternalFilesDir(null);
        if (root == null || generation == null || !generation.matches("g-[0-9a-f]{32}")) return false;
        File data = new File(new File(root, "OriginalLocalization"), generation);
        File shared = new File(data, "shared.bundle"), strings = new File(data, "strings.bundle");
        return shared.isFile() && shared.length() > 0 && strings.isFile() && strings.length() > 0;
    }

    private void setReady(boolean ready) {
        getWindow().clearFlags(WindowManager.LayoutParams.FLAG_KEEP_SCREEN_ON);
        dataReady = ready;
        gameDataStatus.setText(ready ? (es ? "✓  Archivos del juego: listos" : "✓  Game files: ready") : (es ? "✕  Archivos del juego: faltan" : "✕  Game files: needed"));
        gameDataStatus.setTextColor(ready ? Color.rgb(213, 245, 95) : Color.rgb(255, 184, 153));
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
            if (pendingSteam().isFile()) status += es ? "\n✓ ZIP de Megabonk preparado" : "\n✓ Megabonk ZIP queued";
            else if (gameZipImported()) status += es ? "\n✓ ZIP de Megabonk importado" : "\n✓ Megabonk ZIP imported";
            else if (!embeddedData() && LauncherDiagnosticIntegration.available(this)) status += es ? "\n✓ Datos de Megabonk verificados" : "\n✓ Megabonk data verified";
            else status += es ? "\nZIP de Megabonk: sin confirmar" : "\nMegabonk ZIP: not confirmed";
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
        mods.setEnabled(true); importModButton.setEnabled(true); onlineMods.setEnabled(true); updateButton.setEnabled(true);
        (ready ? play : LauncherDiagnosticIntegration.available(this) ? chooseSteam : choose).requestFocus();
    }

    private void setImportBusy() {
        getWindow().addFlags(WindowManager.LayoutParams.FLAG_KEEP_SCREEN_ON);
        gameDataStatus.setText(es ? "◌  Preparando archivos…" : "◌  Preparing files…"); gameDataStatus.setTextColor(Color.rgb(225, 205, 235));
        choose.setEnabled(false); chooseSteam.setEnabled(false); chooseProfile.setEnabled(false); exportCurrentProfile.setEnabled(false);
        play.setEnabled(false); mods.setEnabled(false); importModButton.setEnabled(false); onlineMods.setEnabled(false); updateButton.setEnabled(false);
    }

    private String installedVersion() {
        try { String version = getPackageManager().getPackageInfo(getPackageName(), 0).versionName; return version == null || version.isEmpty() ? "—" : version; }
        catch (Exception e) { return "—"; }
    }

    private void loadUpdateOverview() {
        final int request = ++updateRequest;
        updateStatus.setText(es ? "Buscando la última versión…" : "Checking the latest version…");
        releaseStatus.setVisibility(View.VISIBLE); releaseStatus.setText(es ? "Cargando novedades…" : "Loading release notes…"); releaseList.removeAllViews();
        downloadUpdateButton.setVisibility(View.GONE);
        new Thread(() -> {
            try {
                JSONObject info = LauncherOnline.metadata(LauncherOnline.UPDATES);
                long remote = info.getLong("version_code");
                if (remote <= 0 || info.optString("tag").isEmpty()) throw new IOException("invalid update metadata");
                long installed = getPackageManager().getPackageInfo(getPackageName(), 0).versionCode;
                runOnUiThread(() -> {
                    if (isFinishing() || isDestroyed() || request != updateRequest) return;
                    String status = remote > installed ? (es ? "Actualización disponible" : "Update available") : (es ? "Estás al día" : "Up to date");
                    updateStatus.setText((es ? "Última versión: " : "Latest version: ") + info.optString("tag") + "  ·  " + status);
                    if (remote > installed) {
                        downloadUpdateButton.setVisibility(View.VISIBLE);
                        downloadUpdateButton.setOnClickListener(v -> LauncherOnlineUi.downloadUpdate(this, es, info));
                    }
                });
            } catch (Exception e) { runOnUiThread(() -> {
                if (!isFinishing() && !isDestroyed() && request == updateRequest) updateStatus.setText(es ? "No se pudo comprobar la última versión. Intentá de nuevo." : "Could not check the latest version. Try again.");
            }); }
        }, "openbonk-update-overview").start();
        new Thread(() -> {
            try {
                JSONArray history = LauncherOnline.releases();
                runOnUiThread(() -> { if (!isFinishing() && !isDestroyed() && request == updateRequest) showReleaseHistory(history); });
            } catch (Exception e) { runOnUiThread(() -> {
                if (!isFinishing() && !isDestroyed() && request == updateRequest) releaseStatus.setText(es ? "No se pudieron cargar las novedades. Intentá de nuevo." : "Could not load release notes. Try again.");
            }); }
        }, "openbonk-release-history").start();
    }

    private void showReleaseHistory(JSONArray history) {
        releaseList.removeAllViews(); int count = 0;
        for (int i = 0; i < history.length() && count < 3; i++) {
            JSONObject release = history.optJSONObject(i); if (release == null || release.optBoolean("draft")) continue;
            String tag = release.optString("tag_name", "");
            if (!tag.matches("v[0-9]+\\.[0-9]+\\.[0-9]+(?:-[A-Za-z0-9.]+)?")) continue;
            JSONArray assets = release.optJSONArray("assets"); boolean hasApk = false;
            if (assets != null) for (int j = 0; j < assets.length(); j++) {
                JSONObject asset = assets.optJSONObject(j); if (asset != null && "OpenBonk.apk".equals(asset.optString("name"))) { hasApk = true; break; }
            }
            if (!hasApk) continue;
            count++;
            TextView heading = new TextView(this); heading.setTextColor(Color.rgb(213, 245, 95)); heading.setTypeface(Typeface.DEFAULT, Typeface.BOLD); heading.setTextSize(14);
            String date = release.optString("published_at", ""); if (date.length() >= 10) date = date.substring(0, 10); else date = "";
            heading.setText(tag + (date.isEmpty() ? "" : "  ·  " + date)); heading.setPadding(0, dp(8), 0, dp(2)); releaseList.addView(heading);
            String body = release.optString("body", "").trim();
            if (body.isEmpty()) body = es ? "Sin notas para esta versión." : "No release notes provided.";
            if (body.length() > 4000) body = body.substring(0, 4000) + "…";
            body = body.replace("**", "").replaceAll("(?m)^#{1,6}\\s+", "");
            TextView notes = new TextView(this); notes.setTextColor(Color.rgb(225, 205, 235)); notes.setTextSize(12); notes.setText(body); releaseList.addView(notes);
            Button open = new Button(this); open.setText(es ? "Ver en GitHub" : "View on GitHub"); styleButton(open, false);
            open.setOnClickListener(v -> { try { startActivity(new Intent(Intent.ACTION_VIEW, android.net.Uri.parse("https://github.com/martin2844/OpenBonk/releases/tag/" + tag))); } catch (Exception ignored) { } });
            LinearLayout.LayoutParams openParams = new LinearLayout.LayoutParams(dp(140), dp(37)); openParams.topMargin = dp(4); releaseList.addView(open, openParams);
        }
        releaseStatus.setVisibility(count == 0 ? View.VISIBLE : View.GONE);
        if (count == 0) releaseStatus.setText(es ? "No hay versiones Android disponibles." : "No Android releases found.");
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
            case "Preparando recursos": return "Preparing assets…";
            case "Preparando animaciones": return "Preparing animations…";
            case "Preparando cielos": return "Preparing skies…";
            case "Preparando modelos": return "Preparing models…";
            case "Preparando texturas": return "Preparing textures…";
            case "Preparando audio": return "Preparing audio…";
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

    // Preserve the previous version's OBB; reuse it only if this build accepts its exact hash.
    private void migrateCompatibleObb(File destination) throws IOException {
        if (destination.exists()) return;
        File dir = destination.getParentFile(); File[] files = dir.listFiles();
        if (files == null) return;
        long current;
        try { current = getPackageManager().getPackageInfo(getPackageName(), 0).versionCode; }
        catch (Exception e) { throw new IOException("cannot inspect installed version", e); }
        File candidate = null; long best = 0; String suffix = "." + getPackageName() + ".obb";
        for (File file : files) {
            String name = file.getName();
            if (!name.startsWith("main.") || !name.endsWith(suffix)) continue;
            String number = name.substring(5, name.length() - suffix.length());
            try { long version = Long.parseLong(number); if (version > best && version < current && valid(file)) { best = version; candidate = file; } }
            catch (NumberFormatException ignored) { }
        }
        if (candidate == null) return;
        File staging = File.createTempFile("openbonk-obb-migration-", ".tmp", dir);
        try {
            try (InputStream in = new FileInputStream(candidate); FileOutputStream out = new FileOutputStream(staging)) {
                byte[] buffer = new byte[65536]; int n; while ((n = in.read(buffer)) != -1) out.write(buffer, 0, n);
                out.getFD().sync();
            }
            if (!valid(staging)) throw new IOException("migrated game data failed validation");
            if (destination.exists() || !staging.renameTo(destination)) throw new IOException("cannot install migrated game data");
        } finally { staging.delete(); }
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

    private File pendingSteam() { return new File(getFilesDir(), "megabonk-original-texts.pending.zip"); }
    private File legacyPendingSteam() { return new File(getCacheDir(), "megabonk-original-texts.pending.zip"); }
    private void recoverSteamZip() throws java.io.IOException {
        File destination = pendingSteam();
        SplitFileRecovery.recover(destination, this::plausibleSteamZip);
        SplitFileRecovery.recover(destination, new File(getFilesDir(), "megabonk-original-texts.previous"), this::plausibleSteamZip);
        File legacy = legacyPendingSteam();
        SplitFileRecovery.recover(legacy, this::plausibleSteamZip);
        // Older launchers also used a different backup name for this ZIP.
        SplitFileRecovery.recover(legacy, new File(getCacheDir(), "megabonk-original-texts.previous"), this::plausibleSteamZip);
        SplitFileRecovery.migrate(legacy, destination, this::plausibleSteamZip);
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
        String instructions = es
            ? "ZIP de archivos de Megabonk: elegí un ZIP de la carpeta Megabonk_Data de tu copia compatible. OpenBonk prepara los datos del juego en el dispositivo. La preparación puede tardar y necesita espacio libre.\n\nImportar datos Android preparados: alternativa para usar un paquete OpenBonk-data.zip o archivo OBB compatible ya convertido en una PC. La mayoría de los jugadores puede usar la primera opción.\n\nImportar perfil ZIP: importa progreso guardado; no instala el juego.\n\nExportar perfil: guarda una copia de tu progreso en un ZIP.\n\nEl estado junto a Jugar indica si los datos del juego están listos."
            : "Megabonk game files ZIP: select a ZIP of the Megabonk_Data folder from your compatible game copy. OpenBonk prepares the game data on this device. Preparation can take time and needs free storage.\n\nImport prepared Android data: an alternative for an OpenBonk-data.zip or compatible OBB already converted on a PC. Most players can use the first option.\n\nImport profile ZIP: imports saved progress; it does not install the game.\n\nExport profile: saves a copy of your progress to a ZIP.\n\nThe status beside Play shows whether game data is ready.";
        if (embeddedData()) instructions = (es ? "Esta compilación incluye datos del juego.\n\n" : "This build includes game data.\n\n") + instructions;
        showDialog(new AlertDialog.Builder(this).setTitle(es ? "Preparar datos del juego" : "Prepare game data").setMessage(instructions).setPositiveButton("OK", null).create());
    }

    void showCatalogLoading() {
        modCard.setVisibility(View.GONE); catalogCard.setVisibility(View.VISIBLE); catalogCard.removeAllViews();
        catalogCard.addView(cardTitle(es ? "Catálogo de mods" : "Mod catalog"));
        TextView text = new TextView(this); text.setTextColor(Color.WHITE); text.setText(es ? "Cargando…" : "Loading…"); catalogCard.addView(text);
    }
    void showCatalogError(String error) {
        modCard.setVisibility(View.GONE); catalogCard.setVisibility(View.VISIBLE); catalogCard.removeAllViews();
        catalogCard.addView(cardTitle(es ? "No se pudo cargar el catálogo" : "Could not load catalog"));
        TextView text = new TextView(this); text.setTextColor(Color.WHITE); text.setText(error); catalogCard.addView(text);
        Button back = new Button(this); back.setText(es ? "Volver" : "Back"); styleButton(back, false);
        back.setOnClickListener(v -> { catalogCard.setVisibility(View.GONE); modCard.setVisibility(View.VISIBLE); }); catalogCard.addView(back, buttonParams());
    }
    void showCatalog(JSONArray entries) {
        modCard.setVisibility(View.GONE); catalogCard.setVisibility(View.VISIBLE); catalogCard.removeAllViews();
        catalogCard.addView(cardTitle(es ? "Catálogo de mods" : "Mod catalog"));
        android.widget.EditText search = new android.widget.EditText(this); search.setSingleLine(true); search.setTextColor(Color.WHITE);
        search.setHintTextColor(Color.rgb(184, 163, 192)); search.setTextSize(13); search.setHint(es ? "Buscar mods" : "Search mods"); catalogCard.addView(search, buttonParams());
        LinearLayout results = new LinearLayout(this); results.setOrientation(LinearLayout.VERTICAL); catalogCard.addView(results);
        LinearLayout navigation = new LinearLayout(this); navigation.setGravity(Gravity.CENTER_VERTICAL);
        Button previous = new Button(this); previous.setText("‹"); previous.setContentDescription(es ? "Página anterior" : "Previous page"); styleButton(previous, false);
        TextView pageLabel = new TextView(this); pageLabel.setTextColor(Color.WHITE); pageLabel.setGravity(Gravity.CENTER);
        Button next = new Button(this); next.setText("›"); next.setContentDescription(es ? "Página siguiente" : "Next page"); styleButton(next, false);
        navigation.addView(previous, new LinearLayout.LayoutParams(dp(48), dp(37))); navigation.addView(pageLabel, new LinearLayout.LayoutParams(0, dp(37), 1)); navigation.addView(next, new LinearLayout.LayoutParams(dp(48), dp(37))); catalogCard.addView(navigation);
        int widthDp = (int)(getResources().getDisplayMetrics().widthPixels / getResources().getDisplayMetrics().density);
        int columns = widthDp >= 600 ? 2 : 1;
        int[] page = {0}; int perPage = modPageSize(240, 48) * columns;
        Runnable render = () -> {
            results.removeAllViews(); String query = search.getText().toString().trim().toLowerCase(Locale.ROOT); ArrayList<JSONObject> matches = new ArrayList<>();
            for (int i = 0; i < entries.length(); i++) {
                JSONObject item = entries.optJSONObject(i); if (item == null) continue;
                String name = item.optString("name"), description = item.optString("description");
                if (!(name + " " + description).toLowerCase(Locale.ROOT).contains(query)) continue;
                matches.add(item);
            }
            int pages = Math.max(1, (matches.size() + perPage - 1) / perPage); page[0] = Math.min(page[0], pages - 1);
            int start = page[0] * perPage, end = Math.min(matches.size(), start + perPage);
            LinearLayout rowGroup = null;
            for (int i = start; i < end; i++) {
                if ((i - start) % columns == 0) { rowGroup = new LinearLayout(this); rowGroup.setOrientation(LinearLayout.HORIZONTAL); results.addView(rowGroup, new LinearLayout.LayoutParams(-1, dp(48))); }
                JSONObject item = matches.get(i); LinearLayout row = new LinearLayout(this); row.setGravity(Gravity.CENTER_VERTICAL); row.setPadding(dp(4), dp(2), dp(4), dp(2));
                LinearLayout info = new LinearLayout(this); info.setOrientation(LinearLayout.VERTICAL); row.addView(info, new LinearLayout.LayoutParams(0, -2, 1));
                TextView title = new TextView(this); title.setText(item.optString("name") + "  ·  " + item.optString("version")); title.setTextColor(Color.WHITE); title.setTypeface(Typeface.DEFAULT, Typeface.BOLD); title.setSingleLine(true); title.setEllipsize(android.text.TextUtils.TruncateAt.END); info.addView(title);
                TextView details = new TextView(this); details.setText(item.optString("description")); details.setTextColor(Color.rgb(225, 205, 235)); details.setTextSize(12); details.setSingleLine(true); details.setEllipsize(android.text.TextUtils.TruncateAt.END); info.addView(details);
                info.setFocusable(true); info.setOnClickListener(v -> showDialog(new AlertDialog.Builder(this).setTitle(item.optString("name")).setMessage(item.optString("description")).setPositiveButton("OK", null).create()));
                Button install = new Button(this); install.setText(es ? "Importar mod" : "Import mod"); styleButton(install, true);
                install.setOnClickListener(v -> importOnlineMod(item)); LinearLayout.LayoutParams installParams = new LinearLayout.LayoutParams(dp(112), dp(37)); installParams.leftMargin = dp(6); row.addView(install, installParams); rowGroup.addView(row, new LinearLayout.LayoutParams(0, -1, 1));
            }
            if (matches.isEmpty()) { TextView empty = new TextView(this); empty.setTextColor(Color.WHITE); empty.setText(es ? "No se encontraron mods." : "No mods found."); results.addView(empty); }
            pageLabel.setText((page[0] + 1) + " / " + pages + "  ·  " + matches.size()); previous.setEnabled(page[0] > 0); next.setEnabled(page[0] + 1 < pages);
        };
        previous.setOnClickListener(v -> { if (page[0] > 0) { page[0]--; render.run(); } });
        next.setOnClickListener(v -> { page[0]++; render.run(); });
        search.addTextChangedListener(new android.text.TextWatcher() {
            public void beforeTextChanged(CharSequence text, int start, int count, int after) { }
            public void onTextChanged(CharSequence text, int start, int before, int count) { page[0] = 0; render.run(); }
            public void afterTextChanged(android.text.Editable text) { }
        });
        render.run();
        Button back = new Button(this); back.setText(es ? "Volver" : "Back"); styleButton(back, false);
        back.setOnClickListener(v -> { catalogCard.setVisibility(View.GONE); modCard.setVisibility(View.VISIBLE); }); catalogCard.addView(back, buttonParams());
    }

    void importOnlineMod(JSONObject item) {
        if (!IMPORT_GATE.begin()) { modHint.setText(es ? "Ya hay una importación en curso." : "An import is already in progress."); return; }
        setImportBusy(); modHint.setText(es ? "Descargando mod…" : "Downloading mod…");
        final long generation = IMPORT_GATE.generation();
        runImport(() -> {
            String id = null, error = null; File file = null;
            try {
                file = File.createTempFile("openbonk-mod-", ".zip", getCacheDir());
                LauncherOnline.download(item, file, 128L * 1024 * 1024); LauncherOnline.checkModId(file, item.getString("id"));
                synchronized (DATA_LOCK) {
                    File root = getExternalFilesDir(null); if (root == null) throw new IOException("storage unavailable");
                    try (InputStream in = new FileInputStream(file)) { id = LauncherModImporter.install(in, new File(root, "Mods"), getSharedPreferences("megabonk_mod_state", MODE_PRIVATE)); }
                }
            } catch (Exception e) { error = LauncherImportError.describe(e, es); }
            finally { if (file != null) file.delete(); }
            IMPORT_GATE.modResult(id, error, false); final String failure = error;
            runOnUiThread(() -> { if (isFinishing() || isDestroyed() || IMPORT_GATE.generation() != generation) return;
                setReady(dataReady); showModImportResult();
                if (failure == null) { modCard.setVisibility(View.VISIBLE); catalogCard.setVisibility(View.GONE); }
                else showCatalogError(failure);
            });
        }, "openbonk-online-mod");
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
        File staged = new File(getFilesDir(), "megabonk-original-texts.partial");
        try {
            LauncherSteamZipInstaller.install(input, staged, pendingSteam(),
                new File(getFilesDir(), "megabonk-original-texts.previous"),
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
                    LauncherPreparedSteamImport.install(in, getFilesDir(), destination, pendingSteam(),
                        this::plausibleSteamZip, this::valid, MAX_STEAM_BYTES,
                        LauncherDiagnosticIntegration.create(this, IMPORT_GATE::stage), staged -> {
                            OriginalGameDataValidator.Problem problem = OriginalGameDataValidator.problem(staged);
                            if (problem == null) return null;
                            return OriginalGameDataValidator.describeProblem(problem, es);
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
