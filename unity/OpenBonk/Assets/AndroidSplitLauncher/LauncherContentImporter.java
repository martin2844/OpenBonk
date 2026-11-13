package com.megabonk.launcher;

import android.content.SharedPreferences;
import java.io.*;
import java.nio.ByteBuffer;
import java.nio.charset.CodingErrorAction;
import java.nio.charset.StandardCharsets;
import java.security.MessageDigest;
import java.util.*;
import java.util.zip.*;
import org.json.*;

/** Bounded, staged importer for API 3 content packages. */
final class LauncherContentImporter {
    private static final long FILE_LIMIT = 64L * 1024 * 1024, TOTAL_LIMIT = 128L * 1024 * 1024;
    // Keep in sync with Assets.Scripts.Menu.Shop.EStat. The importer must reject
    // the same unknown or non-canonical stat names as AndroidContentMods does,
    // before replacing an already installed package.
    private enum ItemStat {
        MaxHealth, HealthRegen, Shield, Thorns, Armor, Evasion, Evolve,
        DamageReductionMultiplier, DamageCooldownMultiplier, SizeMultiplier,
        DurationMultiplier, ProjectileSpeedMultiplier, DamageMultiplier, Unused0,
        EffectDurationMultiplier, AttackSpeed, Projectiles, Lifesteal, CritChance,
        CritDamage, FireDamage, IceDamage, LightningDamage, EliteDamageMultiplier,
        KnockbackMultiplier, MoveSpeedMultiplier, JumpHeight, FallDamageReduction,
        Slam, PickupRange, Luck, GoldIncreaseMultiplier, XpIncreaseMultiplier,
        ChestIncreaseMultiplier, ChestPriceMultiplier, ShopPriceReduction, Holiness,
        Wickedness, Difficulty, EliteSpawnIncrease, PowerupBoostMultiplier,
        PowerupChance, BurnChance, FreezeChance, WeaponBurstCooldown,
        ProjectileBounces, ExtraJumps, Overheal, HealingMultiplier,
        SilverIncreaseMultiplier, EnemyAmountMultiplier, EnemySizeMultiplier,
        EnemySpeedMultiplier, EnemyHpMultiplier, EnemyDamageMultiplier,
        EnemyScalingMultiplier, PoisonDamageMultiplier
    }

