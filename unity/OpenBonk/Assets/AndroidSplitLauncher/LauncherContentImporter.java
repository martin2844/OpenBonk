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
        int api = json.optInt("api");
        if ((api != 3 && api != 4) || !json.optString("kind").equals("content") || !id.matches("[a-z0-9][a-z0-9._-]{0,63}")) throw new IOException("unsupported content package");
        JSONObject character = json.optJSONObject("character"), weapon = json.optJSONObject("weapon"), item = json.optJSONObject("item"), map = json.optJSONObject("map");
        JSONObject passive = json.optJSONObject("passive");
        if (api == 4 && map == null) throw new IOException("API 4 requires a map package");
        JSONArray stageEnvironments = map == null ? null : map.optJSONArray("stageEnvironments");
        String mapBundle = null;
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
            if (map.has("stageDurations")) {
                JSONArray durations = map.optJSONArray("stageDurations");
                if (!"Forest".equals(base) || durations == null || durations.length() != 3)
                    throw new IOException("invalid per-stage durations");
                for (int i = 0; i < durations.length(); i++) {
                    Object value = durations.opt(i);
                    if (!(value instanceof Number) || !Double.isFinite(((Number)value).doubleValue()) ||
                        ((Number)value).doubleValue() < 60 || ((Number)value).doubleValue() > 1800)
                        throw new IOException("invalid per-stage duration");
                }
            }
            boolean needsMapBundle = map.has("stageEnvironments") && validateStageEnvironments(api, base, map, stageEnvironments);
            if (map.has("enemies") || map.has("encounters")) {
                validateMapEncounters(map.optJSONArray("enemies"), map.optJSONArray("encounters"));
                needsMapBundle = true;
            }
            if (map.has("objects")) {
                needsMapBundle = true;
                JSONArray objects = map.optJSONArray("objects");
                if (objects == null || objects.length() == 0 || objects.length() > 8) throw new IOException("unsupported map objects");
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
            }
            if (!optionalText(map, "bundle", 120)) throw new IOException("invalid map bundle path");
            String bundlePath = optionalString(map, "bundle");
            if (needsMapBundle == bundlePath.isEmpty()) throw new IOException("map bundle must match declared assets");
            if (needsMapBundle) mapBundle = bundlePath;
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
            !number(weapon, "bounces", 0, 16) || weapon.optDouble("bounces", -1) != weapon.optInt("bounces", -2) ||
            (weapon.has("chargeSeconds") && !number(weapon, "chargeSeconds", 0, 60)) ||
            (weapon.has("dischargeRadius") && !number(weapon, "dischargeRadius", 0, 60)) ||
            (weapon.has("dischargeDamage") && !number(weapon, "dischargeDamage", 0, 20)) ||
            (weapon.has("dischargeOnAim") && !(weapon.opt("dischargeOnAim") instanceof Boolean)) ||
            (weapon.optBoolean("dischargeOnAim", false) && (!"lightning".equals(weapon.optString("effect")) ||
                weapon.optDouble("chargeSeconds", 0) <= 0 || weapon.optDouble("dischargeRadius", 0) < 1)))
            throw new IOException("unsupported content definition");
        if (character != null) {
            for (String key : Arrays.asList("idle", "run", "jump", "surf", "surfboard"))
                if (!optionalText(character, key, 120)) throw new IOException("invalid movement definition: " + key);
            if (character.has("baseStats") && !character.isNull("baseStats")) {
                JSONArray baseStats = character.optJSONArray("baseStats");
                if (baseStats == null || baseStats.length() > 64) throw new IOException("invalid character base stats");
                Set<String> statModes = new HashSet<>();
                for (int i = 0; i < baseStats.length(); i++) {
                    JSONObject baseStat = baseStats.optJSONObject(i);
                    if (baseStat == null) throw new IOException("invalid character base stat");
                    String stat = baseStat.optString("stat", ""), mode = baseStat.optString("mode", "");
                    if (!validItemStat(stat) || "Unused0".equals(stat) ||
                        !("Flat".equals(mode) || "Addition".equals(mode) || "Multiplication".equals(mode)) ||
                        !statModes.add(stat + ":" + mode)) throw new IOException("invalid character base stat");
                    double min = "Multiplication".equals(mode) ? 0 : "Addition".equals(mode) ? -1 : -10000;
                    double max = "Flat".equals(mode) ? 10000 : 1000;
                    if (!number(baseStat, "amount", min, max)) throw new IOException("invalid character base stat amount");
                }
            }
            if (character.has("collider") && !character.isNull("collider")) {
                JSONObject collider = character.optJSONObject("collider");
                if (collider == null || !number(collider, "height", .1, 20) || !number(collider, "width", .05, 10) ||
                    collider.optDouble("height") < 2 * collider.optDouble("width"))
                    throw new IOException("invalid character collider");
            }
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
        if (mapBundle != null && !files.containsKey(mapBundle)) throw new IOException("missing map bundle");
        if (map != null) requireMapTextures(map, files);
        if (map != null && map.has("encounters")) {
            JSONArray encounters = map.getJSONArray("encounters");
            for (int i = 0; i < encounters.length(); i++) {
                JSONObject reward = encounters.getJSONObject(i).optJSONObject("reward");
                if (reward == null) continue;
                JSONObject icon = files.get(reward.getString("icon"));
                if (icon == null || icon.getLong("size") > 4L * 1024 * 1024)
                    throw new IOException("missing arena reward icon");
            }
        }
        if (stageEnvironments != null)
            for (int i = 0; i < stageEnvironments.length(); i++) requireMapTextures(stageEnvironments.getJSONObject(i), files);
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
    private static boolean blankStreetName(String name) {
        // Match .NET IsNullOrWhiteSpace, including non-breaking spaces and NEL.
        for (int i = 0; i < name.length(); i++) {
            char ch = name.charAt(i);
            if (!Character.isWhitespace(ch) && !Character.isSpaceChar(ch) && ch != 0x85) return false;
        }
        return true;
    }
    private static boolean validateStageEnvironments(int api, String base, JSONObject map, JSONArray environments) throws IOException {
        if (!"Forest".equals(base) || environments == null || environments.length() < 1 || environments.length() > 3)
            throw new IOException("unsupported stage environments");
        boolean[] seen = new boolean[3];
        boolean needsBundle = false;
        for (int i = 0; i < environments.length(); i++) {
            JSONObject entry = environments.optJSONObject(i);
            if (entry == null || !(entry.opt("stage") instanceof Number) || !number(entry, "stage", 1, 3) ||
                entry.optDouble("stage") != entry.optInt("stage") || !optionalText(entry, "groundTexture", 120) ||
                !optionalText(entry, "wallTexture", 120) || !optionalText(entry, "potVisual", 120) ||
                !optionalText(entry, "silverPotVisual", 120) || !optionalText(entry, "architectureMaterial", 120) ||
                !optionalText(entry, "skyMaterial", 120) || !optionalText(entry, "arenaLandmark", 120) ||
                !optionalText(entry, "potBreakFx", 120) || !optionalText(entry, "moaiVisual", 120) ||
                !optionalText(entry, "balanceVisual", 120) ||
                (entry.has("replaceDecorations") && !(entry.opt("replaceDecorations") instanceof Boolean)))
                throw new IOException("invalid stage environment");
            int index = entry.optInt("stage") - 1;
            boolean architecture = !optionalString(entry, "architectureMaterial").isEmpty();
            boolean sky = !optionalString(entry, "skyMaterial").isEmpty();
            if (architecture != sky)
                throw new IOException("architecture and sky materials must be provided together");
            if (architecture && ((optionalString(entry, "groundTexture").isEmpty() && optionalString(map, "groundTexture").isEmpty()) ||
                (optionalString(entry, "wallTexture").isEmpty() && optionalString(map, "wallTexture").isEmpty())))
                throw new IOException("architecture requires ground and wall textures");
            if (entry.has("rooftops")) {
                JSONObject layout = entry.optJSONObject("rooftops");
                if (!entry.optBoolean("replaceDecorations") || layout == null ||
                    !strictNumber(layout, "roofFraction", .60, .82) ||
                    !strictNumber(layout, "bridgeFraction", .18, .40) ||
                    !strictNumber(layout, "riseFraction", .03, .10) ||
                    layout.optDouble("riseFraction") > (.96 - layout.optDouble("roofFraction")) * .55)
                    throw new IOException("invalid rooftop dimensions");
            }
            if (entry.has("streetscape")) {
                if (api != 4) throw new IOException("streetscapes require content API 4 and OpenBonk 0.1.17 or newer");
                JSONObject street = entry.optJSONObject("streetscape");
                JSONArray frontages = street == null ? null : street.optJSONArray("frontages");
                if (!entry.optBoolean("replaceDecorations") || !architecture || entry.has("rooftops") || street == null ||
                    !strictNumber(street, "tileWidth", 20, 40) || street.optDouble("tileWidth") != street.optInt("tileWidth") ||
                    !strictNumber(street, "stepHeight", 2, 12) || street.optDouble("stepHeight") != street.optInt("stepHeight") ||
                    street.optDouble("stepHeight") > street.optDouble("tileWidth") * .4 ||
                    !strictNumber(street, "coverage", 0, 1) || frontages == null || frontages.length() < 1 || frontages.length() > 8)
                    throw new IOException("invalid streetscape profile");
                Set<String> streetNames = new HashSet<>();
                for (int f = 0; f < frontages.length(); f++) {
                    Object name = frontages.opt(f);
                    if (!(name instanceof String) || blankStreetName((String)name) || ((String)name).length() > 120 ||
                        ((String)name).indexOf('\0') >= 0 || !streetNames.add((String)name))
                        throw new IOException("invalid street frontage");
                }
                needsBundle = true;
            }
            boolean landmark = !optionalString(entry, "arenaLandmark").isEmpty();
            if (landmark && (entry.has("rooftops") || !entry.optBoolean("replaceDecorations")))
                throw new IOException("arena landmarks require tiled terrain with mod decorations");
            boolean potEffect = !optionalString(entry, "potBreakFx").isEmpty();
            boolean moai = !optionalString(entry, "moaiVisual").isEmpty();
            boolean hasPotVisual = !optionalString(entry, "potVisual").isEmpty() ||
                !optionalString(entry, "silverPotVisual").isEmpty();
            if (seen[index] || (!entry.optBoolean("replaceDecorations") && optionalString(entry, "groundTexture").isEmpty() &&
                optionalString(entry, "wallTexture").isEmpty() && !moai && !hasPotVisual && !architecture && !potEffect)) throw new IOException("invalid stage environment");
            needsBundle |= moai || hasPotVisual || architecture || landmark || potEffect;
            seen[index] = true;
        }
        return needsBundle;
    }
    private static String optionalString(JSONObject entry, String key) { return entry.isNull(key) ? "" : entry.optString(key, ""); }
    private static void validateMapEncounters(JSONArray enemies, JSONArray encounters) throws IOException {
        if (enemies == null || enemies.length() < 1 || enemies.length() > 24 ||
            encounters == null || encounters.length() < 1 || encounters.length() > 3)
            throw new IOException("map enemies and encounters must be declared together");
        Set<String> keys = new HashSet<>();
        for (int i = 0; i < enemies.length(); i++) {
            JSONObject enemy = enemies.optJSONObject(i);
            if (enemy == null || !(enemy.opt("id") instanceof String) ||
                !contentId(enemy.optString("id")) || !keys.add(enemy.optString("id")) ||
                !strictText(enemy, "name", 80) || !strictText(enemy, "material", 120) ||
                !strictInteger(enemy, "hp", 1, 1000000) || !strictInteger(enemy, "damage", 1, 10000) ||
                !strictInteger(enemy, "xp", 0, 1000) || !strictInteger(enemy, "fps", 4, 24) ||
                !strictNumber(enemy, "speed", .1, 40) || !strictNumber(enemy, "cost", .1, 30) ||
                !strictNumber(enemy, "scale", .25, 12) || !strictNumber(enemy, "radius", .1, 8))
                throw new IOException("invalid local enemy");
            String movement = enemy.optString("movement");
            if (!optionalText(enemy, "attack", 12) || !optionalText(enemy, "attackMaterial", 120))
                throw new IOException("invalid enemy attack");
            String attack = optionalString(enemy, "attack");
            if (attack.isEmpty() != optionalString(enemy, "attackMaterial").isEmpty() ||
                (!attack.isEmpty() && !"sweep".equals(attack) && !"pulse".equals(attack) && !"whirlwind".equals(attack)))
                throw new IOException("unsupported enemy attack");
            boolean charge = "charge".equals(movement);
            if (!("ground".equals(movement) || "fly".equals(movement) || charge) ||
                charge != !enemy.isNull("charge"))
                throw new IOException("invalid enemy movement");
            validateEnemyClip(enemy.optJSONObject("walk"));
            if (!enemy.isNull("idle")) validateEnemyClip(enemy.optJSONObject("idle"));
            if (charge) validateEnemyClip(enemy.optJSONObject("charge"));
        }
        boolean[] seen = new boolean[3];
        Set<String> rewardIds = new HashSet<>();
        for (int i = 0; i < encounters.length(); i++) {
            JSONObject encounter = encounters.optJSONObject(i);
            if (encounter == null || !strictInteger(encounter, "stage", 1, 3))
                throw new IOException("invalid encounter stage");
            int index = encounter.optInt("stage") - 1;
            if (seen[index]) throw new IOException("duplicate encounter stage");
            seen[index] = true;
            Set<String> start = validateEnemyRoster(encounter, "start", keys, true, false);
            Set<String> reserve = validateEnemyRoster(encounter, "reserve", keys, false, true);
            for (String id : reserve) if (start.contains(id)) throw new IOException("reinforcement already starts in stage");
            validateEnemyRoster(encounter, "swarm", keys, false, false);
            validateEnemyRoster(encounter, "overtime", keys, false, false);
            validateEnemyRoster(encounter, "minibosses", keys, false, true);
            if (!optionalText(encounter, "boss", 64) ||
                (!optionalString(encounter, "boss").isEmpty() && !keys.contains(encounter.optString("boss"))))
                throw new IOException("unknown local boss");
            if (!encounter.isNull("reward")) {
                JSONObject reward = encounter.optJSONObject("reward");
                if (optionalString(encounter, "boss").isEmpty() || reward == null || !(reward.opt("id") instanceof String) ||
                    !contentId(reward.optString("id")) || !rewardIds.add(reward.optString("id")) ||
                    !strictText(reward, "name", 80) || !strictText(reward, "description", 500) ||
                    !strictText(reward, "icon", 120) || !validItemStat(reward.optString("stat")) ||
                    !strictNumber(reward, "bonus", .01, 1))
                    throw new IOException("invalid arena reward");
            }
        }
    }

    private static void validateEnemyClip(JSONObject clip) throws IOException {
        if (clip == null || !strictText(clip, "prefix", 100) || !strictInteger(clip, "frames", 1, 24))
            throw new IOException("invalid enemy animation clip");
    }

    private static Set<String> validateEnemyRoster(JSONObject encounter, String field, Set<String> keys,
        boolean required, boolean allowEmpty) throws IOException {
        Set<String> used = new HashSet<>();
        if (!required && encounter.isNull(field)) return used;
        JSONArray names = encounter.optJSONArray(field);
        if (names == null || names.length() > 12 || (!allowEmpty && names.length() == 0))
            throw new IOException("invalid enemy roster: " + field);
        for (int i = 0; i < names.length(); i++) {
            Object name = names.opt(i);
            if (!(name instanceof String) || !keys.contains(name) || !used.add((String)name))
                throw new IOException("unknown or duplicate enemy in roster: " + field);
        }
        return used;
    }
    private static void requireMapTextures(JSONObject entry, Map<String, JSONObject> files) throws Exception {
        for (String key : new String[] {"groundTexture", "wallTexture"}) {
            String path = optionalString(entry, key);
            if (!path.isEmpty() && (!files.containsKey(path) || files.get(path).getLong("size") > 4L * 1024 * 1024))
                throw new IOException("missing map texture: " + key);
        }
    }
    private static boolean text(String value, int limit) { return value != null && !value.trim().isEmpty() && value.length() <= limit && value.indexOf('\0') < 0; }
    private static boolean optionalText(JSONObject object, String key, int limit) {
        if (!object.has(key) || object.isNull(key)) return true;
        Object value = object.opt(key);
        return value instanceof String && (((String)value).isEmpty() || text((String)value, limit));
    }
    private static boolean number(JSONObject object, String key, double min, double max) { double value = object.optDouble(key, Double.NaN); return !Double.isNaN(value) && !Double.isInfinite(value) && value >= min && value <= max; }
    private static boolean strictNumber(JSONObject object, String key, double min, double max) {
        return object.opt(key) instanceof Number && number(object, key, min, max);
    }
    private static boolean strictInteger(JSONObject object, String key, int min, int max) {
        return strictNumber(object, key, min, max) && object.optDouble(key) == object.optInt(key);
    }
    private static boolean strictText(JSONObject object, String key, int length) {
        return object.opt(key) instanceof String && text(object.optString(key), length);
    }
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
