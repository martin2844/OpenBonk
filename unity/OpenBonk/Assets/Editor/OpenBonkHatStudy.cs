#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Isolated authoring study; no game save or runtime scene is modified.
public static class OpenBonkHatStudy
{
    public static void Inspect()
    {
        var model = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/OpenBonkModAuthoring/Pikachu/pikachu-study.fbx");
        if (model == null) throw new InvalidDataException("model missing");
        var instance = UnityEngine.Object.Instantiate(model); instance.transform.localScale = Vector3.one * .24f;
        try
        {
            var head = instance.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "Head_Armature");
            if (head == null) throw new InvalidDataException("head missing");
            Debug.Log($"OPENBONK_HAT_STUDY head pos={head.position.ToString("F4")} rot={head.eulerAngles.ToString("F3")} scale={head.lossyScale.ToString("F5")} local={head.localPosition.ToString("F4")}");
            foreach (var r in instance.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                Debug.Log($"OPENBONK_HAT_STUDY renderer={r.name} bounds center={r.bounds.center.ToString("F3")} size={r.bounds.size.ToString("F3")}");
            foreach (string name in new[] { "WizardHat", "SantaHat", "CrownHat", "GlassesHat", "TopHat" })
            {
                var hat = AssetDatabase.LoadAssetAtPath<HatData>($"Assets/MonoBehaviour/{name}.asset");
                if (hat == null || hat.mesh == null) { Debug.LogWarning($"OPENBONK_HAT_STUDY missing {name}"); continue; }
                var orientation = hat.orientations.Find(x => x.character == ECharacter.Fox);
                Debug.Log($"OPENBONK_HAT_STUDY {name} mesh center={hat.mesh.bounds.center.ToString("F3")} size={hat.mesh.bounds.size.ToString("F3")} refPos={orientation?.pos.ToString("F3")} refRot={orientation?.rot.ToString("F3")} refScale={orientation?.scale.ToString("F3")}");
            }
        }
        finally { UnityEngine.Object.DestroyImmediate(instance); }
    }
}
#endif
