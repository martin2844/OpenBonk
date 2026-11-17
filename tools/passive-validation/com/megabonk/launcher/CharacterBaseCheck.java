package com.megabonk.launcher;
import java.io.*;
import java.nio.charset.StandardCharsets;
import java.nio.file.*;
import java.util.*;
import java.util.zip.*;
import org.json.*;

/** Verify optional character data rejects malformed updates before replacement. */
public class CharacterBaseCheck {
 static JSONObject stat(String stat, String mode, Object amount) {
  JSONObject result = new JSONObject().put("stat", stat).put("mode", mode);
  if (amount != null) result.put("amount", amount);
  return result;
 }
 static void rewrite(Path source, Path dest, String key, Object value, boolean remove) throws Exception {
  try (ZipFile in = new ZipFile(source.toFile()); ZipOutputStream out = new ZipOutputStream(Files.newOutputStream(dest))) {
   Enumeration<? extends ZipEntry> entries = in.entries();
   while (entries.hasMoreElements()) {
    ZipEntry entry = entries.nextElement(); if (entry.isDirectory()) continue;
    byte[] data; try (InputStream stream = in.getInputStream(entry)) { data = PassiveCheck.bytes(stream); }
    if (entry.getName().equals("manifest.json")) {
     JSONObject manifest = new JSONObject(new String(data, StandardCharsets.UTF_8));
     JSONObject character = manifest.getJSONObject("character");
     if (remove) character.remove(key); else character.put(key, value);
     data = manifest.toString().getBytes(StandardCharsets.UTF_8);
    }
    out.putNextEntry(new ZipEntry(entry.getName())); out.write(data); out.closeEntry();
   }
  }
 }
 static JSONArray manyStats(int count) {
  String[] names = {"MaxHealth","HealthRegen","Shield","Thorns","Armor","Evasion","Evolve",
   "DamageReductionMultiplier","DamageCooldownMultiplier","SizeMultiplier","DurationMultiplier",
   "ProjectileSpeedMultiplier","DamageMultiplier","EffectDurationMultiplier","AttackSpeed",
   "Projectiles","Lifesteal","CritChance","CritDamage","FireDamage","IceDamage","LightningDamage"};
  String[] modes = {"Flat", "Addition", "Multiplication"}; JSONArray out = new JSONArray();
  for (int i = 0; i < count; i++) out.put(stat(names[i % names.length], modes[i / names.length], 1));
  return out;
 }
 public static void main(String[] args) throws Exception {
  Path source = Paths.get(args[0]), temp = Files.createTempDirectory("character-base-validation-");
  try {
   Path root = Files.createDirectory(temp.resolve("Mods")), zip = temp.resolve("mod.zip");
   RecoveryCheck.Prefs prefs = new RecoveryCheck.Prefs();
   PassiveCheck.install(source, root, prefs); prefs.state.put("pikachu", true);
   Map<String,String> before = PassiveCheck.hashes(root.resolve("pikachu"));
   List<Object[]> invalid = new ArrayList<>();
   invalid.add(new Object[]{"baseStats", "bad"}); invalid.add(new Object[]{"baseStats", new JSONArray().put(JSONObject.NULL)});
   invalid.add(new Object[]{"baseStats", new JSONArray().put(stat("0", "Flat", 1))});
   invalid.add(new Object[]{"baseStats", new JSONArray().put(stat("Unused0", "Flat", 1))});
   invalid.add(new Object[]{"baseStats", new JSONArray().put(stat("Luck", "0", 1))});
   invalid.add(new Object[]{"baseStats", new JSONArray().put(stat("Luck", "Flat", null))});
   invalid.add(new Object[]{"baseStats", new JSONArray().put(stat("Luck", "Flat", 10001))});
   invalid.add(new Object[]{"baseStats", new JSONArray().put(stat("Luck", "Flat", "NaN"))});
   invalid.add(new Object[]{"baseStats", new JSONArray().put(stat("Luck", "Flat", "Infinity"))});
   invalid.add(new Object[]{"baseStats", new JSONArray().put(stat("Luck", "Multiplication", -.1))});
   invalid.add(new Object[]{"baseStats", new JSONArray().put(stat("Luck", "Addition", -1.01))});
   invalid.add(new Object[]{"baseStats", new JSONArray().put(stat("Luck", "Flat", 1)).put(stat("Luck", "Flat", 2))});
   invalid.add(new Object[]{"baseStats", manyStats(65)});
   invalid.add(new Object[]{"collider", "bad"});
   invalid.add(new Object[]{"collider", new JSONObject().put("height", 1).put("width", 1)});
   invalid.add(new Object[]{"collider", new JSONObject().put("height", 2)});
   invalid.add(new Object[]{"collider", new JSONObject().put("height", 21).put("width", .7)});
   for (Object[] entry : invalid) {
    rewrite(source, zip, (String) entry[0], entry[1], false);
    boolean rejected = false;
    try { PassiveCheck.install(zip, root, prefs); } catch (IOException expected) { rejected = true; }
    if (!rejected || !before.equals(PassiveCheck.hashes(root.resolve("pikachu"))) || !prefs.getBoolean("pikachu", false))
     throw new AssertionError("invalid update accepted or existing state changed: " + entry[1]);
   }
   rewrite(source, zip, "baseStats", manyStats(64), false); PassiveCheck.install(zip, root, prefs);
   rewrite(source, zip, "baseStats", new JSONArray(), false); PassiveCheck.install(zip, root, prefs);
   rewrite(source, zip, "baseStats", JSONObject.NULL, false); PassiveCheck.install(zip, root, prefs);
   rewrite(source, zip, "baseStats", null, true); PassiveCheck.install(zip, root, prefs);
   rewrite(source, zip, "collider", new JSONObject().put("height", 3.75).put("width", .7), false); PassiveCheck.install(zip, root, prefs);
   rewrite(source, zip, "collider", JSONObject.NULL, false); PassiveCheck.install(zip, root, prefs);
   rewrite(source, zip, "collider", null, true); PassiveCheck.install(zip, root, prefs);
   System.out.println("PASS: " + invalid.size() + " invalid updates preserve installed files and enabled state; valid and legacy character configs import");
  } finally {
   try (java.util.stream.Stream<Path> paths = Files.walk(temp)) {
    for (Path path : (Iterable<Path>) paths.sorted(Comparator.reverseOrder())::iterator) Files.deleteIfExists(path);
   }
  }
 }
}
