package com.megabonk.launcher;

import android.content.Context;
import android.content.SharedPreferences;
import org.json.JSONObject;

/** Queues launcher edits for Unity; in-game edits publish a snapshot back here. */
final class LauncherTouchSettings {
    static final String PREFIX = "openbonk.touch.layout.";
    static final String[] IDS = {"Move", "Look", "Jump", "Slide", "Interact", "Aim", "MapOverlay", "Pause"};
    private final SharedPreferences prefs;
    private JSONObject snapshot, pending;
    LauncherTouchSettings(Context context) {
        prefs = context.getSharedPreferences("openbonk_touch_layout", Context.MODE_PRIVATE);
        snapshot = parse(prefs.getString("snapshot", "{}")); pending = parse(prefs.getString("pending", "{}"));
    }
    private static JSONObject parse(String text) { try { return new JSONObject(text); } catch (Exception e) { return new JSONObject(); } }
    private Object value(String key) { return pending.has(key) ? pending.opt(key) : snapshot.opt(key); }
    boolean has(String key) { Object v = value(key); return v != null && v != JSONObject.NULL; }
    int integer(String key, int fallback) { Object v = value(key); return v instanceof Number ? ((Number)v).intValue() : fallback; }
    float decimal(String key, float fallback) { Object v = value(key); return v instanceof Number ? ((Number)v).floatValue() : fallback; }
    String string(String key, String fallback) { Object v = value(key); return v instanceof String ? (String)v : fallback; }
    boolean set(String key, Object value) {
        try { pending.put(key, value == null ? JSONObject.NULL : value); return prefs.edit().putString("pending", pending.toString()).commit(); }
        catch (Exception e) { return false; }
    }
    boolean setMany(JSONObject changes) {
        try { java.util.Iterator<String> keys = changes.keys(); while (keys.hasNext()) { String key = keys.next(); pending.put(key, changes.get(key)); }
            return prefs.edit().putString("pending", pending.toString()).commit(); }
        catch (Exception e) { return false; }
    }
    boolean setPosition(String id, float x, float y) {
        try { pending.put(PREFIX + id + ".x", x); pending.put(PREFIX + id + ".y", y); return prefs.edit().putString("pending", pending.toString()).commit(); }
        catch (Exception e) { return false; }
    }
    boolean profileExists(int slot) { return integer(PREFIX + "profile." + slot + ".exists", 0) != 0; }
    boolean saveProfile(int slot) {
        String dst = PREFIX + "profile." + slot + ".";
        try {
            for (String id : IDS) {
                for (String key : new String[]{"x", "y", "scale", "opacity"}) {
                    String source = PREFIX + id + "." + key; boolean exists = has(source);
                    pending.put(dst + id + "." + key + ".exists", exists ? 1 : 0);
                    pending.put(dst + id + "." + key, exists ? value(source) : JSONObject.NULL);
                }
                pending.put(dst + id + ".visible", integer(PREFIX + id + ".visible", 1));
                pending.put(dst + id + ".action", string(PREFIX + id + ".action", id));
            }
            pending.put(dst + "grid", integer(PREFIX + "grid", 0)); pending.put(dst + "autorun", integer(PREFIX + "autorun", 0)); pending.put(dst + "exists", 1);
            return prefs.edit().putString("pending", pending.toString()).commit();
        } catch (Exception e) { return false; }
    }
    boolean loadProfile(int slot) {
        if (!profileExists(slot)) return false;
        String src = PREFIX + "profile." + slot + ".";
        try {
            for (String id : IDS) {
                for (String key : new String[]{"x", "y", "scale", "opacity"}) {
                    String name = src + id + "." + key;
                    pending.put(PREFIX + id + "." + key, integer(name + ".exists", 0) != 0 && has(name) ? value(name) : JSONObject.NULL);
                }
                pending.put(PREFIX + id + ".visible", integer(src + id + ".visible", 1));
                pending.put(PREFIX + id + ".action", string(src + id + ".action", id));
            }
            pending.put(PREFIX + "grid", integer(src + "grid", 0)); pending.put(PREFIX + "autorun", integer(src + "autorun", 0));
            return prefs.edit().putString("pending", pending.toString()).commit();
        } catch (Exception e) { return false; }
    }
    boolean hasSnapshot() { return prefs.contains("snapshot"); }
}
