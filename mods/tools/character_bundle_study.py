#!/usr/bin/env python3
"""Rebuild an extracted character prefab and clips as a local Android bundle.

This is an authoring experiment, not a complete mod converter. An AssetRipper
Unity export, Unity Android support and any rights to the supplied assets must
be obtained separately. The output is not an installable OpenBonk mod.
"""
import argparse
import json
import os
from pathlib import Path
import shutil
import subprocess

EDITOR = r'''using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class OpenBonkCharacterStudy {
    public static void Build() {
        var prefabPath = Environment.GetEnvironmentVariable("OPENBONK_STUDY_PREFAB");
        var clips = Environment.GetEnvironmentVariable("OPENBONK_STUDY_CLIPS").Split(';');
        var output = Environment.GetEnvironmentVariable("OPENBONK_STUDY_OUTPUT");
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null || prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length == 0)
            throw new InvalidDataException("Prefab is missing a skinned mesh");
        var shader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/OpenBonkStudy.shader");
        if (shader == null) throw new InvalidDataException("Study shader missing");
        foreach (var guid in AssetDatabase.FindAssets("t:Material")) {
            var material = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
            if (material == null) continue;
            material.shader = shader;
            EditorUtility.SetDirty(material);
        }
        AssetDatabase.SaveAssets();
        var paths = new List<string> { prefabPath };
        foreach (var clip in clips) {
            if (AssetDatabase.LoadAssetAtPath<AnimationClip>(clip) == null)
                throw new InvalidDataException("Missing animation: " + clip);
            paths.Add(clip);
        }
        Directory.CreateDirectory(output);
        var spec = new AssetBundleBuild { assetBundleName = "character-android", assetNames = paths.ToArray() };
        var result = BuildPipeline.BuildAssetBundles(output, new[] { spec }, BuildAssetBundleOptions.ChunkBasedCompression, BuildTarget.Android);
        if (result == null || !File.Exists(Path.Combine(output, "character-android")))
            throw new InvalidDataException("Android bundle build failed");
        Debug.Log("OPENBONK_CHARACTER_STUDY_OK");
    }
}
'''

SHADER = r'''Shader "OpenBonk/CharacterStudy" {
 Properties { _MainTex ("Texture", 2D) = "white" {} _Color ("Color", Color) = (1,1,1,1) }
 SubShader { Tags { "RenderType"="Opaque" } Pass {
 CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
 struct v2f { float2 uv : TEXCOORD0; float4 vertex : SV_POSITION; };
 sampler2D _MainTex; float4 _MainTex_ST; fixed4 _Color;
 v2f vert(appdata v) { v2f o; o.vertex=UnityObjectToClipPos(v.vertex); o.uv=TRANSFORM_TEX(v.uv,_MainTex); return o; }
 fixed4 frag(v2f i) : SV_Target { return tex2D(_MainTex,i.uv)*_Color; }
 ENDCG
 } }
}
'''


def asset_path(root, text):
    path = Path(text)
    if path.is_absolute() or '..' in path.parts or ';' in text or not text.startswith('Assets/'):
        raise ValueError('asset paths must be relative to Assets/ and may not contain semicolons')
    resolved = (root / path).resolve(strict=True)
    if not resolved.is_relative_to((root / 'Assets').resolve()) or not resolved.is_file():
        raise ValueError('asset must be a file inside the exported Assets directory')
    return path.as_posix()


