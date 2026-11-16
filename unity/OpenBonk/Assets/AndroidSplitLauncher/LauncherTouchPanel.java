package com.megabonk.launcher;

import android.graphics.Color;
import android.view.View;
import android.widget.Button;
import android.widget.CheckBox;
import android.widget.LinearLayout;
import android.widget.ScrollView;
import android.widget.SeekBar;
import android.widget.TextView;
import org.json.JSONObject;

/** Compact launcher editor; edits are applied to Unity at the next run. */
final class LauncherTouchPanel {
    private final SplitDataLauncher a;
    private final boolean es;
    private final LauncherTouchSettings settings;
    private final LinearLayout panel;
    private int selected, slot = 1;
    private TextView selectedTitle, status;
    private static final String P = LauncherTouchSettings.PREFIX;
    private static final String[] IDS = LauncherTouchSettings.IDS;
    private static final String[] EN = {"Movement", "Camera", "Jump", "Slide", "Interact", "Aim", "Map", "Pause"};
    private static final String[] ES = {"Movimiento", "Cámara", "Salto", "Deslizar", "Interactuar", "Apuntar", "Mapa", "Pausa"};
    private static final float[] X = {.10f, .80f, .95f, .95f, .865f, .95f, .78f, .635f};
    private static final float[] Y = {.16f, .16f, .14f, .30f, .37f, .44f, .37f, .93f};
    static View create(SplitDataLauncher a, boolean es) { return new LauncherTouchPanel(a, es).build(); }
    private LauncherTouchPanel(SplitDataLauncher a, boolean es) { this.a = a; this.es = es; settings = new LauncherTouchSettings(a); panel = new LinearLayout(a); panel.setOrientation(LinearLayout.VERTICAL); }
    private TextView text(String value) { TextView t = new TextView(a); t.setText(value); t.setTextSize(12); t.setTextColor(Color.rgb(238, 231, 244)); return t; }
    private Button button(String label, Runnable action) { Button b = new Button(a); b.setText(label); a.styleButton(b, false); b.setOnClickListener(v -> action.run()); return b; }
    private void addButton(LinearLayout row, String label, Runnable action) { row.addView(button(label, action), new LinearLayout.LayoutParams(0, a.dp(35), 1)); }
    private void styleCheck(CheckBox box) { box.setTextColor(Color.WHITE); box.setTextSize(12); box.setButtonTintList(android.content.res.ColorStateList.valueOf(Color.rgb(213, 245, 95))); }
    private LinearLayout row() { LinearLayout r = new LinearLayout(a); r.setOrientation(LinearLayout.HORIZONTAL); return r; }
    private void changed(boolean ok) { if (status != null) status.setText(ok ? (es ? "Cambios listos para la próxima partida." : "Changes will apply next time you play.") : (es ? "No se pudieron guardar los cambios." : "Could not save changes.")); }
    private View build() {
        ScrollView scroll = new ScrollView(a); scroll.setFillViewport(false); scroll.addView(panel);
        scroll.setLayoutParams(new LinearLayout.LayoutParams(-1, a.dp(275))); render(); return scroll;
    }
    private void render() {
        panel.removeAllViews(); panel.addView(text(es ? "Personalizá tus controles táctiles" : "Customize touch controls"));
        if (!settings.hasSnapshot()) panel.addView(text(es ? "Abrí una partida para sincronizar una configuración anterior." : "Start a run to sync an existing layout."));
        CheckBox grid = new CheckBox(a); grid.setText(es ? "Ajustar a cuadrícula" : "Snap to grid"); styleCheck(grid); grid.setChecked(settings.integer(P + "grid", 0) != 0);
        grid.setOnCheckedChangeListener((v, checked) -> changed(settings.set(P + "grid", checked ? 1 : 0))); panel.addView(grid);
        CheckBox auto = new CheckBox(a); auto.setText(es ? "Autoavance cancelable" : "Cancelable auto-run"); styleCheck(auto); auto.setChecked(settings.integer(P + "autorun", 0) != 0);
        auto.setOnCheckedChangeListener((v, checked) -> changed(settings.set(P + "autorun", checked ? 1 : 0))); panel.addView(auto);
        LinearLayout profile = row(); addButton(profile, (es ? "Perfil " : "Profile ") + slot, () -> { slot = slot % 3 + 1; render(); });
        addButton(profile, es ? "Guardar" : "Save", () -> changed(settings.saveProfile(slot)));
        addButton(profile, es ? "Cargar" : "Load", () -> { boolean ok = settings.loadProfile(slot); render(); changed(ok); }); panel.addView(profile);
        LinearLayout all = row(); addButton(all, es ? "Mostrar botones" : "Show buttons", () -> setAll(true)); addButton(all, es ? "Ocultar botones" : "Hide buttons", () -> setAll(false)); panel.addView(all);
        LinearLayout selector = row(); addButton(selector, "‹", () -> { selected = (selected + IDS.length - 1) % IDS.length; render(); });
        selectedTitle = text((es ? ES : EN)[selected]); selectedTitle.setGravity(android.view.Gravity.CENTER); selector.addView(selectedTitle, new LinearLayout.LayoutParams(0, a.dp(35), 2));
        addButton(selector, "›", () -> { selected = (selected + 1) % IDS.length; render(); }); panel.addView(selector);
        String id = IDS[selected], key = P + id + ".";
        CheckBox visible = new CheckBox(a); visible.setText(es ? "Visible y activo" : "Visible and active"); styleCheck(visible); visible.setChecked(settings.integer(key + "visible", 1) != 0);
        visible.setOnCheckedChangeListener((v, checked) -> changed(settings.set(key + "visible", checked ? 1 : 0))); panel.addView(visible);
        slider(es ? "Opacidad" : "Opacity", settings.decimal(key + "opacity", 1f), .1f, 1f, value -> settings.set(key + "opacity", value));
        slider(es ? "Tamaño" : "Size", settings.decimal(key + "scale", 1f), .5f, 1.8f, value -> settings.set(key + "scale", value));
        slider(es ? "Posición horizontal" : "Horizontal position", settings.decimal(key + "x", X[selected]), 0f, 1f,
            value -> settings.setPosition(id, value, settings.decimal(key + "y", Y[selected])));
        slider(es ? "Posición vertical" : "Vertical position", settings.decimal(key + "y", Y[selected]), 0f, 1f,
            value -> settings.setPosition(id, settings.decimal(key + "x", X[selected]), value));
        if (selected >= 2 && selected <= 6) {
            String action = settings.string(key + "action", id);
            Button swap = button((es ? "Acción: " : "Action: ") + action, () -> cycleAction(id)); panel.addView(swap, a.buttonParams());
        }
        Button reset = button(es ? "Restaurar controles" : "Reset controls", this::reset); panel.addView(reset, a.buttonParams());
        status = text(es ? "Los cambios se aplican al iniciar una partida." : "Changes apply when a run starts."); panel.addView(status);
    }
    private interface SetFloat { boolean set(float value); }
    private void slider(String label, float value, float min, float max, SetFloat setter) {
        TextView title = text(label + "  " + Math.round(value * 100) + "%"); panel.addView(title);
        SeekBar bar = new SeekBar(a); bar.setProgressTintList(android.content.res.ColorStateList.valueOf(Color.rgb(213, 245, 95))); bar.setThumbTintList(android.content.res.ColorStateList.valueOf(Color.rgb(255, 169, 20))); bar.setMax(100); bar.setProgress(Math.round((Math.max(min, Math.min(max, value)) - min) * 100 / (max - min)));
        bar.setOnSeekBarChangeListener(new SeekBar.OnSeekBarChangeListener() {
            public void onProgressChanged(SeekBar b, int progress, boolean user) { if (!user) return; float next = min + (max - min) * progress / 100f; title.setText(label + "  " + Math.round(next * 100) + "%"); changed(setter.set(next)); }
            public void onStartTrackingTouch(SeekBar b) { } public void onStopTrackingTouch(SeekBar b) { }
        }); panel.addView(bar);
    }
    private void setAll(boolean visible) {
        try { JSONObject changes = new JSONObject(); for (int i = 2; i < IDS.length; i++) changes.put(P + IDS[i] + ".visible", visible ? 1 : 0); boolean ok = settings.setMany(changes); render(); changed(ok); }
        catch (Exception e) { changed(false); }
    }
    private void cycleAction(String id) {
        String[] choices = {"Jump", "Slide", "Interact", "Aim", "MapOverlay"}; String current = settings.string(P + id + ".action", id); int index = 0;
        for (int i = 0; i < choices.length; i++) if (choices[i].equals(current)) index = i;
        String next = choices[(index + 1) % choices.length];
        try { JSONObject changes = new JSONObject(); changes.put(P + id + ".action", next);
            for (String other : choices) if (!other.equals(id) && settings.string(P + other + ".action", other).equals(next)) { changes.put(P + other + ".action", current); break; }
            boolean ok = settings.setMany(changes); render(); changed(ok);
        } catch (Exception e) { changed(false); }
    }
    private void reset() {
        try { JSONObject changes = new JSONObject(); for (String id : IDS) for (String name : new String[]{"x", "y", "scale", "opacity", "visible", "action"}) changes.put(P + id + "." + name, JSONObject.NULL);
            changes.put(P + "grid", JSONObject.NULL); changes.put(P + "autorun", JSONObject.NULL); boolean ok = settings.setMany(changes); render(); changed(ok);
        } catch (Exception e) { changed(false); }
    }
}
