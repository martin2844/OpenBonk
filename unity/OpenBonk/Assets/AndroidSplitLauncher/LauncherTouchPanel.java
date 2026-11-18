package com.megabonk.launcher;

import android.graphics.Color;
import android.view.Gravity;
import android.view.View;
import android.widget.Button;
import android.widget.LinearLayout;
import android.widget.TextView;

/** Selects locally saved layouts; controls are edited visually in the game. */
final class LauncherTouchPanel {
    private final SplitDataLauncher activity;
    private final boolean spanish;
    private final LauncherTouchSettings settings;
    private final LinearLayout panel;
    private int slot = 1;
    private TextView status;

    static View create(SplitDataLauncher activity, boolean spanish) {
        return new LauncherTouchPanel(activity, spanish).build();
    }
    private LauncherTouchPanel(SplitDataLauncher activity, boolean spanish) {
        this.activity = activity; this.spanish = spanish;
        settings = new LauncherTouchSettings(activity);
        panel = new LinearLayout(activity); panel.setOrientation(LinearLayout.VERTICAL);
    }
    private TextView text(String value) {
        TextView text = new TextView(activity); text.setText(value); text.setTextSize(12);
        text.setTextColor(Color.rgb(225, 205, 235)); return text;
    }
    private Button button(String label, Runnable action) {
        Button result = new Button(activity); result.setText(label); activity.styleButton(result, false);
        result.setOnClickListener(v -> action.run()); return result;
    }
    private View build() { render(); return panel; }
    private void render() {
        panel.removeAllViews();
        activity.divider(panel, 10);
        panel.addView(activity.cardTitle(spanish ? "Perfiles táctiles" : "Touch profiles"));
        panel.addView(text(spanish ? "Editá los controles visualmente durante una partida." : "Edit controls visually during a run."));
        if (!settings.hasSnapshot()) panel.addView(text(spanish
            ? "Iniciá una partida para sincronizar una configuración anterior antes de guardarla."
            : "Start a run to sync an existing layout before saving it."));
        LinearLayout row = new LinearLayout(activity); row.setGravity(Gravity.CENTER_VERTICAL);
        Button select = button((spanish ? "Perfil " : "Profile ") + slot, () -> { slot = slot % 3 + 1; render(); });
        Button save = button(spanish ? "Guardar" : "Save", () -> showResult(settings.saveProfile(slot), false));
        save.setEnabled(settings.hasSnapshot());
        Button load = button(spanish ? "Cargar" : "Load", () -> showResult(settings.loadProfile(slot), true));
        for (Button action : new Button[]{select, save, load}) {
            LinearLayout.LayoutParams params = new LinearLayout.LayoutParams(0, -1, 1);
            if (row.getChildCount() > 0) params.setMarginStart(activity.dp(6));
            row.addView(action, params);
        }
        panel.addView(row, activity.buttonParams());
        status = text(settings.profileExists(slot) ? (spanish ? "Perfil guardado" : "Saved profile") : (spanish ? "Perfil vacío" : "Empty profile"));
        status.setPadding(0, activity.dp(5), 0, 0);
        panel.addView(status);
    }
    private void showResult(boolean success, boolean loaded) {
        if (success) status.setText(loaded
            ? (spanish ? "El perfil se aplicará al iniciar la partida." : "The profile will apply when the game starts.")
            : (spanish ? "Perfil guardado." : "Profile saved."));
        else status.setText(loaded && !settings.profileExists(slot)
            ? (spanish ? "Este perfil está vacío." : "This profile is empty.")
            : (spanish ? "No se pudieron guardar los cambios." : "Could not save changes."));
    }
}
