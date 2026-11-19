#!/usr/bin/env python3
"""Validate actual Nimbo releases and the activity's current auto-enable methods."""
import argparse
import os
from pathlib import Path
import subprocess
import tempfile

p = argparse.ArgumentParser()
p.add_argument('legacy_zip', type=Path)
p.add_argument('release_zip', type=Path)
p.add_argument('--java-home', type=Path, default=os.environ.get('OPENBONK_JAVA_HOME'))
p.add_argument('--json-jar', type=Path, default=os.environ.get('OPENBONK_TEST_JSON_JAR'))
a = p.parse_args()
if a.java_home is None or a.json_jar is None:
    p.error('--java-home and --json-jar are required (or OPENBONK_JAVA_HOME / OPENBONK_TEST_JSON_JAR)')
repo = Path(__file__).resolve().parents[2]
source = repo / 'unity/OpenBonk/Assets/AndroidSplitLauncher'
text = (source / 'SplitDataLauncher.java').read_text()
def method(signature):
    start = text.index(signature)
    brace = text.index('{', start)
    depth = 1
    end = brace + 1
    while depth:
        if text[end] == '{': depth += 1
        if text[end] == '}': depth -= 1
        end += 1
    return text[start:end]
# Both real UI entry points must call the helper after importer success.
for signature in ('private void importMod(android.net.Uri uri)', 'private void importOnlineMod(JSONObject item, Runnable refresh)'):
    body = method(signature)
    assert body.index('LauncherModImporter.install(') < body.index('enableContentMod(root, id)'), signature
with tempfile.TemporaryDirectory(prefix='actual-street-java-') as temp:
    out = Path(temp)
    wrapper = out / 'SplitDataLauncher.java'
    wrapper.write_text('''package com.megabonk.launcher;
import java.io.*;
import java.nio.charset.StandardCharsets;
import org.json.JSONObject;
import android.content.SharedPreferences;
final class SplitDataLauncher {
private static final int MODE_PRIVATE = 0;
private final SharedPreferences prefs;
SplitDataLauncher(SharedPreferences prefs) { this.prefs = prefs; }
SharedPreferences getSharedPreferences(String name, int mode) { return prefs; }
''' + method('static boolean validModHooks(JSONObject manifest)') + '\n' +
        method('private JSONObject readInstalledMod(File folder)') + '\n' +
        method('private void enableContentMod(File root, String id)') + '\n}\n')
    files = [repo / 'tools/mod-recovery-check/android/content/SharedPreferences.java',
             repo / 'tools/mod-recovery-check/com/megabonk/launcher/RecoveryCheck.java', wrapper,
             repo / 'tools/mod-import-check/com/megabonk/launcher/ActualStreetPackageCheck.java']
    files += [source / (name + '.java') for name in ('LauncherModHookValidation', 'LauncherModIntegrity', 'LauncherModRecovery', 'LauncherModImporter', 'LauncherContentImporter')]
    subprocess.run([str(a.java_home / 'bin/javac'), '-cp', str(a.json_jar), '-d', str(out), *map(str, files)], check=True)
    subprocess.run([str(a.java_home / 'bin/java'), '-cp', str(out) + os.pathsep + str(a.json_jar),
                    'com.megabonk.launcher.ActualStreetPackageCheck', str(a.legacy_zip.resolve()), str(a.release_zip.resolve())], check=True)
