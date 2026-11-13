using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class UnderlineDiagnostic
{
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
        int count = 0;
        foreach (var text in Resources.FindObjectsOfTypeAll<TextMeshProUGUI>())
        {
            if (text.text != "PLAY" && text.text != "SETTINGS" && text.text != "CREDITS") continue;
            text.ForceMeshUpdate(true, true);
            var info = text.textInfo; int vertices = 0, visible = 0;
            for (int i = 0; i < info.meshInfo.Length; i++) vertices += info.meshInfo[i].vertexCount;
            for (int i = 0; i < info.characterCount; i++) if (info.characterInfo[i].isVisible) visible++;
            Debug.Log($"UNDERLINE-DIAG text={text.text} active={text.gameObject.activeInHierarchy} style={text.fontStyle} font={text.font?.name} mat={text.fontSharedMaterial?.name} chars={info.characterCount} visible={visible} vertices={vertices} rich={text.richText} raw={text.text}"); count++;
        }
        if (count == 0) throw new Exception("No menu labels found");
    }
}
