using System;
using System.Collections.Generic;
using System.Reflection;
using Assets.Scripts._Data.Hats;
using Assets.Scripts.Saves___Serialization.SaveFiles;
using Assets.Scripts.Settings___Saves.SaveFiles.ConfigSaves;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public static class PlayerHatPortCheck
{
    static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
    static void Set(object instance, string name, object value) => instance.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(instance, value);
    public static void Run()
    {
        var root = new GameObject("PlayerHatCheck");
        var model = new GameObject("model"); model.transform.SetParent(root.transform);
        var head = new GameObject("Head"); head.transform.SetParent(model.transform);
        var hatObject = new GameObject("hat"); hatObject.transform.SetParent(root.transform);
        var mainMesh = new Mesh(); var lowMesh = new Mesh(); var hatMesh = new Mesh();
        var character = ScriptableObject.CreateInstance<CharacterData>();
        var hat = ScriptableObject.CreateInstance<HatData>();
        var secondHat = ScriptableObject.CreateInstance<HatData>();
        var material = new Material(Shader.Find("Unlit/Color"));
        var oldSave = SaveManager.Instance;
        var oldData = DataManager.Instance;
        try
        {
            var player = root.AddComponent<PlayerRenderer>();
            player.renderer = model.AddComponent<SkinnedMeshRenderer>();
            Set(player, "rendererObject", model); Set(player, "characterData", character);
            Set(player, "hatTransform", hatObject.transform);
            Set(player, "hatRenderer", hatObject.AddComponent<MeshRenderer>());
            Set(player, "hatFilter", hatObject.AddComponent<MeshFilter>());
            character.eCharacter = ECharacter.Fox; character.meshDefault = mainMesh; character.meshLow = lowMesh;
            hat.name = "TestHat"; hat.eHat = EHat.Crown; hat.mesh = hatMesh; hat.material = material;
            hat.orientations = new List<HatOrientation> { new HatOrientation { character = ECharacter.Fox, meshForHat = EMeshForHat.Low, pos = new Vector3(1, 2, 3), rot = new Vector3(0, 90, 0), scale = Vector3.one * 2 } };
            Require(hat.GetInternalName() == "TestHat_hat" && hat.GetUnlockableRequirement() == null, "hat identity");
            hat.sortingPriority = 1; secondHat.sortingPriority = 2;
            Require(hat.CompareTo(secondHat) < 0, "hat priority");
            player.SetHat(hat);
            Require(player.GetCurrentHat() == hat && hatObject.activeSelf && hatObject.transform.parent == head.transform, "hat attachment");
            Require(hatObject.GetComponent<MeshFilter>().sharedMesh == hatMesh && hatObject.GetComponent<MeshRenderer>().sharedMaterial == material, "hat mesh and material");
            Require(hatObject.transform.localPosition == new Vector3(1, 2, 3) && hatObject.transform.localScale == Vector3.one * 2 && Quaternion.Angle(hatObject.transform.localRotation, Quaternion.Euler(0, 90, 0)) < 0.01f, "character orientation");
            Require(player.renderer.sharedMesh == lowMesh, "alternate character mesh");
            player.SetHat(null);
            Require(player.GetCurrentHat() == null && !hatObject.activeSelf && player.renderer.sharedMesh == mainMesh, "remove hat and restore mesh");
            var save = root.AddComponent<SaveManager>(); save.config = new ConfigSaveFile(); save.progression = new ProgressionSaveFile();
            typeof(SaveManager).GetProperty("Instance").GetSetMethod(true).Invoke(null, new object[] { save });
            var data = root.AddComponent<DataManager>(); DataManager.Instance = data;
            secondHat.price = 100;
            data.unsortedHats = new List<HatData> { hat, secondHat };
            var selection = root.AddComponent<HatSelection>();
            var popupObject = new GameObject("HatPopup", typeof(RectTransform)); popupObject.transform.SetParent(root.transform);
            var popup = popupObject.AddComponent<HatSelectionPopupWindow>();
            selection.hatSelectionPopupWindow = popup; popup.hatSelection = selection;
            var buttonObject = new GameObject("HatButton", typeof(RectTransform), typeof(Button), typeof(MyButtonHat)); buttonObject.transform.SetParent(popupObject.transform);
            var hatButton = buttonObject.GetComponent<MyButtonHat>(); hatButton.hatSelectionPopupWindow = popup;
            hatButton.background = buttonObject.AddComponent<RawImage>();
            hatButton.i_hatIcon = new GameObject("HatIcon", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
            hatButton.i_hatIcon.transform.SetParent(buttonObject.transform);
            popup.hatButtonPrefab = buttonObject;
            selection.t_hatName = new GameObject("HatName", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
            selection.t_hatName.transform.SetParent(root.transform);
            selection.i_hatIcon = new GameObject("SelectedIcon", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
            selection.i_hatIcon.transform.SetParent(root.transform);
            selection.textSizer = root.AddComponent<TextSizer>(); selection.textWrapper = root.AddComponent<ButtonTextWrapper>();
            typeof(HatSelection).GetMethod("OnEnable", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(selection, null);
            var buttons = (List<MyButtonHat>)typeof(HatSelectionPopupWindow).GetField("hatButtons", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(popup);
            Require(buttons.Count == 2 && buttons[0].hatData == null && buttons[1].hatData == hat, "only owned hats and no-hat option");
            int hoverEvents = 0, changeEvents = 0;
            Action<HatData> hover = h => { if (h == hat) hoverEvents++; };
            Action changed = () => changeEvents++;
            HatSelection.A_HatHover += hover; HatSelection.A_HatChanged += changed;
            buttons[1].StartHover();
            Require(hoverEvents == 1 && save.config.preferences.GetCharacterHat(ECharacter.Fox) == EHat.None, "hover previews without changing save");
            popup.ClickButton(hat);
            HatSelection.A_HatHover -= hover; HatSelection.A_HatChanged -= changed;
            Require(selection.selectedHatData == hat && save.config.preferences.GetCharacterHat(ECharacter.Fox) == EHat.Crown && changeEvents == 1, "hat selection saved and announced");
            Debug.Log("PlayerHatPortCheck passed: attachment, orientation, alternate mesh, owned list, preview and selection");
        }
        finally
        {
            DataManager.Instance = oldData;
            typeof(SaveManager).GetProperty("Instance").GetSetMethod(true).Invoke(null, new object[] { oldSave });
            UnityEngine.Object.DestroyImmediate(root); UnityEngine.Object.DestroyImmediate(mainMesh); UnityEngine.Object.DestroyImmediate(lowMesh);
            UnityEngine.Object.DestroyImmediate(hatMesh); UnityEngine.Object.DestroyImmediate(character); UnityEngine.Object.DestroyImmediate(hat);
            UnityEngine.Object.DestroyImmediate(secondHat); UnityEngine.Object.DestroyImmediate(material);
        }
    }
}
