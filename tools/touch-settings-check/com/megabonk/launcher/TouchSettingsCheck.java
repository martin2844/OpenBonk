package com.megabonk.launcher;
import android.content.Context;
import android.content.SharedPreferences;
import java.util.HashMap;
import org.json.JSONObject;

public final class TouchSettingsCheck {
    static final class Store implements SharedPreferences, SharedPreferences.Editor {
        final HashMap<String, String> values = new HashMap<>();
        public boolean contains(String k) { return values.containsKey(k); }
        public String getString(String k, String fallback) { return values.getOrDefault(k, fallback); }
        public Editor edit() { return this; }
        public Editor putString(String k, String v) { values.put(k, v); return this; }
        public Editor remove(String k) { values.remove(k); return this; }
        public boolean commit() { return true; }
    }
    static void require(boolean value, String message) { if (!value) throw new AssertionError(message); }
    public static void main(String[] args) throws Exception {
        Store store = new Store(); Context ctx = new Context() { public SharedPreferences getSharedPreferences(String n, int m) { return store; } };
        LauncherTouchSettings s = new LauncherTouchSettings(ctx); String p = LauncherTouchSettings.PREFIX;
        require(s.setPosition("Move", .2f, .3f), "position save");
        require(s.set(p + "Move.opacity", .4), "opacity save");
        require(s.set(p + "grid", 1), "grid save"); require(s.saveProfile(1), "profile save");
        require(s.setPosition("Move", .7f, .8f), "position change"); require(s.set(p + "Move.opacity", .9), "opacity change");
        require(s.loadProfile(1), "profile load");
        require(Math.abs(s.decimal(p + "Move.x", 0) - .2f) < .001f, "position restore");
        require(Math.abs(s.decimal(p + "Move.opacity", 0) - .4f) < .001f, "opacity restore");
        require(s.integer(p + "grid", 0) == 1 && !s.loadProfile(2), "profile metadata");
        JSONObject pending = new JSONObject(store.getString("pending", "{}"));
        require(pending.getInt(p + "profile.1.Move.x.exists") == 1, "position presence");
        require(pending.isNull(p + "Jump.x"), "missing positions stay absent");
        LauncherTouchSettings reopened = new LauncherTouchSettings(ctx);
        require(Math.abs(reopened.decimal(p + "Move.y", 0) - .3f) < .001f, "pending changes survive restart");
        System.out.println("Touch settings profiles and pending changes passed");
    }
}