def omit_foreign_code(directory, names):
    """Do not import executable content from a third-party Unity export."""
    return [n for n in names if n.lower() in ('scripts', 'editor', 'plugins') or
            n.lower().endswith(('.cs', '.dll', '.asmdef', '.rsp'))]


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--export', required=True, type=Path, help='AssetRipper ExportedProject')
    parser.add_argument('--unity', required=True, type=Path, help='Unity editor executable with Android support')
    parser.add_argument('--prefab', required=True, help='prefab path relative to export, e.g. Assets/.../Model.prefab')
    parser.add_argument('--clip', action='append', required=True, help='animation path relative to export; repeat as needed')
    parser.add_argument('--output', required=True, type=Path, help='new output directory outside the export and checkout')
    parser.add_argument('--runner', help='optional executable wrapper for Unity')
    args = parser.parse_args()
    project = args.export.expanduser().resolve(strict=True)
    unity = args.unity.expanduser().resolve(strict=True)
    output = args.output.expanduser().absolute()
    repo = Path(__file__).resolve().parents[2]
    if not unity.is_file() or not os.access(unity, os.X_OK): parser.error('Unity executable unavailable')
    if not (project / 'Assets').is_dir(): parser.error('export must contain Assets')
    if output.exists() or output.is_symlink() or not output.parent.is_dir(): parser.error('output must be new with an existing parent')
    output = output.parent.resolve() / output.name
    if output.is_relative_to(project) or project.is_relative_to(output) or output.is_relative_to(repo):
        parser.error('output must be outside the export and checkout')
    try:
        prefab = asset_path(project, args.prefab)
        clips = [asset_path(project, path) for path in args.clip]
    except (ValueError, OSError) as exc:
        parser.error(str(exc))
    if len(set(clips)) != len(clips): parser.error('duplicate animation paths')
    if args.runner and not shutil.which(args.runner): parser.error('runner unavailable')
    if any(path.is_symlink() for path in (project / 'Assets').rglob('*')):
        parser.error('export must not contain symbolic links')
    prefix = ([args.runner] if args.runner else []) + [str(unity)]
    try:
        version = subprocess.run(prefix + ['-version'], text=True, capture_output=True, timeout=30, check=True)
    except (OSError, subprocess.SubprocessError) as exc:
        parser.error('cannot check Unity version: ' + str(exc))
    if version.stdout.strip() != '2023.2.22f1': parser.error('Unity 2023.2.22f1 is required')
    # Unity must not import arbitrary C# or native plugins from a foreign bundle.
    output.mkdir(mode=0o700)
    work = output / 'project'; assets = work / 'Assets'
    shutil.copytree(project / 'Assets', assets, ignore=omit_foreign_code, symlinks=False)
    (work / 'Packages').mkdir(); (work / 'ProjectSettings').mkdir(); (assets / 'Editor').mkdir(exist_ok=True)
    (work / 'Packages/manifest.json').write_text(json.dumps({'dependencies': {
        'com.unity.modules.animation': '1.0.0', 'com.unity.modules.assetbundle': '1.0.0',
        'com.unity.modules.audio': '1.0.0'}}) + '\n', encoding='utf-8')
    shutil.copyfile(repo / 'unity/OpenBonk/ProjectSettings/ProjectVersion.txt', work / 'ProjectSettings/ProjectVersion.txt')
    (assets / 'Editor/OpenBonkCharacterStudy.cs').write_text(EDITOR, encoding='utf-8')
    (assets / 'OpenBonkStudy.shader').write_text(SHADER, encoding='utf-8')
    env = os.environ.copy()
    env.update(OPENBONK_STUDY_PREFAB=prefab, OPENBONK_STUDY_CLIPS=';'.join(clips), OPENBONK_STUDY_OUTPUT=str(output / 'bundle'))
    command = prefix + ['-batchmode', '-nographics', '-quit',
        '-projectPath', str(work), '-buildTarget', 'Android', '-executeMethod', 'OpenBonkCharacterStudy.Build',
        '-logFile', str(output / 'unity.log')]
    with (output / 'console.log').open('x', encoding='utf-8') as log:
        subprocess.run(command, env=env, stdout=log, stderr=subprocess.STDOUT, check=True)
    log = (output / 'unity.log').read_text(encoding='utf-8', errors='replace')
    if 'OPENBONK_CHARACTER_STUDY_OK' not in log or not (output / 'bundle/character-android').is_file():
        raise RuntimeError('Unity did not confirm bundle creation; inspect unity.log')
    print('Android study bundle:', output / 'bundle/character-android')


if __name__ == '__main__':
    main()
