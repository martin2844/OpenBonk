#!/usr/bin/env python3
"""Check launcher item stat validation against the game's actual EStat names."""
import os
from pathlib import Path
import re
import subprocess
import tempfile

root = Path(__file__).resolve().parents[2]
assets = root / 'reconstruction/AssetRipperExport/ExportedProject/Assets'
source = assets / 'Scripts/Assembly-CSharp/Assets/Scripts/Menu/Shop/EStat.cs'
launcher = assets / 'AndroidSplitLauncher'
sdk = Path(os.environ.get('ANDROID_JAR', '/home/your-user/Unity/Hub/Editor/2023.2.22f1/Editor/Data/PlaybackEngines/AndroidPlayer/SDK/platforms/android-35/android.jar'))
names = re.findall(r'^\s*(\w+)\s*=\s*\d+\s*,?\s*$', source.read_text(), re.M)
assert names, 'No EStat names found'
java = '''package com.megabonk.launcher;
import java.lang.reflect.Method;
import java.util.*;
public class ItemStatCheck {
  public static void main(String[] names) throws Exception {
    Class<?> importer = Class.forName("com.megabonk.launcher.LauncherContentImporter");
    Class<?> stat = Class.forName("com.megabonk.launcher.LauncherContentImporter$ItemStat");
    Set<String> expected = new HashSet<>(Arrays.asList(names));
    Set<String> actual = new HashSet<>();
    for (Object value : stat.getEnumConstants()) actual.add(((Enum<?>) value).name());
    if (!actual.equals(expected)) throw new AssertionError("EStat mismatch: " + actual + " vs " + expected);
    Method valid = importer.getDeclaredMethod("validItemStat", String.class);
    valid.setAccessible(true);
    for (String name : names) if (!(boolean)valid.invoke(null, name)) throw new AssertionError(name);
    for (String name : new String[]{"maxhealth", "MaxHeath", "0", "", " MaxHealth", "MaxHealth,Shield"})
      if ((boolean)valid.invoke(null, name)) throw new AssertionError("accepted " + name);
    if ((boolean)valid.invoke(null, (Object)null)) throw new AssertionError("accepted null");
    System.out.println("PASS: " + names.length + " canonical stats; invalid, numeric, combined, and null names rejected");
  }
}
'''
with tempfile.TemporaryDirectory(prefix='openbonk-item-stat-') as temp:
    temp = Path(temp)
    check = temp / 'ItemStatCheck.java'
    check.write_text(java)
    subprocess.run(['javac', '-cp', str(sdk), '-d', str(temp), str(launcher / 'LauncherContentImporter.java'), str(launcher / 'LauncherModRecovery.java'), str(check)], check=True)
    subprocess.run(['java', '-cp', f'{temp}:{sdk}', 'com.megabonk.launcher.ItemStatCheck', *names], check=True)
