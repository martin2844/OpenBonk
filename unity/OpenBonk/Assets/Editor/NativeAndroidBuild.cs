#if UNITY_EDITOR
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Reproducible native Android player build entry points.</summary>
public static class NativeAndroidBuild
{
	internal static bool SplitLauncherProbeEnabled;
	private static string BuildPath(string projectRoot, string relativePath)
	{
		string configured = System.Environment.GetEnvironmentVariable("OPENBONK_BUILD_ROOT");
		string buildRoot = string.IsNullOrEmpty(configured)
			? Path.GetFullPath(Path.Combine(projectRoot, "../../../build"))
			: Path.GetFullPath(configured);
		return Path.GetFullPath(Path.Combine(buildRoot, relativePath));
	}
	public static void BuildArm64Apk()
	{
		BuildApk(ScriptingImplementation.IL2CPP, AndroidArchitecture.ARM64, "Megabonk-arm64-il2cpp.apk", true);
	}

	public static void BuildArm64SideBySideApk()
	{
		string oldName = PlayerSettings.productName;
		string oldId = PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Android);
		try
		{
			PlayerSettings.productName = "Megabonk Test";
			BuildApk(ScriptingImplementation.IL2CPP, AndroidArchitecture.ARM64, "Megabonk-arm64-test.apk", true, "com.megabonk.nativeport.test");
		}
		finally
		{
			PlayerSettings.productName = oldName;
			PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, oldId);
		}
	}

	// Single-pass research build for evaluating the current IL2CPP runtime.
	// The launcher data hash is not updated here; this is not an installer.
	public static void BuildArm64RuntimeResearchApk()
	{
		string oldName = PlayerSettings.productName;
		string oldId = PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Android);
		bool oldSplit = PlayerSettings.Android.splitApplicationBinary;
		AndroidSdkVersions oldTargetSdk = PlayerSettings.Android.targetSdkVersion;
		string projectRoot = Directory.GetParent(Application.dataPath).FullName;
		string output = BuildPath(projectRoot, "android/Megabonk-arm64-runtime-research.apk");
		string obb = BuildPath(projectRoot, "android/Megabonk-arm64-runtime-research.main.obb");
		if (File.Exists(output) || File.Exists(obb)) throw new System.InvalidOperationException("Research output already exists");
		try
		{
			PlayerSettings.productName = "OpenBonk";
			PlayerSettings.Android.splitApplicationBinary = true;
			SplitLauncherProbeEnabled = true;
			BuildApk(ScriptingImplementation.IL2CPP, AndroidArchitecture.ARM64, "Megabonk-arm64-runtime-research.apk", true, "com.megabonk.nativeport.splittest");
		}
		finally
		{
			SplitLauncherProbeEnabled = false;
			PlayerSettings.Android.splitApplicationBinary = oldSplit;
			PlayerSettings.Android.targetSdkVersion = oldTargetSdk;
			PlayerSettings.productName = oldName;
			PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, oldId);
		}
	}

	// Experimental local Gradle export for assembling the launcher. The export
	// includes game-derived data and is not a distributable APK.
	public static void ExportArm64RuntimeGradleProject()
	{
		string oldName = PlayerSettings.productName;
		string oldId = PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Android);
		bool oldSplit = PlayerSettings.Android.splitApplicationBinary;
		bool oldExport = EditorUserBuildSettings.exportAsGoogleAndroidProject;
		AndroidSdkVersions oldTargetSdk = PlayerSettings.Android.targetSdkVersion;
		string root = Directory.GetParent(Application.dataPath).FullName;
		string output = BuildPath(root, "android/OpenBonk-gradle-project");
		if (File.Exists(output) || Directory.Exists(output))
			throw new System.InvalidOperationException("Gradle export output already exists");
		try
		{
			PlayerSettings.productName = "OpenBonk";
			PlayerSettings.Android.splitApplicationBinary = true;
			EditorUserBuildSettings.exportAsGoogleAndroidProject = true;
			SplitLauncherProbeEnabled = true;
			BuildApk(ScriptingImplementation.IL2CPP, AndroidArchitecture.ARM64, "OpenBonk-gradle-project", true, "com.megabonk.nativeport.splittest");
		}
		finally
		{
			SplitLauncherProbeEnabled = false;
			EditorUserBuildSettings.exportAsGoogleAndroidProject = oldExport;
			PlayerSettings.Android.splitApplicationBinary = oldSplit;
			PlayerSettings.Android.targetSdkVersion = oldTargetSdk;
			PlayerSettings.productName = oldName;
			PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, oldId);
		}
	}

	public static void BuildArm64ArrowResearchApk()
	{
		string oldName = PlayerSettings.productName;
		string oldId = PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Android);
		bool oldSplit = PlayerSettings.Android.splitApplicationBinary;
		string root = Directory.GetParent(Application.dataPath).FullName;
		string output = BuildPath(root, "android/OpenBonk-arrow-research.apk");
		string obb = BuildPath(root, "android/OpenBonk-arrow-research.main.obb");
		if (File.Exists(output) || File.Exists(obb)) throw new System.InvalidOperationException("Research output already exists");
		try
		{
			PlayerSettings.productName = "OpenBonk Runtime Research";
			PlayerSettings.Android.splitApplicationBinary = true;
			SplitLauncherProbeEnabled = true;
			BuildApk(ScriptingImplementation.IL2CPP, AndroidArchitecture.ARM64, "OpenBonk-arrow-research.apk", true, "com.megabonk.nativeport.splittest");
		}
		finally
		{
			SplitLauncherProbeEnabled = false;
			PlayerSettings.Android.splitApplicationBinary = oldSplit;
			PlayerSettings.productName = oldName;
			PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, oldId);
		}
	}

	public static void BuildArm64GoldResearchApk()
	{
		string oldName = PlayerSettings.productName;
		string oldId = PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Android);
		bool oldSplit = PlayerSettings.Android.splitApplicationBinary;
		string root = Directory.GetParent(Application.dataPath).FullName;
		string output = BuildPath(root, "android/OpenBonk-gold-research.apk");
		string obb = BuildPath(root, "android/OpenBonk-gold-research.main.obb");
		if (File.Exists(output) || File.Exists(obb)) throw new System.InvalidOperationException("Research output already exists");
		try
		{
			PlayerSettings.productName = "OpenBonk Runtime Research";
			PlayerSettings.Android.splitApplicationBinary = true;
			SplitLauncherProbeEnabled = true;
			BuildApk(ScriptingImplementation.IL2CPP, AndroidArchitecture.ARM64, "OpenBonk-gold-research.apk", true, "com.megabonk.nativeport.splittest");
		}
		finally
		{
			SplitLauncherProbeEnabled = false;
			PlayerSettings.Android.splitApplicationBinary = oldSplit;
			PlayerSettings.productName = oldName;
			PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, oldId);
		}
	}

	// Experiment: keep the existing Test/Preview packages and settings intact
	// while measuring Unity's APK + external expansion-file packaging.
	public static void BuildArm64SplitDataProbeApk()
	{
		string oldName = PlayerSettings.productName;
		string oldId = PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Android);
		bool oldSplit = PlayerSettings.Android.splitApplicationBinary;
		string projectRoot = Directory.GetParent(Application.dataPath).FullName;
		string generatedJava = Path.Combine(projectRoot, "Assets/AndroidSplitLauncher/SplitExpectedData.java");
		string oldJava = File.ReadAllText(generatedJava);
		string obb = BuildPath(projectRoot, "android/Megabonk-arm64-split-test.main.obb");
		string splitDiagnosticDir = System.Environment.GetEnvironmentVariable("OPENBONK_SPLIT_DIAGNOSTIC_DIR");
		try
		{
			PlayerSettings.productName = "Megabonk Split Test";
			PlayerSettings.Android.splitApplicationBinary = true;
			SplitLauncherProbeEnabled = true;
			BuildApk(ScriptingImplementation.IL2CPP, AndroidArchitecture.ARM64, "Megabonk-arm64-split-test.apk", true, "com.megabonk.nativeport.splittest");
			if (!string.IsNullOrEmpty(splitDiagnosticDir)) File.Copy(obb, Path.Combine(splitDiagnosticDir, "first.obb"), false);
			string expected = HashFile(obb);
			File.WriteAllText(generatedJava, "package com.megabonk.launcher;\nfinal class SplitExpectedData { static final String SHA256 = \"" + expected + "\", LEGACY_SHA256 = \"\"; }\n");
			AssetDatabase.ImportAsset("Assets/AndroidSplitLauncher/SplitExpectedData.java", ImportAssetOptions.ForceUpdate);
			BuildApk(ScriptingImplementation.IL2CPP, AndroidArchitecture.ARM64, "Megabonk-arm64-split-test.apk", true, "com.megabonk.nativeport.splittest");
			if (!string.IsNullOrEmpty(splitDiagnosticDir))
			{
				File.Copy(obb, Path.Combine(splitDiagnosticDir, "second.obb"), false);
				File.Copy(Path.Combine(Path.GetDirectoryName(obb), "Megabonk-arm64-split-test.apk"), Path.Combine(splitDiagnosticDir, "second.apk"), false);
			}
			if (HashFile(obb) != expected) throw new System.Exception("Split data changed between build passes; APK and OBB must not be distributed");
			Debug.Log("Split-data expected SHA-256: " + expected);
		}
		finally
		{
			File.WriteAllText(generatedJava, oldJava);
			AssetDatabase.ImportAsset("Assets/AndroidSplitLauncher/SplitExpectedData.java", ImportAssetOptions.ForceUpdate);
			SplitLauncherProbeEnabled = false;
			PlayerSettings.Android.splitApplicationBinary = oldSplit;
			PlayerSettings.productName = oldName;
			PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, oldId);
		}
	}

	// Experimental launcher with Unity's game data embedded in a single APK.
	// Reuses the split-test package ID so its optional ZIP imports and profile
	// carry over on upgrade; Preview remains a separate application.
	public static void BuildArm64EmbeddedLauncherApk()
	{
		string oldName = PlayerSettings.productName;
		string oldId = PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Android);
		bool oldSplit = PlayerSettings.Android.splitApplicationBinary;
		string projectRoot = Directory.GetParent(Application.dataPath).FullName;
		string generatedJava = Path.Combine(projectRoot, "Assets/AndroidSplitLauncher/SplitExpectedData.java");
		string oldJava = File.ReadAllText(generatedJava);
		try
		{
			PlayerSettings.productName = "OpenBonk";
			PlayerSettings.Android.splitApplicationBinary = false;
			SplitLauncherProbeEnabled = true;
			File.WriteAllText(generatedJava, "package com.megabonk.launcher;\nfinal class SplitExpectedData { static final String SHA256 = \"EMBEDDED\", LEGACY_SHA256 = \"\"; }\n");
			AssetDatabase.ImportAsset("Assets/AndroidSplitLauncher/SplitExpectedData.java", ImportAssetOptions.ForceUpdate);
			BuildApk(ScriptingImplementation.IL2CPP, AndroidArchitecture.ARM64, "Megabonk-arm64-launcher-test.apk", true, "com.megabonk.nativeport.splittest");
		}
		finally
		{
			File.WriteAllText(generatedJava, oldJava);
			AssetDatabase.ImportAsset("Assets/AndroidSplitLauncher/SplitExpectedData.java", ImportAssetOptions.ForceUpdate);
			SplitLauncherProbeEnabled = false;
			PlayerSettings.Android.splitApplicationBinary = oldSplit;
			PlayerSettings.productName = oldName;
			PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, oldId);
		}
	}

	private static string HashFile(string path)
	{
		using (var sha = System.Security.Cryptography.SHA256.Create())
		using (var stream = File.OpenRead(path))
			return System.BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
	}

	public static void BuildArm64PreviewApk()
	{
		string oldName = PlayerSettings.productName;
		string oldId = PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Android);
		try
		{
			PlayerSettings.productName = "Megabonk Preview";
			BuildApk(ScriptingImplementation.IL2CPP, AndroidArchitecture.ARM64, "Megabonk-arm64-preview.apk", true, "com.megabonk.nativeport.preview");
		}
		finally
		{
			PlayerSettings.productName = oldName;
			PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, oldId);
		}
	}

	public static void BuildArm64MovementProbeApk()
	{
		string oldName = PlayerSettings.productName;
		string oldId = PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Android);
		try
		{
			PlayerSettings.productName = "Megabonk Movement Test";
			BuildApk(ScriptingImplementation.IL2CPP, AndroidArchitecture.ARM64, "Megabonk-arm64-movement-test.apk", true, "com.megabonk.nativeport.movementtest");
		}
		finally
		{
			PlayerSettings.productName = oldName;
			PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, oldId);
		}
	}

	public static void BuildArm64UnstrippedApk()
	{
		PlayerSettings.SetManagedStrippingLevel(BuildTargetGroup.Android, ManagedStrippingLevel.Disabled);
		BuildApk(ScriptingImplementation.IL2CPP, AndroidArchitecture.ARM64, "Megabonk-arm64-unstripped.apk", true);
	}

	public static void BuildArmv7MonoApk()
	{
		// Unity 2023.2 ships its Android Mono runtime for ARMv7 only. The
		// ARM64 player is IL2CPP, built by BuildArm64Apk above.
		BuildApk(ScriptingImplementation.Mono2x, AndroidArchitecture.ARMv7, "Megabonk-armv7-mono.apk");
	}

	public static void BuildArmv7MonoSteamworksStubApk()
	{
		BuildApk(ScriptingImplementation.Mono2x, AndroidArchitecture.ARMv7, "Megabonk-armv7-mono-steamworks-stub.apk");
	}

	public static void BuildArmv7MonoUiDiagnosticApk()
	{
		BuildApk(ScriptingImplementation.Mono2x, AndroidArchitecture.ARMv7, "Megabonk-armv7-ui-diagnostic.apk");
	}

	public static void BuildArmv7MonoGameplayApk()
	{
		BuildApk(ScriptingImplementation.Mono2x, AndroidArchitecture.ARMv7, "Megabonk-armv7-gameplay-reconstruction.apk");
	}

	private static void BuildApk(ScriptingImplementation backend, AndroidArchitecture architecture, string fileName, bool sanitizeAndroidScenes = false, string applicationId = "com.megabonk.nativeport")
	{
		const BuildTargetGroup targetGroup = BuildTargetGroup.Android;

		EditorUserBuildSettings.SwitchActiveBuildTarget(targetGroup, BuildTarget.Android);
		EditorUserBuildSettings.buildAppBundle = false;
		PlayerSettings.SetApplicationIdentifier(targetGroup, applicationId);
		PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
		PlayerSettings.allowedAutorotateToPortrait = false;
		PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
		PlayerSettings.allowedAutorotateToLandscapeLeft = true;
		PlayerSettings.allowedAutorotateToLandscapeRight = true;
		PlayerSettings.Android.targetArchitectures = architecture;
		PlayerSettings.SetScriptingBackend(targetGroup, backend);
		PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
		string androidTarget = System.Environment.GetEnvironmentVariable("OPENBONK_ANDROID_TARGET_SDK");
		if (string.IsNullOrEmpty(androidTarget) || androidTarget == "33")
			PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel33;
		else if (androidTarget == "35")
			PlayerSettings.Android.targetSdkVersion = (AndroidSdkVersions)35;
		else
			throw new System.InvalidOperationException("Unsupported OPENBONK_ANDROID_TARGET_SDK");
		PlayerSettings.Android.bundleVersionCode = 1;

		string projectRoot = Directory.GetParent(Application.dataPath).FullName;
		string absoluteOutput = BuildPath(projectRoot, Path.Combine("android", fileName));
		Directory.CreateDirectory(Path.GetDirectoryName(absoluteOutput));

		string[] scenes = sanitizeAndroidScenes ? PrepareAndroidScenes() : new[]
		{
			"Assets/Scenes/BootScene.unity",
			"Assets/Scenes/MainMenu.unity",
			"Assets/Scenes/GeneratedMap.unity",
			"Assets/Scenes/LoadingScreen.unity",
			"Assets/Scenes/FinalBossMap.unity"
		};

		BuildReport report;
		if (sanitizeAndroidScenes)
		{
			report = BuildAndroidPlayerWithDesktopOnlyPreloadsRemoved(scenes, absoluteOutput);
		}
		else
		{
			report = BuildPlayer(scenes, absoluteOutput);
		}

		if (report.summary.result != BuildResult.Succeeded)
		{
			throw new System.Exception("Native Android APK build failed: " + report.summary.result);
		}

		string outputKind = EditorUserBuildSettings.exportAsGoogleAndroidProject ? "Gradle project" : "APK";
		Debug.Log("Built native Android " + outputKind + " (" + architecture + ", " + backend + "): " + absoluteOutput + " (" + report.summary.totalSize + " bytes)");
	}

	private static BuildReport BuildPlayer(string[] scenes, string absoluteOutput)
	{
		bool codeProbe = System.Environment.GetEnvironmentVariable("OPENBONK_NO_REWIRED_CODE") == "1";
		bool sceneProbe = codeProbe || System.Environment.GetEnvironmentVariable("OPENBONK_NO_REWIRED_SCENE") == "1";
		string probeCacheMarker = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Library/OpenBonk-rewired-probe-cache");
		bool clean = sceneProbe || File.Exists(probeCacheMarker);
		// Keep the marker until a subsequent ordinary build succeeds, including
		// after a failed or interrupted probe, so its cached scenes cannot leak.
		if (sceneProbe) File.WriteAllText(probeCacheMarker, "probe\n");
		var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
		{
			scenes = scenes,
			locationPathName = absoluteOutput,
			target = BuildTarget.Android,
			extraScriptingDefines = codeProbe ? new[] { "OPENBONK_NO_REWIRED" } : System.Array.Empty<string>(),
			options = clean ? BuildOptions.CleanBuildCache : BuildOptions.None
		});
		if (!sceneProbe && report.summary.result == BuildResult.Succeeded && File.Exists(probeCacheMarker)) File.Delete(probeCacheMarker);
		return report;
	}

	private static BuildReport BuildAndroidPlayerWithDesktopOnlyPreloadsRemoved(string[] scenes, string absoluteOutput)
	{
		string projectRoot = Directory.GetParent(Application.dataPath).FullName;
		string projectSettingsPath = Path.Combine(projectRoot, "ProjectSettings/ProjectSettings.asset");
		string[] managedPluginPaths =
		{
			"Assets/Plugins/Rewired_Core.dll",
			"Assets/Plugins/Coffee.UIParticle.dll",
			"Assets/Plugins/com.rlabrecque.steamworks.net.dll"
		};
		string[] normalizedPluginPaths =
		{
			BuildPath(projectRoot, "android/managed-plugins/Rewired_Core.dll"),
			BuildPath(projectRoot, "android/managed-plugins/Coffee.UIParticle.dll"),
			BuildPath(projectRoot, "android/managed-plugins/com.rlabrecque.steamworks.net.dll")
		};
		string originalSettings = File.ReadAllText(projectSettingsPath);
		var originalPluginBytes = new byte[managedPluginPaths.Length][];
		var normalizedPluginBytes = new byte[managedPluginPaths.Length][];
		for (int i = 0; i < managedPluginPaths.Length; i++)
		{
			originalPluginBytes[i] = File.ReadAllBytes(Path.Combine(projectRoot, managedPluginPaths[i]));
			normalizedPluginBytes[i] = File.ReadAllBytes(normalizedPluginPaths[i]);
		}
		const string desktopOnlyPreload = "  - {fileID: 11400000, guid: fdaaa6919a450484ea3dd0c16b60eebe, type: 2}\n";
		try
		{
			File.WriteAllText(projectSettingsPath, originalSettings.Replace(desktopOnlyPreload, ""));
			for (int i = 0; i < managedPluginPaths.Length; i++)
				File.WriteAllBytes(Path.Combine(projectRoot, managedPluginPaths[i]), normalizedPluginBytes[i]);
			AssetDatabase.Refresh();
			foreach (string pluginPath in managedPluginPaths)
				AssetDatabase.ImportAsset(pluginPath, ImportAssetOptions.ForceUpdate);
			if (System.Environment.GetEnvironmentVariable("OPENBONK_NO_REWIRED_PLUGINS") == "1")
			{
				if (System.Environment.GetEnvironmentVariable("OPENBONK_NO_REWIRED_CODE") != "1")
					throw new System.InvalidOperationException("Excluding Rewired plugins requires the no-Rewired code probe");
				string responseFile = Path.Combine(projectRoot, "Assets/csc.rsp");
				if (!File.Exists(responseFile) || !System.Array.Exists(File.ReadAllLines(responseFile), line => line.Trim() == "-define:OPENBONK_NO_REWIRED"))
					throw new System.InvalidOperationException("Rewired plugin probe requires OPENBONK_NO_REWIRED in Assets/csc.rsp for the Addressables compilation too");
				foreach (string pluginPath in new[] { "Assets/Plugins/Rewired_Core.dll", "Assets/Plugins/Rewired_Linux.dll" })
				{
					var importer = AssetImporter.GetAtPath(pluginPath) as PluginImporter;
					if (importer == null) throw new System.InvalidOperationException("Missing plugin importer: " + pluginPath);
					importer.SetIncludeInBuildDelegate(path => false);
					Debug.Log("Rewired plugin probe: requested build exclusion for " + pluginPath);
				}
			}
			return BuildPlayer(scenes, absoluteOutput);
		}
		finally
		{
			if (System.Environment.GetEnvironmentVariable("OPENBONK_NO_REWIRED_PLUGINS") == "1")
				foreach (string pluginPath in new[] { "Assets/Plugins/Rewired_Core.dll", "Assets/Plugins/Rewired_Linux.dll" })
					try { (AssetImporter.GetAtPath(pluginPath) as PluginImporter)?.SetIncludeInBuildDelegate(null); }
					catch (System.Exception e) { Debug.LogWarning("Could not reset temporary plugin delegate: " + e.Message); }
			for (int i = 0; i < managedPluginPaths.Length; i++)
				File.WriteAllBytes(Path.Combine(projectRoot, managedPluginPaths[i]), originalPluginBytes[i]);
			File.WriteAllText(projectSettingsPath, originalSettings);
			AssetDatabase.Refresh();
			foreach (string pluginPath in managedPluginPaths)
				AssetDatabase.ImportAsset(pluginPath, ImportAssetOptions.ForceUpdate);
		}
	}

	private static string[] PrepareAndroidScenes()
	{
		const string androidSceneDirectory = "Assets/ScenesAndroid";
		string androidCoinEffectGuid = PrepareAndroidCoinEffectPrefab();
		string projectRoot = Directory.GetParent(Application.dataPath).FullName;
		Directory.CreateDirectory(Path.Combine(projectRoot, androidSceneDirectory));
		AssetDatabase.Refresh();

		string[] sourceScenes =
		{
			"Assets/Scenes/BootScene.unity",
			"Assets/Scenes/MainMenu.unity",
			"Assets/Scenes/GeneratedMap.unity",
			"Assets/Scenes/LoadingScreen.unity",
			"Assets/Scenes/FinalBossMap.unity"
		};
		var androidScenes = new List<string>(sourceScenes.Length);
		foreach (string sourcePath in sourceScenes)
		{
			string androidPath = androidSceneDirectory + "/" + Path.GetFileName(sourcePath);
			string sceneYaml = File.ReadAllText(Path.Combine(projectRoot, sourcePath));
			sceneYaml = sceneYaml.Replace("5ac2f9a279b6c734fbc3c2dd5ad587e5", androidCoinEffectGuid);
			sceneYaml = System.Text.RegularExpressions.Regex.Replace(
				sceneYaml,
				"(?m)^  _controllerDataFiles: \\{fileID: [^\\r\\n]+\\r?\\n",
				"  _controllerDataFiles: {fileID: 0}\n");
			File.WriteAllText(Path.Combine(projectRoot, androidPath), sceneYaml);
			AssetDatabase.ImportAsset(androidPath, ImportAssetOptions.ForceUpdate);
			Scene scene = EditorSceneManager.OpenScene(androidPath, OpenSceneMode.Single);
			int removed = 0;
			foreach (GameObject root in scene.GetRootGameObjects())
			foreach (Component component in root.GetComponentsInChildren<Component>(true))
			{
				if (component == null) continue;
				System.Type componentType = component.GetType();
				string assembly = componentType.Assembly.GetName().Name;
				string typeNamespace = componentType.Namespace ?? "";
				bool isMenuCoinAttractor = Path.GetFileName(sourcePath) == "MainMenu.unity" &&
					component is Coffee.UIExtensions.UIParticleAttractor && component.gameObject.name == "Particle Attractor";
				bool isUnsupportedPlugin = assembly == "Coffee.UIParticle" || assembly == "Rewired_Core" ||
					(typeNamespace == "Rewired" || typeNamespace.StartsWith("Rewired."));
				if (!isUnsupportedPlugin || isMenuCoinAttractor) continue;
				Object.DestroyImmediate(component, true);
				removed++;
			}
			if (Path.GetFileName(sourcePath) == "GeneratedMap.unity")
			{
				foreach (GameObject root in scene.GetRootGameObjects())
				foreach (RsgController crypt in root.GetComponentsInChildren<RsgController>(true))
				{
					if (crypt.navmeshSurface == null) continue;
					crypt.navmeshSurface.layerMask = 1 << LayerMask.NameToLayer("Ground");
					EditorUtility.SetDirty(crypt.navmeshSurface);
				}
				foreach (GameObject root in scene.GetRootGameObjects())
				foreach (ProceduralTileGeneration generator in root.GetComponentsInChildren<ProceduralTileGeneration>(true))
				{
					generator.stairsMesh = LoadTilePrefab("StairMesh");
					generator.flatTile = LoadTilePrefab("TileGround");
					generator.slopeTile = LoadTilePrefab("TileSlope");
					generator.ceilingTile = LoadTilePrefab("TileCeiling");
					generator.wallFlat = LoadTilePrefab("TileWall");
					generator.wallLeftUp = LoadTilePrefab("TileWallSlopeLeftUp");
					generator.wallLeftDown = LoadTilePrefab("TileWallSlopeLeftDown");
					generator.wallLeftCross = LoadTilePrefab("TileWallSlopeLeftDownCross");
					EditorUtility.SetDirty(generator);
				}
			}
			EditorSceneManager.SaveScene(scene);
			EditorSceneManager.CloseScene(scene, true);
			Debug.Log("Prepared Android scene " + androidPath + "; removed unsupported plugin components=" + removed);
			androidScenes.Add(androidPath);
		}
		AssetDatabase.Refresh();
		return androidScenes.ToArray();
	}

	private static GameObject LoadTilePrefab(string name)
	{
		GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/" + name + ".prefab");
		if (prefab == null) throw new System.Exception("Missing procedural tile prefab: " + name);
		return prefab;
	}

	private static string PrepareAndroidCoinEffectPrefab()
	{
		const string androidPrefabDirectory = "Assets/GameObjectAndroid";
		const string androidPrefabPath = androidPrefabDirectory + "/ClaimCoinsEffectNew.prefab";
		Directory.CreateDirectory(Path.Combine(Directory.GetParent(Application.dataPath).FullName, androidPrefabDirectory));
		if (!File.Exists(Path.Combine(Directory.GetParent(Application.dataPath).FullName, androidPrefabPath)))
		{
			if (!AssetDatabase.CopyAsset("Assets/GameObject/ClaimCoinsEffectNew.prefab", androidPrefabPath))
				throw new System.Exception("Could not create Android coin effect prefab");
		}
		// Regenerate from the exported original so earlier sanitized copies regain
		// the UIParticle renderer while retaining the Android prefab's own GUID.
		string projectRoot = Directory.GetParent(Application.dataPath).FullName;
		File.Copy(Path.Combine(projectRoot, "Assets/GameObject/ClaimCoinsEffectNew.prefab"), Path.Combine(projectRoot, androidPrefabPath), true);
		AssetDatabase.ImportAsset(androidPrefabPath, ImportAssetOptions.ForceUpdate);
		GameObject prefabRoot = PrefabUtility.LoadPrefabContents(androidPrefabPath);
		try
		{
			int removed = 0;
			foreach (Component component in prefabRoot.GetComponentsInChildren<Component>(true))
			{
				if (component == null) continue;
				string assembly = component.GetType().Assembly.GetName().Name;
				if (assembly != "Rewired_Core") continue;
				Object.DestroyImmediate(component, true);
				removed++;
			}
			PrefabUtility.SaveAsPrefabAsset(prefabRoot, androidPrefabPath);
			Debug.Log("Prepared Android coin effect prefab; removed unsupported plugin components=" + removed);
		}
		finally
		{
			PrefabUtility.UnloadPrefabContents(prefabRoot);
		}
		AssetDatabase.Refresh();
		return AssetDatabase.AssetPathToGUID(androidPrefabPath);
	}
}
#endif
