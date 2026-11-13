using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharacterInfoUI : MonoBehaviour
{
	public CharacterMenu characterMenu;

	public TextMeshProUGUI t_name;

	public TextMeshProUGUI t_description;

	public TextMeshProUGUI t_weaponName;

	public TextMeshProUGUI t_weaponDesc;

	public TextMeshProUGUI t_passiveName;

	public TextMeshProUGUI t_passiveDesc;

	public RawImage i_weapon;

	public RawImage i_passive;

	public SkinSelection skinSelection;

	public RequirementsContainer reqContainer;

	[Header("Ranks and runs")]
	public TextMeshProUGUI t_rank;

	public TextMeshProUGUI t_runs;

	public RawImage i_rankFrame;

	public RawImage i_rankIcon;

	public RawImage progressBar;

	public RawImage i_character;

	public RawImage i_runs;

	public GameObject star;

	private void Awake()
	{
		MyButtonCharacter.A_Select += OnCharacterSelected;
	}

	private void OnDestroy()
	{
		MyButtonCharacter.A_Select -= OnCharacterSelected;
	}

	private void OnCharacterSelected(MyButtonCharacter btn)
	{
		if (btn == null) return;
		CharacterData character = btn.characterData;
		bool unlocked = character != null && Assets.Scripts.Saves___Serialization.Progression.Unlocks.MyAchievements.IsUnlocked(character, out _);
		bool purchased = unlocked && Assets.Scripts.Saves___Serialization.Progression.Unlocks.MyAchievements.IsPurchased(character);
		bool hidden = character == null || (!unlocked && character.IsBlackedOutInCharacterSelectionScreen());
		if (t_name != null) t_name.text = hidden ? "??" : character.GetName();
		if (t_description != null)
		{
			t_description.gameObject.SetActive(purchased);
			t_description.text = unlocked ? character.GetDescription() : string.Empty;
		}
		if (reqContainer != null)
		{
			reqContainer.gameObject.SetActive(!purchased && !hidden);
			if (character != null && !purchased) reqContainer.Set(character);
		}
		if (i_character != null) i_character.texture = unlocked ? character.GetIcon() : Assets.Scripts.Managers.IconManager.Instance != null ? Assets.Scripts.Managers.IconManager.Instance.questionMark : null;
		if (t_weaponName != null) t_weaponName.text = unlocked && character.weapon != null ? character.weapon.GetName() : "???";
		if (t_weaponDesc != null) t_weaponDesc.text = unlocked && character.weapon != null ? character.weapon.GetDescription() : "???";
		if (i_weapon != null) i_weapon.texture = unlocked && character.weapon != null ? character.weapon.GetIcon() : null;
		if (t_passiveName != null) t_passiveName.text = unlocked && character.passive != null ? character.passive.GetName() : "???";
		if (t_passiveDesc != null) t_passiveDesc.text = unlocked && character.passive != null ? character.passive.GetDescription() : "???";
		if (i_passive != null) { i_passive.gameObject.SetActive(unlocked && character.passive != null); i_passive.texture = unlocked && character.passive != null ? character.passive.icon : null; }
		if (unlocked && SaveManager.Instance != null && SaveManager.Instance.progression != null)
		{
			var progress = SaveManager.Instance.progression.GetCharacterProgression(character.eCharacter);
			Assets.Scripts.Menu.Shop.Rank.Ranks.GetRankTextures(character.GetDisplayRank(), out var frame, out var rankIcon, out var rankColor, out var frameColor);
			if (i_rankFrame != null) { i_rankFrame.texture = frame; i_rankFrame.color = frameColor; }
			if (i_rankIcon != null) { i_rankIcon.texture = rankIcon; i_rankIcon.color = rankColor; }
			if (t_rank != null) t_rank.text = Assets.Scripts.UI.Localization.LocalizationUtility.GetLocalizedString("Other", "RANK", "Rank", false) + " " + character.GetDisplayRank();
			if (t_runs != null) t_runs.text = progress.numRuns + " " + Assets.Scripts.UI.Localization.LocalizationUtility.GetLocalizedString("Other", "RUNS", "Runs", false);
			if (star != null) star.SetActive(progress.HasStar());
			if (progressBar != null) progressBar.transform.localScale = new Vector3(Inventory__Items__Pickups.XpUtility.CurrentLevelProgress(progress.xp), 1f, 1f);
		}
		else
		{
			if (t_rank != null) t_rank.text = string.Empty;
			if (t_runs != null) t_runs.text = string.Empty;
			if (star != null) star.SetActive(false);
		}
		if (skinSelection != null) { if (unlocked) skinSelection.SetCharacter(btn); else skinSelection.SetNotUnlocked(); }
	}
}
