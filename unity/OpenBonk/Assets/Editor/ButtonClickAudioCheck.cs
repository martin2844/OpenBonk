using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

public sealed class ButtonClickAudioFixture : MyButton
{
    public int clicks;
    public override void StartHover() { }
    public override void StopHover() { }
    protected override void OnClick() { clicks++; }
}

public static class ButtonClickAudioCheck
{
    static void Check(bool value, string message) { if (!value) throw new Exception("ButtonClickAudioCheck: " + message); }
    public static void Run()
    {
        var oldManager = AudioManager.Instance;
        var root = new GameObject("button audio fixture"); root.SetActive(false);
        var manager = root.AddComponent<AudioManager>(); AudioManager.Instance = manager;
        var defaultSource = root.AddComponent<AudioSource>(); defaultSource.enabled = false;
        var defaultSound = root.AddComponent<RandomSfx>(); defaultSound.s = defaultSource;
        var customSource = root.AddComponent<AudioSource>(); customSource.enabled = false;
        var customSound = root.AddComponent<RandomSfx>(); customSound.s = customSource;
        var defaultClip = AudioClip.Create("default click", 1000, 1, 8000, false);
        var customClip = AudioClip.Create("custom click", 1000, 1, 8000, false);
        defaultSound.sounds = new[] { defaultClip }; manager.uiClick = defaultSound; manager.customSfx = customSound;
        var buttonObject = new GameObject("button", typeof(RectTransform), typeof(Button)); buttonObject.transform.SetParent(root.transform);
        var button = buttonObject.AddComponent<ButtonClickAudioFixture>();
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;
        try
        {
            typeof(MyButton).GetMethod("Awake", flags).Invoke(button, null);
            button.GetButton().onClick.Invoke();
            Check(button.clicks == 1, "normal click dispatched");
            Check((bool)typeof(AudioManager).GetField("queueEnter", flags).GetValue(manager), "normal click queued");
            typeof(AudioManager).GetMethod("LateUpdate", flags).Invoke(manager, null);
            Check(defaultSource.clip == defaultClip, "queued click reaches audio source");
            button.customSfx = customClip;
            button.GetButton().onClick.Invoke();
            Check(button.clicks == 2 && customSource.clip == customClip, "custom sound reaches audio source");
            Check(!(bool)typeof(AudioManager).GetField("queueEnter", flags).GetValue(manager), "custom sound does not queue default");
            AudioManager.Instance = null;
            button.GetButton().onClick.Invoke();
            Check(button.clicks == 3, "click remains usable without audio manager");
            Debug.Log("ButtonClickAudioCheck passed: actual button dispatch, default and custom audio paths, missing manager");
        }
        finally
        {
            AudioManager.Instance = oldManager;
            UnityEngine.Object.DestroyImmediate(root);
            UnityEngine.Object.DestroyImmediate(defaultClip);
            UnityEngine.Object.DestroyImmediate(customClip);
        }
    }
}