    private static boolean validItemStat(String name) {
        try { ItemStat.valueOf(name); return true; }
        catch (IllegalArgumentException | NullPointerException ignored) { return false; }
    }
    static String install(ZipFile zip, File root, SharedPreferences prefs, String prefix) throws Exception {
        if (!prefix.isEmpty() && (!prefix.endsWith("/") || prefix.indexOf('/') != prefix.length() - 1 || !validPath(prefix.substring(0, prefix.length() - 1))))
            throw new IOException("invalid content directory");
        Map<String, ZipEntry> entries = new HashMap<>(); Set<String> seen = new HashSet<>();
        Enumeration<? extends ZipEntry> all = zip.entries(); int count = 0;
        while (all.hasMoreElements()) {
            ZipEntry entry = all.nextElement(); if (++count > 128) throw new IOException("too many files");
            String path = entry.getName();
            if (!prefix.isEmpty()) {
                if (entry.isDirectory() && path.equals(prefix)) continue;
                if (!path.startsWith(prefix)) throw new IOException("files must share a directory");
                path = path.substring(prefix.length());
            }
            if (entry.isDirectory()) { if (!validPath(path.substring(0, path.length() - 1))) throw new IOException("invalid directory"); continue; }
            if (!validPath(path) || !seen.add(path.toLowerCase(Locale.ROOT))) throw new IOException("invalid or duplicate path");
            entries.put(path, entry);
        }
        ZipEntry manifestEntry = entries.get("manifest.json");
        if (manifestEntry == null) throw new IOException("missing manifest");
        byte[] manifest = readSmall(zip, manifestEntry, 65536);
        JSONObject json = new JSONObject(utf8(manifest));
        String id = json.optString("id", "");
        if (json.optInt("api") != 3 || !json.optString("kind").equals("content") || !id.matches("[a-z0-9][a-z0-9._-]{0,63}")) throw new IOException("unsupported content package");
        JSONObject character = json.optJSONObject("character"), weapon = json.optJSONObject("weapon"), item = json.optJSONObject("item"), map = json.optJSONObject("map");
        JSONObject passive = json.optJSONObject("passive");
        if (json.has("passive") && !json.isNull("passive") && passive == null)
            throw new IOException("invalid passive definition");
        if (map != null) {
            if (character != null || weapon != null || item != null || json.optJSONObject("passive") != null ||
                !contentId(map.optString("id")) || !text(map.optString("name"), 80) || !text(map.optString("description"), 500) ||
                !text(map.optString("icon"), 120) || !optionalText(map, "groundTexture", 120) || !optionalText(map, "wallTexture", 120) ||
                !number(map, "stageDuration", 60, 1800))
                throw new IOException("unsupported map definition");
            String base = map.optString("base");
            if ("Forest".equals(base)) {
                if (!number(map, "tileGridSize", 7, 20) || map.optDouble("tileGridSize") != map.optInt("tileGridSize") ||
                    !number(map, "volatility", 0, 2) || !number(map, "slopeStrength", -1, 1) || !number(map, "flatMapBias", -1, 1))
                    throw new IOException("unsupported tile recipe");
            } else if ("Desert".equals(base)) {
                if (!number(map, "noiseScale", 25, 250) || !number(map, "heightMultiplier", 1, 20) || !number(map, "mapScale", .75, 1.25))
                    throw new IOException("unsupported mesh recipe");
            } else throw new IOException("unsupported map template");
            if (map.has("objects")) {
                JSONArray objects = map.optJSONArray("objects");
                if (objects == null || objects.length() == 0 || objects.length() > 8 || !text(map.optString("bundle"), 120)) throw new IOException("unsupported map objects");
                for (int i = 0; i < objects.length(); i++) {
                    JSONObject object = objects.optJSONObject(i);
                    if (object == null || !text(object.optString("prefab"), 120) || !number(object, "amount", 1, 64) || object.optDouble("amount") != object.optInt("amount") ||
                        !number(object, "scaleMin", .1, 10) || !number(object, "scaleMax", object.optDouble("scaleMin"), 10) ||
                        !number(object, "checkRadius", .1, 20) || !number(object, "maxSlopeAngle", 0, 60) || !number(object, "upOffset", -10, 10))
                        throw new IOException("unsupported map object placement");
                    if (object.has("stages")) {
                        JSONArray stages = object.optJSONArray("stages");
                        if (stages == null || stages.length() == 0 || stages.length() > 3) throw new IOException("invalid map object stages");
                        boolean[] stageSeen = new boolean[3];
                        for (int j = 0; j < stages.length(); j++) {
                            Object value = stages.opt(j);
                            if (!(value instanceof Number)) throw new IOException("invalid map object stage");
                            double numeric = ((Number) value).doubleValue();
                            if (numeric != Math.rint(numeric) || numeric < 1 || numeric > 3 || stageSeen[(int) numeric - 1]) throw new IOException("invalid map object stage");
                            stageSeen[(int) numeric - 1] = true;
                        }
                    }
                }
            } else if (map.has("bundle") && !map.isNull("bundle") && !map.optString("bundle").isEmpty()) throw new IOException("map bundle requires objects");
            if (map.has("hooks")) {
                JSONArray hooks = map.optJSONArray("hooks");
                if (hooks == null || hooks.length() > 7) throw new IOException("unsupported map hooks");
                HashSet<String> mapHooks = new HashSet<>();
                for (int i = 0; i < hooks.length(); i++) {
                    String hook = hooks.optString(i, "");
                    if ((!"stage_spawn_multiplier".equals(hook) && !"stage_enemy_health_multiplier".equals(hook) && !"stage_enemy_speed_multiplier".equals(hook) && !"stage_enemy_damage_multiplier".equals(hook) && !"enemy_health_multiplier".equals(hook) && !"enemy_speed_multiplier".equals(hook) && !"enemy_damage_multiplier".equals(hook)) || !mapHooks.add(hook))
                        throw new IOException("unsupported map hooks");
                }
            }
        } else if (item != null) {
            JSONArray bonuses = item.optJSONArray("bonuses");
            if (character != null || weapon != null || json.optJSONObject("passive") != null ||
                !contentId(item.optString("id")) || !text(item.optString("name"), 80) || !text(item.optString("description"), 500) ||
                !Arrays.asList("Common", "Rare", "Epic", "Legendary", "Corrupted", "Quest").contains(item.optString("rarity")) ||
                !optionalInteger(item, "maxAmount", 0, 100000) || !optionalInteger(item, "maxAmountPerRun", 0, 100000) ||
                (item.has("bonuses") && bonuses == null) || (bonuses != null && bonuses.length() > 16)) throw new IOException("unsupported item definition");
            Set<String> stats = new HashSet<>();
            for (int i = 0; bonuses != null && i < bonuses.length(); i++) {
                JSONObject bonus = bonuses.optJSONObject(i);
                if (bonus == null || !validItemStat(bonus.optString("stat")) || !stats.add(bonus.optString("stat")) ||
                    !("Flat".equals(bonus.optString("mode")) || "Addition".equals(bonus.optString("mode"))) || !number(bonus, "amount", -10, 10))
                    throw new IOException("unsupported item bonus");
            }
            boolean attackHook = false, hitHook = false, beforeDamageHook = false, tickHook = false, killHook = false;
            if (item.has("hooks")) {
                JSONArray hooks = item.optJSONArray("hooks");
                if (hooks == null || hooks.length() > 8) throw new IOException("invalid item hooks");
                Set<String> distinct = new HashSet<>();
                for (int i = 0; i < hooks.length(); i++) {
                    String hook = hooks.optString(i, "");
                    if (!(hook.equals("stack_changed") || hook.equals("pre_attack") || hook.equals("on_hit") || hook.equals("level_up") || hook.equals("enemy_killed") || hook.equals("player_damaged") || hook.equals("before_player_damage") || hook.equals("tick")) || !distinct.add(hook)) throw new IOException("invalid item hook");
                    if (hook.equals("pre_attack")) attackHook = true;
                    if (hook.equals("on_hit")) hitHook = true;
                    if (hook.equals("before_player_damage")) beforeDamageHook = true;
                    if (hook.equals("tick")) tickHook = true;
                    if (hook.equals("enemy_killed")) killHook = true;
                }
            }
            if ((bonuses == null || bonuses.length() == 0) && !attackHook && !hitHook && !beforeDamageHook && !tickHook && !killHook) throw new IOException("item needs bonuses or an active hook");
        } else if (weapon == null || !("lightning".equals(weapon.optString("effect")) || "fireball".equals(weapon.optString("effect")) || "arrow".equals(weapon.optString("effect"))) ||
            !contentId(weapon.optString("id")) ||
            (character == null && (json.optJSONObject("passive") != null || !text(weapon.optString("icon"), 120))) ||
            (character != null && (!contentId(character.optString("id")) || !text(character.optString("name"), 80) || !text(character.optString("description"), 500) ||
            !text(character.optString("model"), 120) || !text(character.optString("walk"), 120) || !number(character, "scale", .01, 10))) ||
            !text(weapon.optString("name"), 80) || !text(weapon.optString("description"), 500) ||
            !number(weapon, "damage", 0, 10000) ||
            !number(weapon, "cooldown", .05, 100) || !number(weapon, "range", .1, 100) ||
            !number(weapon, "chainRange", 0, 100) || !number(weapon, "chainProc", 0, 1) ||
            !number(weapon, "bounces", 0, 16) || weapon.optDouble("bounces", -1) != weapon.optInt("bounces", -2))
            throw new IOException("unsupported content definition");
        if (character != null) {
            for (String key : Arrays.asList("idle", "run", "jump", "surf", "surfboard"))
                if (!optionalText(character, key, 120)) throw new IOException("invalid movement definition: " + key);
        }
        if (weapon != null) {
            if (!optionalText(weapon, "icon", 120) || !optionalText(weapon, "soundClip", 120) || !optionalText(weapon, "lightningColor", 7))
                throw new IOException("invalid weapon appearance");
            String color = weapon.isNull("lightningColor") ? "" : weapon.optString("lightningColor", "");
            String sound = weapon.isNull("soundClip") ? "" : weapon.optString("soundClip", "");
            if ((!color.isEmpty() && (!"lightning".equals(weapon.optString("effect")) || !color.matches("#[0-9a-fA-F]{6}"))) ||
                (!sound.isEmpty() && (character == null || !"lightning".equals(weapon.optString("effect")))))
                throw new IOException("invalid weapon appearance");
        }
        if (passive != null) {
            JSONArray bonuses = passive.optJSONArray("perLevel");
            if (character == null || !text(passive.optString("name"), 80) || !text(passive.optString("description"), 500) ||
                bonuses == null || bonuses.length() == 0 || bonuses.length() > 16)
                throw new IOException("invalid passive definition");
            Set<String> stats = new HashSet<>();
            for (int i = 0; i < bonuses.length(); i++) {
                JSONObject bonus = bonuses.optJSONObject(i);
                if (bonus == null || !validItemStat(bonus.optString("stat")) || !stats.add(bonus.optString("stat")) ||
                    !number(bonus, "amount", -1, 1))
                    throw new IOException("invalid per-level bonus");
            }
        }
        if (character != null && character.has("hat")) {
            JSONObject hat = character.optJSONObject("hat");
            if (hat == null || !text(hat.optString("bone"), 120) || !number(hat, "offsetX", -20, 20) ||
                !number(hat, "offsetY", -20, 20) || !number(hat, "offsetZ", -20, 20) || !number(hat, "scale", .01, 10))
                throw new IOException("unsupported hat anchor");
        }
        JSONArray declarations = json.optJSONArray("files");
        if (declarations == null || declarations.length() < (character == null ? 2 : 3) || declarations.length() > 64 || entries.size() != declarations.length() + 1) throw new IOException("invalid file declarations");
        Map<String, JSONObject> files = new LinkedHashMap<>(); Set<String> unique = new HashSet<>(); long total = 0;
        for (int i = 0; i < declarations.length(); i++) {
            JSONObject spec = declarations.getJSONObject(i); String path = spec.optString("path", ""); long size = spec.optLong("size", -1); String hash = spec.optString("sha256", "");
            if (!validPath(path) || path.equalsIgnoreCase("manifest.json") || !unique.add(path.toLowerCase(Locale.ROOT)) || size <= 0 || size > FILE_LIMIT || !hash.matches("[a-fA-F0-9]{64}")) throw new IOException("invalid file declaration");
            ZipEntry entry = entries.get(path); if (entry == null || entry.getSize() != size) throw new IOException("missing file or size mismatch");
            total += size; if (total > TOTAL_LIMIT) throw new IOException("content too large"); files.put(path, spec);
        }
        String portrait = map != null ? map.optString("icon", "") : item != null ? item.optString("icon", "") : character != null ? character.optString("portrait", "") : weapon.optString("icon", "");
        String bundle = character == null ? null : character.optString("bundle", "");
        if (!files.containsKey("main.lua") || !files.containsKey(portrait) || (bundle != null && !files.containsKey(bundle)) || files.get("main.lua").getLong("size") > 65536 || files.get(portrait).getLong("size") > 4L * 1024 * 1024) throw new IOException("missing content files");
        String weaponIcon = weapon == null || weapon.isNull("icon") ? "" : weapon.optString("icon", "");
        if (!weaponIcon.isEmpty() && (!files.containsKey(weaponIcon) || files.get(weaponIcon).getLong("size") > 4L * 1024 * 1024)) throw new IOException("missing weapon icon");
        String mapBundle = map == null || !map.has("objects") ? null : map.optString("bundle", "");
        if (mapBundle != null && !files.containsKey(mapBundle)) throw new IOException("missing map bundle");
        String groundTexture = map == null || map.isNull("groundTexture") ? "" : map.optString("groundTexture", "");
        if (!groundTexture.isEmpty() && (!files.containsKey(groundTexture) || files.get(groundTexture).getLong("size") > 4L * 1024 * 1024)) throw new IOException("missing ground texture");
        String wallTexture = map == null || map.isNull("wallTexture") ? "" : map.optString("wallTexture", "");
        if (!wallTexture.isEmpty() && (!files.containsKey(wallTexture) || files.get(wallTexture).getLong("size") > 4L * 1024 * 1024)) throw new IOException("missing wall texture");
        File destination = new File(root, id), staging = new File(root, ".import-" + UUID.randomUUID());
        if (!staging.mkdir()) throw new IOException("cannot stage mod");
        try {
            try (FileOutputStream out = new FileOutputStream(new File(staging, "manifest.json"))) { out.write(manifest); out.getFD().sync(); }
            byte[] buffer = new byte[65536];
            for (Map.Entry<String, JSONObject> file : files.entrySet()) {
                String path = file.getKey(); JSONObject spec = file.getValue(); long expected = spec.getLong("size"), copied = 0;
                File target = new File(staging, path); File parent = target.getParentFile();
                if (!parent.isDirectory() && !parent.mkdirs()) throw new IOException("cannot create directory");
                MessageDigest hash = MessageDigest.getInstance("SHA-256"); CRC32 crc = new CRC32();
                ZipEntry source = entries.get(path); if (source.getCrc() < 0) throw new IOException("missing ZIP checksum");
                try (InputStream in = zip.getInputStream(entries.get(path)); FileOutputStream out = new FileOutputStream(target)) {
                    int n; while ((n = in.read(buffer)) != -1) { copied += n; if (copied > expected) throw new IOException("file too large"); hash.update(buffer, 0, n); crc.update(buffer, 0, n); out.write(buffer, 0, n); }
                    if (copied != expected || crc.getValue() != source.getCrc() || !hex(hash.digest()).equalsIgnoreCase(spec.getString("sha256"))) throw new IOException("content hash mismatch");
                    out.getFD().sync();
                }
            }
            try (InputStream in = new FileInputStream(new File(staging, "main.lua")); ByteArrayOutputStream out = new ByteArrayOutputStream()) {
                int n; while ((n = in.read(buffer)) != -1) out.write(buffer, 0, n);
                if (utf8(out.toByteArray()).indexOf('\0') >= 0) throw new IOException("invalid script");
            }
            LauncherModRecovery.install(root, id, staging, prefs);
            return id;
        } finally { deleteTree(staging); }
    }
    private static byte[] readSmall(ZipFile zip, ZipEntry entry, int limit) throws IOException {
        if (entry.getSize() <= 0 || entry.getSize() > limit) throw new IOException("invalid manifest size");
        return LauncherModImporter.readEntry(zip, entry, limit);
    }
    private static String utf8(byte[] bytes) throws Exception { return StandardCharsets.UTF_8.newDecoder().onMalformedInput(CodingErrorAction.REPORT).onUnmappableCharacter(CodingErrorAction.REPORT).decode(ByteBuffer.wrap(bytes)).toString(); }
    private static boolean contentId(String value) { return value.matches("[a-z0-9][a-z0-9._-]{0,63}"); }
    private static boolean text(String value, int limit) { return value != null && !value.trim().isEmpty() && value.length() <= limit && value.indexOf('\0') < 0; }
    private static boolean optionalText(JSONObject object, String key, int limit) {
        if (!object.has(key) || object.isNull(key)) return true;
        Object value = object.opt(key);
        return value instanceof String && (((String)value).isEmpty() || text((String)value, limit));
    }
    private static boolean number(JSONObject object, String key, double min, double max) { double value = object.optDouble(key, Double.NaN); return !Double.isNaN(value) && !Double.isInfinite(value) && value >= min && value <= max; }
    private static boolean optionalInteger(JSONObject object, String key, int min, int max) { return !object.has(key) || (number(object, key, min, max) && object.optDouble(key) == object.optInt(key)); }
    private static String hex(byte[] bytes) { StringBuilder out = new StringBuilder(); for (byte b : bytes) out.append(String.format(Locale.ROOT, "%02x", b & 255)); return out.toString(); }
    private static boolean validPath(String path) {
        if (path == null || path.length() == 0 || path.length() > 240 || path.indexOf('\\') >= 0) return false;
        String[] parts = path.split("/", -1); if (parts.length > 8) return false;
        for (String part : parts) if (!part.matches("[A-Za-z0-9][A-Za-z0-9._-]{0,63}")) return false;
        return true;
    }
    private static void deleteTree(File file) { if (file.isDirectory()) { File[] children = file.listFiles(); if (children != null) for (File child : children) deleteTree(child); } file.delete(); }
}
