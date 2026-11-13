#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine;

internal sealed class AndroidModImportReceiver : MonoBehaviour
{
    internal AndroidModsMenu menu;
    private GameObject receiver;
    internal void EnsureObject()
    {
        if (receiver != null) return;
        receiver = new GameObject("AndroidModImportReceiver");
        receiver.transform.SetParent(transform, false);
        receiver.AddComponent<AndroidModImportCallback>().owner = this;
    }
    internal void Complete(string result) { if (menu != null) menu.OnImportResult(result); }
}

internal sealed class AndroidModImportCallback : MonoBehaviour
{
    internal AndroidModImportReceiver owner;
    public void OnPickerResult(string result) { if (owner != null) owner.Complete(result); }
}
#endif
