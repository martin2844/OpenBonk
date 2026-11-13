using System;
using System.Linq;
using System.Reflection;
using Assets.Scripts.Managers;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class SelectionHoverSceneCheck
{
    static void Check(bool value, string message) { if (!value) throw new Exception("SelectionHoverSceneCheck: " + message); }
    public static void Run()
    {
        var flag = typeof(MyButton).GetField("isHovering", BindingFlags.Instance | BindingFlags.NonPublic);
        int tested = 0, scalable = 0;
        try
        {
            foreach (var name in new[] { "MainMenu", "GeneratedMap", "FinalBossMap" })
            {
                var scene = EditorSceneManager.OpenScene("Assets/ScenesAndroid/" + name + ".unity", OpenSceneMode.Single);
                var buttons = scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<SelectionGroupToggleSingleButton>(true))
                    .Where(x => x.GetType() == typeof(SelectionGroupToggleSingleButton)).ToArray();
                foreach (var button in buttons)
                {
                    bool selected = button.isSelected;
                    ButtonManager.StartedHoveringButton(button);
                    Check((bool)flag.GetValue(button), name + " focused " + button.name);
                    ButtonManager.SetNull();
                    Check(!(bool)flag.GetValue(button) && button.isSelected == selected, name + " unfocused " + button.name);
                    tested++;
                    if (button.scaleOnHover != null) scalable++;
                    Debug.Log($"SelectionHoverSceneCheck: scene={name}, button={button.name}, parent={(button.transform.parent != null ? button.transform.parent.name : "none")}, hoverTarget={(button.scaleOnHover != null ? button.scaleOnHover.name : "none")}, hoverScale={button.hoverScale}");
                }
            }
            Check(tested > 0, "actual scene has selection buttons");
            Debug.Log($"SelectionHoverSceneCheck passed: {tested} scene buttons, {scalable} with hover scale, focus and selection state");
        }
        finally
        {
            ButtonManager.SetNull();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }
    }
}
