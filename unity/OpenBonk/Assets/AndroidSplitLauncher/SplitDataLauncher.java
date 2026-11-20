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
import android.widget.FrameLayout;
import android.widget.ProgressBar;
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
    private static final int CANVAS = 0xff190f24;
    private static final int PANEL = 0xff251831;
    private static final int INSET = 0xff30203e;
    private static final int LINE = 0xff493453;
    private static final int INK = 0xfffaf2ff;
    private static final int MUTED = 0xffcbbbd4;
    private static final int GOLD = 0xffffb744;
    private static final int UPDATE_ORANGE = 0xffff9b42;
    private static final int LIME = 0xffd5f55f;
    private static final Typeface DISPLAY = Typeface.create("sans-serif-black", Typeface.NORMAL);
    private static final Typeface MEDIUM = Typeface.create("sans-serif-medium", Typeface.NORMAL);
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
    private static final String LAUNCH_ATTEMPT = "openbonk_launch_attempt";
    private static final String LAUNCH_PID = "openbonk_launch_pid";
    private static final String LAUNCH_REPORT = "openbonk_launch_report";
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
    private Button downloadUpdateButton, updatesTab;
    private BusyAction appUpdateAction;
    private static final LauncherTransferState MOD_TRANSFER = new LauncherTransferState();
    private final java.util.HashMap<String, CatalogAction> catalogActions = new java.util.HashMap<>();
    private Runnable catalogRefresh;
    private long renderedModCompletion;
    private LauncherTransferState.Snapshot shownAppTransfer;
    private final Runnable transferUiTick = new Runnable() {
        @Override public void run() {
            if (isFinishing() || isDestroyed()) return;
            refreshTransferUi();
            message.postDelayed(this, 250);
        }
    };
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

    public static void markGameMenuReady(Activity activity) {
        long attempt = activity.getIntent().getLongExtra(LAUNCH_ATTEMPT, 0L);
        SharedPreferences prefs = activity.getSharedPreferences(LAUNCHER_PREFS, MODE_PRIVATE);
        if (attempt != 0L && attempt == prefs.getLong(LAUNCH_ATTEMPT, 0L)) {
            prefs.edit().remove(LAUNCH_ATTEMPT).remove(LAUNCH_PID).apply();
        }
    }

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
        boolean wide = getResources().getConfiguration().screenWidthDp >= 620;
        LinearLayout layout = stack();
        layout.setBackgroundColor(CANVAS);
        LinearLayout header = new LinearLayout(this);
        header.setOrientation(wide ? LinearLayout.HORIZONTAL : LinearLayout.VERTICAL);
        header.setGravity(Gravity.CENTER_VERTICAL);
        header.setPadding(dp(20), dp(6), dp(16), dp(6));
        header.setBackgroundColor(PANEL);
        TextView title = uiText("OPENBONK", 22, GOLD);
        title.setTypeface(DISPLAY);
        title.setLetterSpacing(0.045f);
        header.addView(title, new LinearLayout.LayoutParams(wide ? dp(154) : -1, dp(38)));
        title.setGravity(Gravity.CENTER_VERTICAL);
        LinearLayout tabs = new LinearLayout(this);
        header.addView(tabs, new LinearLayout.LayoutParams(wide ? 0 : -1, dp(44), wide ? 1 : 0));
        layout.addView(header);
        divider(layout, 0);
        ScrollView scroll = new ScrollView(this);
        scroll.setFillViewport(true);
        scroll.setClipToPadding(false);
        scroll.setVerticalScrollBarEnabled(false);
        layout.addView(scroll, new LinearLayout.LayoutParams(-1, 0, 1));
        LinearLayout body = new LinearLayout(this);
        body.setOrientation(wide ? LinearLayout.HORIZONTAL : LinearLayout.VERTICAL);
        body.setGravity(Gravity.TOP);
        body.setPadding(dp(16), dp(12), dp(16), dp(16));
        scroll.addView(body, new ScrollView.LayoutParams(-1, -2));

        android.widget.FrameLayout art = new android.widget.FrameLayout(this);
        GradientDrawable artBackground = new GradientDrawable(GradientDrawable.Orientation.TL_BR,
            new int[]{0xff4e295e, PANEL, CANVAS});
        artBackground.setCornerRadius(dp(16));
        art.setBackground(artBackground);
        ImageView mascot = new ImageView(this);
        int mascotId = getResources().getIdentifier("openbonk_mascot", "drawable", getPackageName());
        if (mascotId != 0) mascot.setImageResource(mascotId);
        else try (InputStream in = getAssets().open("openbonk-diagnostic/openbonk_mascot.png")) {
            mascot.setImageDrawable(Drawable.createFromStream(in, null));
        } catch (IOException ignored) { }
        mascot.setScaleType(ImageView.ScaleType.FIT_CENTER);
        mascot.setPadding(dp(6), dp(8), dp(6), dp(8));
        art.addView(mascot, new android.widget.FrameLayout.LayoutParams(-1, -1));
        body.addView(art, new LinearLayout.LayoutParams(wide ? 0 : -1, dp(wide ? 232 : 140), wide ? 0.85f : 0));

        LinearLayout left = stack();
        LinearLayout.LayoutParams contentParams = new LinearLayout.LayoutParams(wide ? 0 : -1, -2, wide ? 1.48f : 0);
        body.addView(left, contentParams);
        LinearLayout right = stack();
        addColumn(body, right, wide, 0.95f, 12);
        LinearLayout dataCard = card(); left.addView(dataCard);
        LinearLayout dataHeading = new LinearLayout(this); dataHeading.setGravity(Gravity.CENTER_VERTICAL);
        dataHeading.addView(cardTitle(es ? "Datos del juego" : "Game data"), new LinearLayout.LayoutParams(0, -2, 1));
        Button dataHelp = new Button(this); dataHelp.setText("?"); dataHelp.setContentDescription(es ? "Cómo preparar los datos del juego" : "How to prepare game data");
        dataHelp.setOnClickListener(v -> showGameDataHelp()); styleButton(dataHelp, false);
        dataHeading.addView(dataHelp, new LinearLayout.LayoutParams(dp(40), dp(36))); dataCard.addView(dataHeading);
        message = new TextView(this); message.setTextColor(MUTED); message.setTextSize(12); message.setPadding(0, dp(3), 0, dp(4));
        message.setText(es ? "Comprobando datos…" : "Checking data…");
        dataCard.addView(message);
        SharedPreferences launcherPrefs = getSharedPreferences(LAUNCHER_PREFS, MODE_PRIVATE);
        collectStartupReport(launcherPrefs);
        controlChanged = launcherPrefs.contains(PENDING_CONTROL);
        touchMode = controlChanged ? launcherPrefs.getInt(PENDING_CONTROL, readTouchMode()) : readTouchMode();
        controls = new RadioGroup(this); controls.setOrientation(RadioGroup.HORIZONTAL); controls.setGravity(Gravity.START);
        RadioButton controller = new RadioButton(this); controller.setId(View.generateViewId()); controller.setText(es ? "Mando" : "Controller");
        RadioButton touch = new RadioButton(this); touch.setId(View.generateViewId()); touch.setText(es ? "Táctil" : "Touch");
        ColorStateList tint = ColorStateList.valueOf(Color.rgb(238, 231, 244));
        controller.setTextColor(tint); controller.setButtonTintList(tint); touch.setTextColor(tint); touch.setButtonTintList(tint);
        controller.setTextSize(13); touch.setTextSize(13); styleControlOption(controller); styleControlOption(touch);
        controller.setPadding(dp(5), 0, dp(5), 0); touch.setPadding(dp(5), 0, dp(5), 0);
        addOption(controls, controller); addOption(controls, touch); controls.check(touchMode == 1 ? touch.getId() : controller.getId());
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
        styleButton(chooseSteam, true); dataCard.addView(chooseSteam, buttonParams());
        dataCard.addView(choose, buttonParams());
        LinearLayout inputCard = card(); left.addView(inputCard);
        inputCard.addView(cardTitle(es ? "Modo de control" : "Input mode"));
        TextView controlHint = new TextView(this); controlHint.setTextColor(MUTED); controlHint.setTextSize(12);
        controlHint.setText(es ? "Elegí el modo de control para la partida." : "Choose the control mode for your run.");
        inputCard.addView(controlHint);
        inputCard.addView(controls, buttonParams());
        touchSettingsPanel = LauncherTouchPanel.create(this, es); inputCard.addView(touchSettingsPanel);
        touchSettingsPanel.setVisibility(touchMode == 1 ? View.VISIBLE : View.GONE);
        chooseProfile = new Button(this); chooseProfile.setText(es ? "Importar perfil ZIP" : "Import profile ZIP");
        chooseProfile.setEnabled(false); chooseProfile.setOnClickListener(v -> confirmProfilePicker());
        divider(dataCard, 7);
        dataCard.addView(uiText(es ? "Progreso guardado" : "Save profile", 13, MUTED));
        LinearLayout profileActions = new LinearLayout(this);
        dataCard.addView(profileActions, buttonParams());
        styleButton(chooseProfile, false);
        addAction(profileActions, chooseProfile, 0);
        exportCurrentProfile = new Button(this); exportCurrentProfile.setText(es ? "Exportar perfil" : "Export profile");
        exportCurrentProfile.setEnabled(false); exportCurrentProfile.setOnClickListener(v -> chooseCurrentProfile());
        styleButton(exportCurrentProfile, false); addAction(profileActions, exportCurrentProfile, 6);
        LinearLayout modPage = new LinearLayout(this); modPage.setOrientation(LinearLayout.VERTICAL); left.addView(modPage);
        modCard = card(); modPage.addView(modCard);
        catalogCard = card(); modPage.addView(catalogCard); catalogCard.setVisibility(View.GONE);
        modCard.addView(cardTitle("Mods"));
        modHint = uiText(es ? "Los cambios se aplican al iniciar el juego." : "Changes apply when you launch the game.", 13, MUTED);
        modHint.setPadding(0, dp(3), 0, 0);
        modCard.addView(modHint);
        importModButton = new Button(this); importModButton.setText(es ? "Importar ZIP" : "Import ZIP"); importModButton.setEnabled(false); importModButton.setOnClickListener(v -> openModPicker()); styleButton(importModButton, false);
        onlineMods = new Button(this); onlineMods.setText(es ? "Buscar mods" : "Browse mods");
        onlineMods.setOnClickListener(v -> LauncherOnlineUi.browse(this, es)); styleButton(onlineMods, true);
        mods = new Button(this); mods.setText(es ? "Gestionar mods" : "Manage mods");
        mods.setEnabled(false); mods.setOnClickListener(v -> showMods());
        styleButton(mods, false);
        addActionRow(modCard, es ? "Catálogo de OpenBonk" : "OpenBonk catalog",
            es ? "Encontrá e importá mods publicados por la comunidad." : "Find and import community mods.", onlineMods);
        addActionRow(modCard, es ? "En este dispositivo" : "Installed on this device",
            es ? "Activá, desactivá o restaurá mods instalados." : "Enable, disable, or restore installed mods.", mods);
        addActionRow(modCard, es ? "Archivo local" : "Local file",
            es ? "Instalá un mod desde un ZIP guardado en tu dispositivo." : "Install a mod from a ZIP on your device.", importModButton);
        LinearLayout settingsCard = card(); left.addView(settingsCard);
        settingsCard.addView(cardTitle(es ? "Resolución del juego" : "Game resolution"));
        settingsCard.addView(uiText(es ? "Elegí una escala o una altura fija." : "Choose a scale or a fixed height.", 13, MUTED));
        RadioGroup resolution = new RadioGroup(this); resolution.setOrientation(RadioGroup.HORIZONTAL);
        RadioGroup resolutionPresets = new RadioGroup(this); resolutionPresets.setOrientation(RadioGroup.HORIZONTAL);
        String[] resolutionNames = es ? new String[] { "Juego", "Nativa", "75%", "50%" } : new String[] { "Game", "Native", "75%", "50%" };
        int[] resolutionValues = {0, 100, 75, 50}; int savedResolution = launcherPrefs.getInt("render_resolution_percent", 0);
        for (int i = 0; i < resolutionValues.length; i++) {
            final int value = resolutionValues[i]; RadioButton option = new RadioButton(this); option.setId(View.generateViewId()); option.setText(resolutionNames[i]);
            option.setTextColor(Color.WHITE); option.setTextSize(12); option.setButtonTintList(ColorStateList.valueOf(LIME)); styleControlOption(option);
            addOption(resolution, option); if (value == savedResolution) resolution.check(option.getId());
            option.setOnClickListener(v -> { resolutionPresets.clearCheck(); launcherPrefs.edit().putInt("render_resolution_percent", value).apply(); });
        }
        settingsCard.addView(resolution, buttonParams());
        int[] presetHeights = {540, 720, 1080, 1440};
        for (int height : presetHeights) {
            RadioButton option = new RadioButton(this); option.setId(View.generateViewId()); option.setText(height + "p");
            option.setTextColor(Color.WHITE); option.setTextSize(12); option.setButtonTintList(ColorStateList.valueOf(LIME)); styleControlOption(option);
            addOption(resolutionPresets, option);
            if (savedResolution == -height) resolutionPresets.check(option.getId());
            option.setOnClickListener(v -> { resolution.clearCheck(); launcherPrefs.edit().putInt("render_resolution_percent", -height).apply(); });
        }
        if (resolution.getCheckedRadioButtonId() == -1 && resolutionPresets.getCheckedRadioButtonId() == -1 && !(savedResolution <= -240 && savedResolution >= -2160))
            resolution.check(((RadioButton)resolution.getChildAt(0)).getId());
        settingsCard.addView(resolutionPresets, buttonParams());
        LinearLayout customRow = new LinearLayout(this); customRow.setOrientation(LinearLayout.HORIZONTAL); customRow.setGravity(Gravity.CENTER_VERTICAL);
        android.widget.EditText customHeight = new android.widget.EditText(this); customHeight.setSingleLine(true); customHeight.setInputType(android.text.InputType.TYPE_CLASS_NUMBER);
        styleField(customHeight);
        customHeight.setHint(es ? "Altura personalizada (240–2160)" : "Custom height (240–2160)");
        if (savedResolution <= -240 && savedResolution >= -2160) customHeight.setText(Integer.toString(-savedResolution));
        customRow.addView(customHeight, new LinearLayout.LayoutParams(0, -1, 1));
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
        LinearLayout.LayoutParams applyParams = new LinearLayout.LayoutParams(dp(96), -1);
        applyParams.leftMargin = dp(6);
        customRow.addView(applyHeight, applyParams);
        settingsCard.addView(customRow, buttonParams());
        TextView resolutionHint = new TextView(this); resolutionHint.setTextColor(MUTED); resolutionHint.setTextSize(12);
        resolutionHint.setText(es ? "Se aplica al iniciar y conserva la proporción de la pantalla. Juego usa el ajuste del menú del juego." : "Applies on launch and keeps the screen's aspect ratio. Game uses the in-game setting."); settingsCard.addView(resolutionHint);
        divider(settingsCard, 9);
        android.widget.CheckBox autoUpdates = new android.widget.CheckBox(this); styleCheckbox(autoUpdates);
        autoUpdates.setText(es ? "Buscar actualizaciones automáticamente" : "Check for updates automatically");
        autoUpdates.setChecked(launcherPrefs.getBoolean("auto_update_checks", true));
        autoUpdates.setOnCheckedChangeListener((v, checked) -> launcherPrefs.edit().putBoolean("auto_update_checks", checked).apply()); settingsCard.addView(autoUpdates);
        LinearLayout updatePage = new LinearLayout(this);
        updatePage.setOrientation(wide ? LinearLayout.HORIZONTAL : LinearLayout.VERTICAL);
        left.addView(updatePage);
        LinearLayout updateCard = card();
        addColumn(updatePage, updateCard, wide, 0.78f, 0);
        updateCard.addView(cardTitle(es ? "Versión de OpenBonk" : "OpenBonk version"));
        LinearLayout historyCard = card();
        addColumn(updatePage, historyCard, wide, 1.22f, 12);
        TextView installedVersion = new TextView(this); installedVersion.setTextColor(Color.WHITE); installedVersion.setTextSize(14);
        installedVersion.setText((es ? "Versión instalada: " : "Installed version: ") + installedVersion()); updateCard.addView(installedVersion);
        updateStatus = new TextView(this); updateStatus.setTextColor(MUTED); updateStatus.setTextSize(13);
        updateStatus.setText(es ? "Última versión: sin comprobar" : "Latest version: not checked"); updateCard.addView(updateStatus);
        updateButton = new Button(this); updateButton.setText(es ? "Buscar actualización" : "Check for updates");
        updateButton.setOnClickListener(v -> loadUpdateOverview()); styleButton(updateButton, false); updateCard.addView(updateButton, buttonParams());
        appUpdateAction = new BusyAction(true);
        downloadUpdateButton = appUpdateAction.button;
        downloadUpdateButton.setText(es ? "Descargar actualización" : "Download update");
        appUpdateAction.setVisibility(View.GONE); updateCard.addView(appUpdateAction, buttonParams());
        historyCard.addView(cardTitle(es ? "Últimos cambios" : "Recent releases"));
        releaseStatus = new TextView(this); releaseStatus.setTextColor(MUTED); releaseStatus.setTextSize(12);
        releaseStatus.setText(es ? "Abrí esta sección para cargar los cambios." : "Open this section to load release notes."); historyCard.addView(releaseStatus);
        releaseList = new LinearLayout(this); releaseList.setOrientation(LinearLayout.VERTICAL); historyCard.addView(releaseList);
        LinearLayout helpCard = card(); left.addView(helpCard); helpCard.addView(cardTitle(es ? "Ayuda y soporte" : "Support & help"));
        Button feedback = new Button(this); feedback.setText(es ? "Enviar comentarios" : "Send feedback");
        feedback.setOnClickListener(v -> {
            try { startActivity(new Intent(Intent.ACTION_VIEW, android.net.Uri.parse("https://github.com/martin2844/OpenBonk/issues/new/choose"))); }
            catch (Exception e) { new AlertDialog.Builder(this).setMessage(es ? "No se pudo abrir el navegador." : "Could not open the browser.").setPositiveButton("OK", null).show(); }
        });
        Button dataGuide = new Button(this);
        dataGuide.setText(es ? "Ver guía" : "View guide");
        styleButton(dataGuide, false);
        dataGuide.setOnClickListener(v -> showGameDataHelp());
        addActionRow(helpCard, es ? "Archivos y progreso" : "Game files and saves",
            es ? "Qué archivo elegir y cómo transferir tu progreso." : "Which file to choose and how to move your saves.", dataGuide);
        styleButton(feedback, true);
        addActionRow(helpCard, es ? "Comentarios" : "Feedback",
            es ? "Reportá un problema o sugerí una mejora en GitHub." : "Report a problem or suggest an improvement on GitHub.", feedback);
        String startupReport = launcherPrefs.getString(LAUNCH_REPORT, "");
        if (!startupReport.isEmpty()) {
            Button report = new Button(this);
            report.setText(es ? "Ver informe" : "View report");
            styleButton(report, false);
            report.setOnClickListener(v -> showStartupReport(startupReport));
            addActionRow(helpCard, es ? "Error de inicio" : "Startup failure",
                es ? "Datos de diagnóstico del último error al iniciar." : "Details from the last failed startup.", report);
        }
        if (hasTextureNotices()) {
            Button notices = new Button(this); notices.setText(es ? "Avisos" : "Notices");
            notices.setOnClickListener(v -> showTextureNotices()); styleButton(notices, false);
            addActionRow(helpCard, es ? "Créditos" : "Credits",
                es ? "Avisos y licencias de terceros." : "Third-party notices and licenses.", notices);
        }
        LinearLayout[] pages = { dataCard, inputCard, modPage, settingsCard, updatePage, helpCard };
        String[] tabNames = es ? new String[] { "Juego", "Controles", "Mods", "Ajustes", "Actualizar", "Ayuda" } : new String[] { "Game", "Controls", "Mods", "Settings", "Updates", "Help" };
        Button[] tabButtons = new Button[pages.length];
        for (int i = 0; i < pages.length; i++) {
            final int index = i;
            Button tab = new Button(this);
            tab.setText(tabNames[i]);
            tabButtons[i] = tab;
            if (i == 4) updatesTab = tab;
            tabs.addView(tab, new LinearLayout.LayoutParams(0, -1, 1));
            tab.setOnClickListener(v -> {
                boolean game = index == 0;
                art.setVisibility(game ? View.VISIBLE : View.GONE);
                right.setVisibility(game ? View.VISIBLE : View.GONE);
                contentParams.setMarginStart(wide && game ? dp(12) : 0);
                contentParams.topMargin = !wide && game ? dp(12) : 0;
                left.setLayoutParams(contentParams);
                for (int n = 0; n < pages.length; n++) {
                    pages[n].setVisibility(n == index ? View.VISIBLE : View.GONE);
                    tabButtons[n].setSelected(n == index);
                }
                scroll.scrollTo(0, 0);
                if (index == 4 && updateRequest == 0) loadUpdateOverview();
            });
            styleTab(tab);
        }

        LinearLayout gameStatusCard = card();
        right.addView(gameStatusCard);
        gameStatusCard.addView(cardTitle(es ? "Iniciar partida" : "Launch game"));
        play = new Button(this);
        play.setText(es ? "▶   Jugar" : "▶   Play");
        play.setEnabled(false);
        play.setOnClickListener(v -> launchGame());
        stylePlay(play);
        LinearLayout.LayoutParams playParams = new LinearLayout.LayoutParams(-1, dp(49));
        playParams.topMargin = dp(6);
        gameStatusCard.addView(play, playParams);
        divider(gameStatusCard, 10);
        gameDataStatus = uiText(es ? "◌  Comprobando datos…" : "◌  Checking game files…", 13, MUTED);
        gameStatusCard.addView(gameDataStatus);
        TextView gameVersion = uiText((es ? "Versión " : "Version ") + installedVersion(), 12, MUTED);
        gameVersion.setPadding(0, dp(4), 0, 0);
        gameStatusCard.addView(gameVersion);

        setContentView(layout);
        (LauncherOnlineUi.UPDATE.snapshot().busy ? tabButtons[4] : tabButtons[0]).performClick();
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
    private LinearLayout stack() {
        LinearLayout view = new LinearLayout(this);
        view.setOrientation(LinearLayout.VERTICAL);
        return view;
    }

    TextView uiText(String text, int size, int color) {
        TextView view = new TextView(this);
        view.setText(text);
        view.setTextSize(size);
        view.setTextColor(color);
        return view;
    }

    private GradientDrawable shape(int fill, int stroke) {
        GradientDrawable drawable = new GradientDrawable();
        drawable.setColor(fill);
        drawable.setCornerRadius(dp(8));
        if (stroke != Color.TRANSPARENT) drawable.setStroke(dp(1), stroke);
        return drawable;
    }

    private LinearLayout card() {
        LinearLayout view = stack();
        view.setPadding(dp(14), dp(12), dp(14), dp(12));
        view.setBackground(shape(PANEL, LINE));
        return view;
    }

    TextView cardTitle(String text) {
        TextView view = uiText(text, 18, INK);
        view.setTypeface(DISPLAY);
        view.setPadding(0, 0, 0, dp(3));
        return view;
    }

    void divider(LinearLayout parent, int margin) {
        View rule = new View(this);
        rule.setBackgroundColor(LINE);
        rule.setImportantForAccessibility(View.IMPORTANT_FOR_ACCESSIBILITY_NO);
        LinearLayout.LayoutParams params = new LinearLayout.LayoutParams(-1, dp(1));
        params.topMargin = params.bottomMargin = dp(margin);
        parent.addView(rule, params);
    }

    private void addColumn(LinearLayout parent, View view, boolean wide, float weight, int gap) {
        LinearLayout.LayoutParams params = new LinearLayout.LayoutParams(wide ? 0 : -1, -2, wide ? weight : 0);
        if (wide) params.setMarginStart(dp(gap));
        else params.topMargin = dp(gap);
        parent.addView(view, params);
    }

    private void addActionRow(LinearLayout parent, String title, String description, Button action) {
        divider(parent, 7);
        boolean inline = getResources().getConfiguration().screenWidthDp >= 440;
        LinearLayout row = new LinearLayout(this);
        row.setOrientation(inline ? LinearLayout.HORIZONTAL : LinearLayout.VERTICAL);
        row.setGravity(Gravity.CENTER_VERTICAL);
        LinearLayout labels = stack();
        TextView label = uiText(title, 14, INK);
        label.setTypeface(MEDIUM);
        labels.addView(label);
        labels.addView(uiText(description, 12, MUTED));
        row.addView(labels, new LinearLayout.LayoutParams(inline ? 0 : -1, -2, inline ? 1 : 0));
        LinearLayout.LayoutParams params = new LinearLayout.LayoutParams(inline ? dp(154) : -1, dp(38));
        if (inline) params.setMarginStart(dp(12));
        else params.topMargin = dp(5);
        row.addView(action, params);
        parent.addView(row);
    }

    private void addAction(LinearLayout row, Button button, int gap) {
        LinearLayout.LayoutParams params = new LinearLayout.LayoutParams(0, -1, 1);
        params.setMarginStart(dp(gap));
        row.addView(button, params);
    }

    LinearLayout.LayoutParams buttonParams() {
        LinearLayout.LayoutParams params = new LinearLayout.LayoutParams(-1, dp(38));
        params.topMargin = dp(5);
        return params;
    }

    private ColorStateList buttonText(int active) {
        return new ColorStateList(new int[][]{{-android.R.attr.state_enabled}, {}},
            new int[]{0xff98869f, active});
    }

    private void configureButton(Button button) {
        button.setTextSize(13);
        button.setTypeface(MEDIUM);
        button.setAllCaps(false);
        button.setMinHeight(0);
        button.setMinimumHeight(0);
        button.setPadding(dp(8), 0, dp(8), 0);
        button.setStateListAnimator(null);
    }

    private void actionStyle(Button button, int fill, int outline, int text) {
        configureButton(button);
        button.setTextColor(buttonText(text));
        StateListDrawable states = new StateListDrawable();
        states.addState(new int[]{-android.R.attr.state_enabled}, shape(0xff2c2235, LINE));
        states.addState(new int[]{android.R.attr.state_focused}, shape(fill, LIME));
        states.addState(new int[]{android.R.attr.state_pressed}, shape(fill, GOLD));
        states.addState(new int[]{}, shape(fill, outline));
        button.setBackground(states);
    }

    void styleButton(Button button, boolean primary) {
        actionStyle(button, primary ? LIME : INSET, primary ? Color.TRANSPARENT : LINE, primary ? CANVAS : INK);
    }

    void styleUpdateButton(Button button) {
        actionStyle(button, UPDATE_ORANGE, Color.TRANSPARENT, CANVAS);
    }

    private final class BusyAction extends FrameLayout {
        final Button button;
        final ProgressBar spinner;
        BusyAction(boolean update) {
            super(SplitDataLauncher.this);
            button = new Button(SplitDataLauncher.this);
            if (update) styleUpdateButton(button); else styleButton(button, false);
            addView(button, new FrameLayout.LayoutParams(-1, -1));
            spinner = new ProgressBar(SplitDataLauncher.this, null, android.R.attr.progressBarStyleSmall);
            spinner.setIndeterminate(true);
            spinner.setIndeterminateTintList(ColorStateList.valueOf(UPDATE_ORANGE));
            spinner.setImportantForAccessibility(View.IMPORTANT_FOR_ACCESSIBILITY_NO);
            FrameLayout.LayoutParams progress = new FrameLayout.LayoutParams(dp(18), dp(18), Gravity.START | Gravity.CENTER_VERTICAL);
            progress.setMarginStart(dp(9)); addView(spinner, progress);
            spinner.setVisibility(View.GONE);
        }
        void bind(boolean busy, boolean enabled, String label) {
            if (!label.contentEquals(button.getText())) button.setText(label);
            button.setEnabled(enabled && !busy);
            button.setPadding(dp(busy ? 32 : 8), 0, dp(8), 0);
            spinner.setVisibility(busy ? View.VISIBLE : View.GONE);
            button.setContentDescription(label);
        }
    }

    private final class CatalogAction {
        final BusyAction action;
        final TextView details;
        final String description, label;
        CatalogAction(BusyAction action, TextView details, String description, String label) {
            this.action = action; this.details = details; this.description = description; this.label = label;
        }
    }

    private String transferStatus(LauncherTransferState.Snapshot state, boolean app) {
        switch (state.phase) {
            case DOWNLOAD: return es ? "Descargando…" : "Downloading…";
            case VERIFY: return es ? "Verificando…" : "Verifying…";
            case INSTALL: return app ? (es ? "Abriendo instalador…" : "Opening installer…")
                : (es ? "Instalando…" : "Installing…");
            case FAILED: return (es ? "Falló. Intentá de nuevo: " : "Failed. Try again: ") + state.failure;
            case DONE: return app ? (es ? "Descarga lista. Android debe confirmar la instalación." : "Download ready. Android must confirm installation.")
                : (es ? "Instalado" : "Installed");
            default: return "";
        }
    }

    void showAppUpdateProgress() {
        updatesTab.performClick();
        refreshTransferUi();
    }

    void refreshTransferUi() {
        if (appUpdateAction == null) return;
        LauncherTransferState.Snapshot app = LauncherOnlineUi.UPDATE.snapshot();
        if (app != shownAppTransfer) {
            shownAppTransfer = app;
            if (app.phase != LauncherTransferState.Phase.IDLE) {
                appUpdateAction.setVisibility(View.VISIBLE);
                appUpdateAction.bind(app.busy, true, app.busy
                    ? (es ? "Actualizando…" : "Updating…")
                    : (es ? "Descargar actualización" : "Download update"));
                updateStatus.setText(transferStatus(app, true));
                JSONObject info = LauncherOnlineUi.updateInfo();
                if (info != null) downloadUpdateButton.setOnClickListener(v -> LauncherOnlineUi.downloadUpdate(this, es, info));
            }
        }
        updateButton.setEnabled(!app.busy && !IMPORT_GATE.isBusy());
        LauncherTransferState.Snapshot mod = MOD_TRANSFER.snapshot();
        if (mod.phase == LauncherTransferState.Phase.DONE && mod.generation != renderedModCompletion) {
            renderedModCompletion = mod.generation;
            if (catalogRefresh != null) catalogRefresh.run();
        }
        for (java.util.Map.Entry<String, CatalogAction> entry : catalogActions.entrySet()) {
            CatalogAction row = entry.getValue();
            boolean thisMod = entry.getKey().equals(mod.id), busy = thisMod && mod.busy;
            row.action.bind(busy, !IMPORT_GATE.isBusy(), busy ? (es ? "Espera…" : "Wait…") : row.label);
            String detail = thisMod && (mod.busy || mod.phase == LauncherTransferState.Phase.FAILED)
                ? transferStatus(mod, false) : row.description;
            if (!detail.contentEquals(row.details.getText())) row.details.setText(detail);
        }
    }

    private void stylePlay(Button button) {
        actionStyle(button, GOLD, Color.TRANSPARENT, CANVAS);
        button.setTextSize(19);
        button.setTypeface(DISPLAY);
    }

    private void styleTab(Button button) {
        configureButton(button);
        button.setPadding(dp(2), 0, dp(2), 0);
        button.setTextColor(new ColorStateList(new int[][]{{android.R.attr.state_selected}, {}}, new int[]{GOLD, MUTED}));
        StateListDrawable states = new StateListDrawable();
        states.addState(new int[]{android.R.attr.state_focused}, shape(INSET, LIME));
        states.addState(new int[]{android.R.attr.state_selected}, shape(INSET, Color.TRANSPARENT));
        states.addState(new int[]{android.R.attr.state_pressed}, shape(INSET, LINE));
        states.addState(new int[]{}, shape(Color.TRANSPARENT, Color.TRANSPARENT));
        button.setBackground(states);
    }

    private void styleField(android.widget.EditText field) {
        field.setTextColor(INK);
        field.setHintTextColor(MUTED);
        field.setTextSize(13);
        StateListDrawable states = new StateListDrawable();
        states.addState(new int[]{android.R.attr.state_focused}, shape(CANVAS, LIME));
        states.addState(new int[]{}, shape(CANVAS, LINE));
        field.setBackground(states);
        field.setPadding(dp(10), 0, dp(10), 0);
    }

    private void styleCheckbox(android.widget.CheckBox checkbox) {
        checkbox.setTextColor(INK);
        checkbox.setTextSize(13);
        checkbox.setButtonTintList(ColorStateList.valueOf(LIME));
        StateListDrawable states = new StateListDrawable();
        states.addState(new int[]{android.R.attr.state_focused}, shape(INSET, LIME));
        states.addState(new int[]{}, shape(Color.TRANSPARENT, Color.TRANSPARENT));
        checkbox.setBackground(states);
        checkbox.setMinHeight(dp(38));
    }

    private void addOption(RadioGroup group, RadioButton option) {
        RadioGroup.LayoutParams params = new RadioGroup.LayoutParams(0, -1, 1);
        if (group.getChildCount() > 0) params.setMarginStart(dp(6));
        group.addView(option, params);
    }

    private void styleControlOption(RadioButton option) {
        option.setButtonDrawable((Drawable)null);
        option.setGravity(Gravity.CENTER);
        option.setTypeface(MEDIUM);
        option.setTextColor(new ColorStateList(new int[][]{{android.R.attr.state_checked}, {}}, new int[]{LIME, INK}));
        StateListDrawable states = new StateListDrawable();
        states.addState(new int[]{android.R.attr.state_focused}, shape(INSET, LIME));
        states.addState(new int[]{android.R.attr.state_checked}, shape(0xff3b3046, LIME));
        states.addState(new int[]{}, shape(INSET, LINE));
        option.setBackground(states);
    }

    @Override protected void onResume() {
        super.onResume();
        if (message != null) { message.removeCallbacks(transferUiTick); transferUiTick.run(); }
        if (touchSettingsPanel != null && touchSettingsPanel.getParent() instanceof android.view.ViewGroup) {
            android.view.ViewGroup parent = (android.view.ViewGroup)touchSettingsPanel.getParent();
            int position = parent.indexOfChild(touchSettingsPanel); parent.removeView(touchSettingsPanel);
            touchSettingsPanel = LauncherTouchPanel.create(this, es); parent.addView(touchSettingsPanel, position);
            touchSettingsPanel.setVisibility(touchMode == 1 ? View.VISIBLE : View.GONE);
        }
    }

    @Override protected void onPause() {
        if (message != null) message.removeCallbacks(transferUiTick);
        super.onPause();
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

    void showDialog(AlertDialog dialog) {
        dialog.setOnKeyListener((d, code, event) -> {
            KeyEvent mapped = swappedKey(event);
            return mapped != null && dialog.dispatchKeyEvent(mapped);
        });
        dialog.show();
        if (dialog.getWindow() != null) dialog.getWindow().setBackgroundDrawable(
            new android.graphics.drawable.InsetDrawable(shape(PANEL, LINE), dp(6)));
        TextView message = dialog.findViewById(android.R.id.message);
        if (message != null) {
            message.setTextSize(14);
            message.setTextColor(MUTED);
            message.setTypeface(Typeface.DEFAULT);
        }
        for (int which : new int[]{AlertDialog.BUTTON_POSITIVE, AlertDialog.BUTTON_NEGATIVE, AlertDialog.BUTTON_NEUTRAL}) {
            Button button = dialog.getButton(which);
            if (button != null) {
                button.setAllCaps(false);
                button.setTextColor(which == AlertDialog.BUTTON_POSITIVE ? LIME : INK);
            }
        }
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
            ArrayList<String> names = new ArrayList<>();
            ArrayList<Boolean> defaults = new ArrayList<>();
            ArrayList<Boolean> usesLegacyPreference = new ArrayList<>();
            for (int i = 0; i < folders.length; i++) {
                JSONObject manifest = readInstalledMod(folders[i]);
                if (manifest == null) continue;
                boolean content = (manifest.optInt("api") == 3 || manifest.optInt("api") == 4);
                ids.add(folders[i].getName());
                names.add(modDisplayName(manifest, folders[i].getName()));
                defaults.add(!content && manifest.optBoolean("enabled", false));
                usesLegacyPreference.add(!content);
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
            catalogActions.clear(); catalogRefresh = null;
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
                    choice.setText(names.get(i)); choice.setTextColor(Color.WHITE); choice.setTextSize(14); choice.setSingleLine(true); choice.setEllipsize(android.text.TextUtils.TruncateAt.END);
                    styleCheckbox(choice); choice.setChecked(selected[i]);
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

    private static String modDisplayName(JSONObject manifest, String id) {
        if (manifest == null) return id;
        JSONObject map = manifest.optJSONObject("map");
        String name = map == null ? manifest.optString("name", "") : map.optString("name", "");
        name = name.trim();
        return name.isEmpty() ? id : name;
    }

    private String installedModDisplayName(String id) {
        File root = getExternalFilesDir(null);
        return root == null || id == null ? id : modDisplayName(readInstalledMod(new File(new File(root, "Mods"), id)), id);
    }

    private JSONObject readInstalledMod(File folder) {
        File file = new File(folder, "manifest.json");
        if (!file.isFile() || file.length() > 65536 || !new File(folder, "main.lua").isFile()) return null;
        try (InputStream in = new FileInputStream(file); ByteArrayOutputStream bytes = new ByteArrayOutputStream()) {
            byte[] buffer = new byte[4096];
            int read;
            while ((read = in.read(buffer)) != -1) {
                if (bytes.size() + read > 65536) return null;
                bytes.write(buffer, 0, read);
            }
            JSONObject manifest = new JSONObject(new String(bytes.toByteArray(), StandardCharsets.UTF_8));
            if (!folder.getName().equals(manifest.optString("id"))) return null;
            boolean content = (manifest.optInt("api") == 3 || manifest.optInt("api") == 4) && "content".equals(manifest.optString("kind"));
            return content || validModHooks(manifest) ? manifest : null;
        } catch (Exception ignored) { return null; }
    }

    // Content mods are ready to play as soon as they are imported or updated;
    // Lua mods keep their manifest default.
    private void enableContentMod(File root, String id) {
        if (root == null || id == null) return;
        JSONObject manifest = readInstalledMod(new File(new File(root, "Mods"), id));
        if (manifest != null && (manifest.optInt("api") == 3 || manifest.optInt("api") == 4))
            getSharedPreferences("megabonk_mod_state", MODE_PRIVATE).edit().putBoolean(id, true).commit();
    }

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
        gameDataStatus.setTextColor(ready ? LIME : Color.rgb(255, 184, 153));
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
        mods.setEnabled(true); importModButton.setEnabled(true); onlineMods.setEnabled(true); updateButton.setEnabled(!LauncherOnlineUi.UPDATE.snapshot().busy);
        (ready ? play : LauncherDiagnosticIntegration.available(this) ? chooseSteam : choose).requestFocus();
    }

    private void setImportBusy() {
        getWindow().addFlags(WindowManager.LayoutParams.FLAG_KEEP_SCREEN_ON);
        gameDataStatus.setText(es ? "◌  Preparando archivos…" : "◌  Preparing files…"); gameDataStatus.setTextColor(MUTED);
        choose.setEnabled(false); chooseSteam.setEnabled(false); chooseProfile.setEnabled(false); exportCurrentProfile.setEnabled(false);
        play.setEnabled(false); mods.setEnabled(false); importModButton.setEnabled(false); onlineMods.setEnabled(false); updateButton.setEnabled(false);
    }

    private String installedVersion() {
        try { String version = getPackageManager().getPackageInfo(getPackageName(), 0).versionName; return version == null || version.isEmpty() ? "—" : version; }
        catch (Exception e) { return "—"; }
    }

    private void loadUpdateOverview() {
        if (LauncherOnlineUi.UPDATE.snapshot().busy) { refreshTransferUi(); return; }
        final int request = ++updateRequest;
        final long transferGeneration = LauncherOnlineUi.UPDATE.snapshot().generation;
        updateStatus.setText(es ? "Buscando la última versión…" : "Checking the latest version…");
        releaseStatus.setVisibility(View.VISIBLE); releaseStatus.setText(es ? "Cargando novedades…" : "Loading release notes…"); releaseList.removeAllViews();
        appUpdateAction.setVisibility(View.GONE);
        new Thread(() -> {
            try {
                JSONObject info = LauncherOnline.metadata(LauncherOnline.UPDATES);
                long remote = info.getLong("version_code");
                if (remote <= 0 || info.optString("tag").isEmpty()) throw new IOException("invalid update metadata");
                long installed = getPackageManager().getPackageInfo(getPackageName(), 0).versionCode;
                runOnUiThread(() -> {
                    if (isFinishing() || isDestroyed() || request != updateRequest) return;
                    String status = remote > installed ? (es ? "Actualización disponible" : "Update available") : (es ? "Estás al día" : "Up to date");
                    if (LauncherOnlineUi.UPDATE.snapshot().generation != transferGeneration) { refreshTransferUi(); return; }
                    updateStatus.setText((es ? "Última versión: " : "Latest version: ") + info.optString("tag") + "  ·  " + status);
                    if (remote > installed) {
                        appUpdateAction.setVisibility(View.VISIBLE);
                        downloadUpdateButton.setOnClickListener(v -> LauncherOnlineUi.downloadUpdate(this, es, info));
                    }
                });
            } catch (Exception e) { runOnUiThread(() -> {
                if (!isFinishing() && !isDestroyed() && request == updateRequest && LauncherOnlineUi.UPDATE.snapshot().generation == transferGeneration) updateStatus.setText(es ? "No se pudo comprobar la última versión. Intentá de nuevo." : "Could not check the latest version. Try again.");
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
            divider(releaseList, 8);
            LinearLayout releaseHeader = new LinearLayout(this);
            releaseHeader.setGravity(Gravity.CENTER_VERTICAL);
            TextView heading = uiText(tag, 15, INK);
            heading.setTypeface(MEDIUM);
            releaseHeader.addView(heading, new LinearLayout.LayoutParams(0, -2, 1));
            String date = release.optString("published_at", "");
            if (date.length() >= 10) releaseHeader.addView(uiText(date.substring(0, 10), 12, MUTED));
            releaseList.addView(releaseHeader);
            String body = release.optString("body", "").trim();
            if (body.isEmpty()) body = es ? "Sin notas para esta versión." : "No release notes provided.";
            if (body.length() > 4000) body = body.substring(0, 4000) + "…";
            String notes = body.replace("**", "").replaceAll("(?m)^#{1,6} +", "").replaceAll("(?m)^- ", "• ");
            String preview = notes.replaceFirst("(?i)^OpenBonk Android beta [^\\n]+\\s*", "");
            TextView summary = uiText(preview, 13, MUTED);
            summary.setMaxLines(2);
            summary.setEllipsize(android.text.TextUtils.TruncateAt.END);
            summary.setPadding(0, dp(3), 0, dp(5));
            releaseList.addView(summary);
            Button open = new Button(this);
            open.setText(es ? "Leer cambios" : "Read notes");
            open.setContentDescription(open.getText() + " " + tag);
            styleButton(open, false);
            open.setOnClickListener(v -> showDialog(new AlertDialog.Builder(this).setTitle(tag).setMessage(notes)
                .setNeutralButton(es ? "Ver en GitHub" : "View on GitHub", (d, w) -> openRelease(tag))
                .setPositiveButton(es ? "Cerrar" : "Close", null).create()));
            releaseList.addView(open, new LinearLayout.LayoutParams(dp(114), dp(34)));
        }
        releaseStatus.setVisibility(count == 0 ? View.VISIBLE : View.GONE);
        if (count == 0) releaseStatus.setText(es ? "No hay versiones Android disponibles." : "No Android releases found.");
    }

    private void openRelease(String tag) {
        try { startActivity(new Intent(Intent.ACTION_VIEW, android.net.Uri.parse("https://github.com/martin2844/OpenBonk/releases/tag/" + tag))); }
        catch (Exception e) {
            showDialog(new AlertDialog.Builder(this).setMessage(es ? "No se pudo abrir el navegador." : "Could not open the browser.").setPositiveButton("OK", null).create());
        }
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
                    : (es ? "Mod importado o actualizado: " : "Mod imported or updated: ")) + installedModDisplayName(installed));
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
        catalogActions.clear(); catalogRefresh = null;
        modCard.setVisibility(View.GONE); catalogCard.setVisibility(View.VISIBLE); catalogCard.removeAllViews();
        catalogCard.addView(cardTitle(es ? "Catálogo de mods" : "Mod catalog"));
        TextView text = new TextView(this); text.setTextColor(Color.WHITE); text.setText(es ? "Cargando…" : "Loading…"); catalogCard.addView(text);
    }
    void showCatalogError(String error) {
        catalogActions.clear(); catalogRefresh = null;
        modCard.setVisibility(View.GONE); catalogCard.setVisibility(View.VISIBLE); catalogCard.removeAllViews();
        catalogCard.addView(cardTitle(es ? "No se pudo cargar el catálogo" : "Could not load catalog"));
        TextView text = new TextView(this); text.setTextColor(Color.WHITE); text.setText(error); catalogCard.addView(text);
        Button back = new Button(this); back.setText(es ? "Volver" : "Back"); styleButton(back, false);
        back.setOnClickListener(v -> { catalogCard.setVisibility(View.GONE); modCard.setVisibility(View.VISIBLE); }); catalogCard.addView(back, new LinearLayout.LayoutParams(dp(106), dp(36)));
    }
    void showCatalog(JSONArray entries) {
        catalogActions.clear(); catalogRefresh = null;
        modCard.setVisibility(View.GONE); catalogCard.setVisibility(View.VISIBLE); catalogCard.removeAllViews();
        catalogCard.addView(cardTitle(es ? "Catálogo de mods" : "Mod catalog"));
        android.widget.EditText search = new android.widget.EditText(this); search.setSingleLine(true); search.setTextColor(Color.WHITE);
        styleField(search); search.setHint(es ? "Buscar mods" : "Search mods"); catalogCard.addView(search, buttonParams());
        LinearLayout results = new LinearLayout(this); results.setOrientation(LinearLayout.VERTICAL); catalogCard.addView(results);
        LinearLayout navigation = new LinearLayout(this); navigation.setGravity(Gravity.CENTER_VERTICAL);
        Button previous = new Button(this); previous.setText("‹"); previous.setContentDescription(es ? "Página anterior" : "Previous page"); styleButton(previous, false);
        TextView pageLabel = new TextView(this); pageLabel.setTextColor(Color.WHITE); pageLabel.setGravity(Gravity.CENTER);
        Button next = new Button(this); next.setText("›"); next.setContentDescription(es ? "Página siguiente" : "Next page"); styleButton(next, false);
        navigation.addView(previous, new LinearLayout.LayoutParams(dp(48), dp(37))); navigation.addView(pageLabel, new LinearLayout.LayoutParams(0, dp(37), 1)); navigation.addView(next, new LinearLayout.LayoutParams(dp(48), dp(37))); catalogCard.addView(navigation);
        int widthDp = getResources().getConfiguration().screenWidthDp;
        int columns = widthDp >= 600 ? 2 : 1;
        int[] page = {0}; int perPage = modPageSize(widthDp >= 620 ? 217 : 253, 50) * columns;
        File storage = getExternalFilesDir(null);
        File modsRoot = storage == null ? null : new File(storage, "Mods");
        Runnable[] refresh = new Runnable[1];
        Runnable render = () -> {
            catalogActions.clear();
            results.removeAllViews(); String query = search.getText().toString().trim().toLowerCase(Locale.ROOT); ArrayList<JSONObject> matches = new ArrayList<>();
            for (int i = 0; i < entries.length(); i++) {
                JSONObject item = entries.optJSONObject(i); if (item == null) continue;
                String name = item.optString("name"), description = item.optString("description");
                if (!(name + " " + description).toLowerCase(Locale.ROOT).contains(query)) continue;
                matches.add(item);
            }
            int pages = Math.max(1, (matches.size() + perPage - 1) / perPage); page[0] = Math.min(page[0], pages - 1);
            // Installed mods first so updates and enabled picks are easy to find.
            matches.sort(java.util.Comparator.comparingInt(item -> {
                String id = item.optString("id");
                return modsRoot != null && id.matches("[A-Za-z0-9_-]{1,64}") && readInstalledMod(new File(modsRoot, id)) != null ? 0 : 1;
            }));
            int start = page[0] * perPage, end = Math.min(matches.size(), start + perPage);
            LinearLayout rowGroup = null;
            for (int i = start; i < end; i++) {
                if ((i - start) % columns == 0) {
                    divider(results, 0);
                    rowGroup = new LinearLayout(this);
                    results.addView(rowGroup, new LinearLayout.LayoutParams(-1, dp(49)));
                }
                JSONObject item = matches.get(i);
                String id = item.optString("id");
                JSONObject installed = modsRoot != null && id.matches("[A-Za-z0-9_-]{1,64}") ? readInstalledMod(new File(modsRoot, id)) : null;
                String installedVersion = installed == null ? "" : installed.optString("version");
                String catalogVersion = item.optString("version");
                boolean hasUpdate = installed != null && modHasUpdate(catalogVersion, installedVersion);
                LinearLayout row = new LinearLayout(this); row.setGravity(Gravity.CENTER_VERTICAL); row.setPadding(dp(4), dp(2), dp(4), dp(2));
                LinearLayout info = new LinearLayout(this); info.setOrientation(LinearLayout.VERTICAL); row.addView(info, new LinearLayout.LayoutParams(0, -2, 1));
                TextView title = new TextView(this); title.setText(item.optString("name")); title.setTextColor(INK); title.setTypeface(MEDIUM); title.setSingleLine(true); title.setEllipsize(android.text.TextUtils.TruncateAt.END); info.addView(title);
                TextView details = new TextView(this); details.setText(item.optString("description")); details.setTextColor(MUTED); details.setTextSize(12); details.setSingleLine(true); details.setEllipsize(android.text.TextUtils.TruncateAt.END); info.addView(details);
                String installedLabel = es ? "Instalado" : "Installed";
                info.setFocusable(true);
                info.setOnClickListener(v -> {
                    String state = installed == null ? "" : installedLabel
                        + (installedVersion.isEmpty() ? "" : " " + installedVersion) + "\n";
                    AlertDialog.Builder detailsDialog = new AlertDialog.Builder(this)
                        .setTitle(item.optString("name"))
                        .setMessage(state + (es ? "Versión del catálogo: " : "Catalog version: ") + catalogVersion
                            + "\n\n" + item.optString("description"))
                        .setPositiveButton("OK", null);
                    if (installed != null && installedVersion.isEmpty()) {
                        detailsDialog.setNeutralButton(es ? "Reemplazar…" : "Replace…",
                            (dialog, which) -> confirmModReplacement(item));
                    }
                    showDialog(detailsDialog.create());
                });
                View action;
                if (installed == null || hasUpdate) {
                    BusyAction importAction = new BusyAction(hasUpdate);
                    String label = hasUpdate ? (es ? "Actualizar" : "Update") : (es ? "Importar" : "Import");
                    importAction.button.setOnClickListener(v -> importOnlineMod(item));
                    catalogActions.put(id, new CatalogAction(importAction, details, item.optString("description"), label));
                    action = importAction;
                } else {
                    TextView badge = uiText("✓ " + installedLabel, 12, LIME);
                    badge.setGravity(Gravity.CENTER);
                    badge.setTypeface(MEDIUM);
                    badge.setBackground(shape(0xff32343a, LINE));
                    action = badge;
                }
                LinearLayout.LayoutParams actionParams = new LinearLayout.LayoutParams(dp(104), dp(installed != null && !hasUpdate ? 29 : 36));
                actionParams.setMarginStart(dp(6));
                row.addView(action, actionParams);
                rowGroup.addView(row, new LinearLayout.LayoutParams(0, -1, 1));
            }
            if (matches.isEmpty()) { TextView empty = new TextView(this); empty.setTextColor(Color.WHITE); empty.setText(es ? "No se encontraron mods." : "No mods found."); results.addView(empty); }
            pageLabel.setText((page[0] + 1) + " / " + pages + "  ·  " + matches.size()); previous.setEnabled(page[0] > 0); next.setEnabled(page[0] + 1 < pages);
        };
        refresh[0] = () -> { if (results.isAttachedToWindow()) { render.run(); refreshTransferUi(); } };
        catalogRefresh = refresh[0];
        previous.setOnClickListener(v -> { if (page[0] > 0) { page[0]--; render.run(); refreshTransferUi(); } });
        next.setOnClickListener(v -> { page[0]++; render.run(); refreshTransferUi(); });
        search.addTextChangedListener(new android.text.TextWatcher() {
            public void beforeTextChanged(CharSequence text, int start, int count, int after) { }
            public void onTextChanged(CharSequence text, int start, int before, int count) { page[0] = 0; render.run(); refreshTransferUi(); }
            public void afterTextChanged(android.text.Editable text) { }
        });
        render.run();
        refreshTransferUi();
        Button back = new Button(this); back.setText(es ? "Volver" : "Back"); styleButton(back, false);
        back.setOnClickListener(v -> { catalogCard.setVisibility(View.GONE); modCard.setVisibility(View.VISIBLE); }); catalogCard.addView(back, new LinearLayout.LayoutParams(dp(106), dp(36)));
    }

    // A catalog entry newer than the installed manifest, or an installed copy
    // that never declared a version (older packages), offers an update.
    private static boolean modHasUpdate(String catalog, String installed) {
        String numericVersion = "[0-9]{1,9}\\.[0-9]{1,9}\\.[0-9]{1,9}";
        if (catalog.isEmpty()) return false;
        if (installed.isEmpty() || !installed.matches(numericVersion) || !catalog.matches(numericVersion)) return true;
        return newerModVersion(catalog, installed);
    }

    private static boolean newerModVersion(String catalog, String installed) {
        String numericVersion = "[0-9]{1,9}\\.[0-9]{1,9}\\.[0-9]{1,9}";
        if (!catalog.matches(numericVersion) || !installed.matches(numericVersion)) return false;
        String[] remote = catalog.split("\\.");
        String[] local = installed.split("\\.");
        for (int i = 0; i < remote.length; i++) {
            int comparison = Integer.compare(Integer.parseInt(remote[i]), Integer.parseInt(local[i]));
            if (comparison != 0) return comparison > 0;
        }
        return false;
    }

    private void confirmModReplacement(JSONObject item) {
        showDialog(new AlertDialog.Builder(this).setTitle(item.optString("name"))
            .setMessage(es ? "Esto reemplaza el mod instalado por la versión del catálogo." : "This replaces the installed mod with the catalog version.")
            .setNegativeButton(es ? "Cancelar" : "Cancel", null)
            .setPositiveButton(es ? "Reemplazar" : "Replace", (dialog, which) -> importOnlineMod(item)).create());
    }

    private void importOnlineMod(JSONObject item) {
        if (!IMPORT_GATE.begin()) { modHint.setText(es ? "Ya hay una importación en curso." : "An import is already in progress."); return; }
        File storage = getExternalFilesDir(null);
        String itemId = item.optString("id");
        boolean updating = storage != null && itemId.matches("[A-Za-z0-9_-]{1,64}")
            && readInstalledMod(new File(new File(storage, "Mods"), itemId)) != null;
        final long transfer = MOD_TRANSFER.begin(itemId, updating);
        setImportBusy(); modHint.setText(es ? "Descargando mod…" : "Downloading mod…");
        refreshTransferUi();
        final long generation = IMPORT_GATE.generation();
        try { runImport(() -> {
            String id = null, error = null; File file = null;
            try {
                file = File.createTempFile("openbonk-mod-", ".zip", getCacheDir());
                LauncherOnline.download(item, file, 128L * 1024 * 1024);
                MOD_TRANSFER.phase(transfer, LauncherTransferState.Phase.VERIFY);
                LauncherOnline.checkModId(file, item.getString("id"));
                MOD_TRANSFER.phase(transfer, LauncherTransferState.Phase.INSTALL);
                synchronized (DATA_LOCK) {
                    File root = getExternalFilesDir(null); if (root == null) throw new IOException("storage unavailable");
                    try (InputStream in = new FileInputStream(file)) { id = LauncherModImporter.install(in, new File(root, "Mods"), getSharedPreferences("megabonk_mod_state", MODE_PRIVATE)); }
                enableContentMod(root, id);
                }
            } catch (Exception e) { error = LauncherImportError.describe(e, es); }
            finally { if (file != null) file.delete(); }
            IMPORT_GATE.modResult(id, error, false); final String failure = error;
            MOD_TRANSFER.finish(transfer, error);
            runOnUiThread(() -> { if (isFinishing() || isDestroyed() || IMPORT_GATE.generation() != generation) return;
                setReady(dataReady); showModImportResult();
                refreshTransferUi();
                if (failure != null) showDialog(new AlertDialog.Builder(this)
                    .setTitle(es ? "No se pudo completar" : "Could not complete")
                    .setMessage(failure).setPositiveButton("OK", null).create());
            });
        }, "openbonk-online-mod");
        } catch (RuntimeException e) {
            MOD_TRANSFER.finish(transfer, LauncherImportError.describe(e, es));
            setReady(dataReady); refreshTransferUi();
        }
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
                enableContentMod(root, id);
            } catch (Exception e) { error = LauncherImportError.describe(e, es); }
            IMPORT_GATE.modResult(id, error, false);
            }
            final String installed = id, failure = error;
            runOnUiThread(() -> {
                if (isFinishing() || isDestroyed() || IMPORT_GATE.generation() != generation) return;
                setReady(dataReady);
                showModImportResult();
                showDialog(new AlertDialog.Builder(this).setTitle("Mods").setMessage(failure == null ? (es ? "Importado o actualizado: " : "Imported or updated: ") + installedModDisplayName(installed) : (es ? "No se pudo importar: " : "Import failed: ") + failure).setPositiveButton("OK", (d, w) -> showMods()).create());
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

    private void collectStartupReport(SharedPreferences prefs) {
        if (android.os.Build.VERSION.SDK_INT < 30) return;
        long attempt = prefs.getLong(LAUNCH_ATTEMPT, 0L);
        int pid = prefs.getInt(LAUNCH_PID, 0);
        if (attempt == 0L || pid == 0 || pid == android.os.Process.myPid()) return;
        try {
            android.app.ActivityManager manager = getSystemService(android.app.ActivityManager.class);
            if (manager == null) return;
            for (android.app.ApplicationExitInfo exit : manager.getHistoricalProcessExitReasons(getPackageName(), pid, 5)) {
                if (exit.getTimestamp() < attempt) continue;
                String reason = startupExitReason(exit.getReason());
                SharedPreferences.Editor editor = prefs.edit().remove(LAUNCH_ATTEMPT).remove(LAUNCH_PID);
                if (reason != null) {
                    String version = prefs.getString("openbonk_launch_version", "");
                    editor.putString(LAUNCH_REPORT, startupReport(version, reason + " (Android status " + exit.getStatus() + ")"));
                }
                editor.apply();
                break;
            }
        } catch (RuntimeException e) {
            android.util.Log.w("OpenBonk", "Could not read startup exit reason", e);
        }
    }

    private static String startupExitReason(int reason) {
        switch (reason) {
            case android.app.ApplicationExitInfo.REASON_CRASH: return "Java crash";
            case android.app.ApplicationExitInfo.REASON_CRASH_NATIVE: return "Native crash";
            case android.app.ApplicationExitInfo.REASON_LOW_MEMORY: return "Low memory";
            case android.app.ApplicationExitInfo.REASON_ANR: return "App not responding";
            case android.app.ApplicationExitInfo.REASON_INITIALIZATION_FAILURE: return "Process initialization failed";
            default: return null;
        }
    }

    private String startupReport(String version, String reason) {
        return "OpenBonk " + version + "\n" + android.os.Build.MANUFACTURER + " " + android.os.Build.MODEL
            + ", Android " + android.os.Build.VERSION.RELEASE + "\nPlay → main menu: " + reason;
    }

    private void showStartupReport(String report) {
        showDialog(new AlertDialog.Builder(this).setTitle(es ? "Informe de inicio" : "Startup report")
            .setMessage(report)
            .setNegativeButton(es ? "Cerrar" : "Close", null)
            .setPositiveButton(es ? "Copiar" : "Copy", (dialog, which) -> {
                android.content.ClipboardManager clipboard = getSystemService(android.content.ClipboardManager.class);
                if (clipboard != null) clipboard.setPrimaryClip(android.content.ClipData.newPlainText("OpenBonk startup report", report));
            }).create());
    }

    private void prepareRuntimeMetadata(SharedPreferences prefs)
            throws IOException, android.content.pm.PackageManager.NameNotFoundException {
        long installedAt = getPackageManager().getPackageInfo(getPackageName(), 0).lastUpdateTime;
        String key = "openbonk_runtime_metadata_install";
        if (prefs.getLong(key, -1L) == installedAt) return;

        // Unity extracts this file once and may otherwise reuse a previous
        // APK's metadata with the upgraded IL2CPP library.
        File external = getExternalFilesDir(null);
        if (external == null) throw new IOException("Runtime storage unavailable");
        for (File root : new File[] { external, getFilesDir() }) {
            File cached = new File(root, "il2cpp/Metadata/global-metadata.dat");
            if (cached.exists() && (!cached.isFile() || !cached.delete()))
                throw new IOException("Could not refresh the Unity metadata cache");
        }
        if (!prefs.edit().putLong(key, installedAt).commit())
            throw new IOException("Could not record the Unity metadata refresh");
    }

    private void launchGame() {
        SharedPreferences prefs = getSharedPreferences(LAUNCHER_PREFS, MODE_PRIVATE);
        try {
            prepareRuntimeMetadata(prefs);
        } catch (IOException | android.content.pm.PackageManager.NameNotFoundException e) {
            android.util.Log.w("OpenBonk", "Could not prepare Unity runtime metadata", e);
            showStartupReport(startupReport(installedVersion(),
                es ? "No se pudo actualizar la caché del juego" : "Could not refresh the game cache"));
            return;
        }
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
        long attempt = System.currentTimeMillis();
        String version = installedVersion();
        game.putExtra(LAUNCH_ATTEMPT, attempt);
        prefs.edit().putLong(LAUNCH_ATTEMPT, attempt).putInt(LAUNCH_PID, android.os.Process.myPid())
            .putString("openbonk_launch_version", version).commit();
        try {
            startActivity(game);
            finish();
        } catch (RuntimeException e) {
            String report = startupReport(version, e.getClass().getSimpleName());
            prefs.edit().remove(LAUNCH_ATTEMPT).remove(LAUNCH_PID).putString(LAUNCH_REPORT, report).apply();
            showStartupReport(report);
        }
    }
}
