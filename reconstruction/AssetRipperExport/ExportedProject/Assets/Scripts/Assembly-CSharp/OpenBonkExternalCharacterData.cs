using UnityEngine;

// Runtime data for external characters. A content loader must populate these
// fields and register the instance; this class does not load mods by itself.
public sealed class OpenBonkExternalCharacterData : CharacterData
{
    public string externalId;
    public string displayName;
    public string description;
    public AnimationClip walkAnimation;
    public AnimationClip idleAnimation, runAnimation, jumpAnimation, surfAnimation;
    public string surfboard;
    public float modelScale = 1f;
    // Optional authoring override; without it, known head-bone names use a
    // conservative anchor inferred from the model's bounds.
    public string hatBone;
    public Vector3 hatOffset;
    public float hatScale = 1f;
    public bool hasHatOverride;
    public override string GetName() => displayName ?? string.Empty;
    public override string GetDescription() => description ?? string.Empty;
    public override string GetInternalName() => externalId ?? string.Empty;
}
