using UnityEngine;

public class CharacterDisplay : MonoBehaviour
{
	public PlayerRenderer playerRenderer;

	public LayerMask layerMask;

	private CharacterData currentCharacter;

	public Material lockedMaterial;

	private bool usingNonOwnedSkin;

	private void Start()
	{
		MyButtonCharacter.A_Select += OnCharacterSelected;
		SkinContainer.A_Hover += OnSkinSelected;
		SkinContainer.A_HoverMouse += OnSkinSelected;
		SkinSelection.A_ForceSkinDisplay += OnSkinSelected;
		MapSelectionUi.A_MapSelectionEnabled += OnMapSelectionEnabled;
		HatSelection.A_HatChanged += OnHatChanged;
		HatSelection.A_HatHover += OnHatHover;
	}

	private void OnCharacterSelected(CharacterData characterData)
	{
		if (characterData == null || currentCharacter == characterData) return;
		currentCharacter = characterData.isEnabled ? characterData : null;
		playerRenderer.SetCharacter(currentCharacter, null, Vector3.zero);
		playerRenderer.SetIdle();
		playerRenderer.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
		if (currentCharacter != null && !Assets.Scripts.Saves___Serialization.Progression.Unlocks.MyAchievements.IsUnlocked(currentCharacter, out _))
			playerRenderer.SetMaterial(lockedMaterial);
	}

	private void OnCharacterSelected(MyButtonCharacter btn)
	{
		OnCharacterSelected(btn.characterData);
	}

	private void OnSkinSelected(SkinContainer skinContainer)
	{
		if (!Assets.Scripts.Saves___Serialization.Progression.Unlocks.MyAchievements.IsUnlocked(skinContainer.skin, out _)) return;
		usingNonOwnedSkin = !Assets.Scripts.Saves___Serialization.Progression.Unlocks.MyAchievements.IsPurchased(skinContainer.skin);
		if (playerRenderer.renderer != null) playerRenderer.SetSkin(skinContainer.skin);
	}

	private void OnMapSelectionEnabled()
	{
		if (!usingNonOwnedSkin) return;
		foreach (SkinData skin in DataManager.Instance.GetSkins(currentCharacter.eCharacter))
			if (skin.skinType == Assets.Scripts._Data.ESkinType.Default) playerRenderer.SetSkin(skin);
	}

	private void OnHatChanged()
	{
		playerRenderer.SetHat(DataManager.Instance.GetHat(SaveManager.Instance.config.preferences.GetCharacterHat(currentCharacter.eCharacter)));
	}

	private void OnHatHover(HatData hatData)
	{
		playerRenderer.SetHat(hatData);
	}

	private void OnDestroy()
	{
		MyButtonCharacter.A_Select -= OnCharacterSelected;
		SkinContainer.A_Hover -= OnSkinSelected;
		SkinContainer.A_HoverMouse -= OnSkinSelected;
		SkinSelection.A_ForceSkinDisplay -= OnSkinSelected;
		MapSelectionUi.A_MapSelectionEnabled -= OnMapSelectionEnabled;
		HatSelection.A_HatChanged -= OnHatChanged;
		HatSelection.A_HatHover -= OnHatHover;
	}
}
