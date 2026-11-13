using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CharacterMenu : Window
{
	public BackEscape windowBackEscape;

	public static ECharacter selectedCharacter;

	public Transform characterGridParent;

	public GameObject characterPrefabUi;

	public MyButton b_confirm;

	public MyButton b_purchase;

	public MyButton b_back;

	public MyButton b_hatPrev;

	private List<MyButtonCharacter> characterButtons;

	public SkinSelection skinSelection;

	public GameObject hatSelection;

	public MyButton b_hats;

	private MyButtonCharacter selectedButton;

	public MainMenu mainMenu;

	public UnlocksUi shopUi;

	public new void FocusWindow()
	{
		base.FocusWindow();
	}

	private new void Awake()
	{
		base.Awake();
		TryInit();
	}

	private new void OnEnable()
	{
		base.OnEnable();
		TryInit();
	}

	private void TryInit()
	{
		if (characterButtons != null || DataManager.Instance == null || characterPrefabUi == null || characterGridParent == null) return;
		characterButtons = new List<MyButtonCharacter>();
		var first = characterPrefabUi.GetComponent<MyButtonCharacter>();
		if (first == null) return;
		characterButtons.Add(first);
		var characters = new List<CharacterData>(DataManager.Instance.unsortedCharacterData);
		characters.Sort();
		for (int i = 0; i < characters.Count; i++)
		{
			if (i >= characterButtons.Count) characterButtons.Add(Instantiate(characterPrefabUi, characterGridParent).GetComponent<MyButtonCharacter>());
			characterButtons[i].SetCharacter(characters[i].isEnabled ? DataManager.Instance.GetCharacterData(characters[i].eCharacter) : null);
		}
		for (int i = 0; i < Assets.Scripts.Saves___Serialization.Progression.Unlocks.MyAchievements.fakeCharacters; i++)
		{
			var placeholder = Instantiate(characterPrefabUi, characterGridParent).GetComponent<MyButtonCharacter>();
			characterButtons.Add(placeholder);
			placeholder.SetCharacter(null);
		}
		FindAllButtonsInWindow();
		SetupButtonNavigation();
		if (SaveManager.Instance != null && SaveManager.Instance.config != null && SaveManager.Instance.config.preferences != null)
			foreach (var button in characterButtons)
				if (button.characterData != null && button.characterData.eCharacter == SaveManager.Instance.config.preferences.selectedCharacter) startBtn = button;
		MyButtonCharacter.A_Select += OnSelectCharacter;
		MyButtonCharacter.A_Confirm += OnConfirmCharacter;
	}

	private void SetupButtonNavigation()
	{
		for (int i = 0; i < characterButtons.Count; i++)
		{
			var button = characterButtons[i].GetButton();
			if (button == null) continue;
			var navigation = new Navigation { mode = Navigation.Mode.Explicit };
			if (i >= 4) navigation.selectOnUp = characterButtons[i - 4].GetButton();
			else if (b_back != null) navigation.selectOnUp = b_back.GetButton();
			if (i + 4 < characterButtons.Count) navigation.selectOnDown = characterButtons[i + 4].GetButton();
			if (i % 4 != 0) navigation.selectOnLeft = characterButtons[i - 1].GetButton();
			if (i % 4 != 3 && i + 1 < characterButtons.Count) navigation.selectOnRight = characterButtons[i + 1].GetButton();
			button.navigation = navigation;
		}
	}

	private void OnSelectCharacter(MyButtonCharacter characterButton)
	{
		if (!isActiveAndEnabled || characterButton == null) return;
		selectedButton = characterButton;
		if (b_confirm != null && b_confirm.GetButton() != null)
		{
			var navigation = b_confirm.GetButton().navigation;
			navigation.selectOnLeft = characterButton.GetButton();
			if (b_hats != null) navigation.selectOnDown = b_hats.GetButton();
			b_confirm.GetButton().navigation = navigation;
		}
		if (windowBackEscape != null) windowBackEscape.enabled = true;
		if (b_purchase != null) b_purchase.gameObject.SetActive(false);
		if (b_confirm != null) b_confirm.gameObject.SetActive(true);
		var character = characterButton.characterData;
		bool unlocked = character != null && Assets.Scripts.Saves___Serialization.Progression.Unlocks.MyAchievements.IsUnlocked(character, out _);
		bool purchased = unlocked && Assets.Scripts.Saves___Serialization.Progression.Unlocks.MyAchievements.IsPurchased(character);
		if (unlocked && !purchased)
		{
			if (b_purchase != null) b_purchase.gameObject.SetActive(true);
			if (b_confirm != null) b_confirm.gameObject.SetActive(false);
		}
		else if (b_confirm != null) b_confirm.SetInteractable(purchased);
		if (unlocked) selectedCharacter = character.eCharacter;
		if (SaveManager.Instance != null && SaveManager.Instance.config != null && SaveManager.Instance.config.preferences != null)
			SaveManager.Instance.config.preferences.selectedCharacter = selectedCharacter;
	}

	private void DisableNavigationStatWindow()
	{
		if (b_confirm == null || b_confirm.GetButton() == null) return;
		var navigation = b_confirm.GetButton().navigation;
		navigation.mode = Navigation.Mode.None;
		b_confirm.GetButton().navigation = navigation;
	}

	private void EnableNavigationStatWindow()
	{
		if (b_confirm == null || b_confirm.GetButton() == null) return;
		var navigation = b_confirm.GetButton().navigation;
		navigation.mode = Navigation.Mode.Explicit;
		navigation.selectOnLeft = selectedButton != null ? selectedButton.GetButton() : null;
		navigation.selectOnDown = b_hatPrev != null ? b_hatPrev.GetButton() : null;
		b_confirm.GetButton().navigation = navigation;
		if (skinSelection != null && selectedButton != null) skinSelection.EnableNavigation(selectedButton.GetButton());
	}

	private void OnConfirmCharacter(MyButtonCharacter characterButton)
	{
		if (!isActiveAndEnabled || characterButton == null) return;
		if (!characterButton.canUseCharacter)
		{
			if (AlwaysUi.Instance != null && AlwaysUi.Instance.UiTextPopup != null)
				AlwaysUi.Instance.UiTextPopup.SetText(characterButton.cantUseCharacterReason, characterButton.transform.position, Color.red);
			return;
		}
		MyButton target = b_purchase != null && b_purchase.gameObject.activeInHierarchy ? b_purchase : b_confirm;
		Assets.Scripts.Managers.ButtonManager.ForceHoverButton(target);
		characterButton.StartHover();
		if (windowBackEscape != null) windowBackEscape.enabled = false;
		selectedButton = characterButton;
	}

	public void BuyCharacter()
	{
		if (mainMenu == null || shopUi == null || selectedButton == null) return;
		mainMenu.GoToUnlocks();
		shopUi.FocusCharacterPurchase(selectedButton.characterData);
	}

	private new void Update()
	{
		if (MyInputManager.GetButtonDown(MyInputManager.UICancel) && windowBackEscape != null && !windowBackEscape.enabled)
			Assets.Scripts.Managers.ButtonManager.ForceHoverButton(selectedButton);
	}

	private new void OnDisable()
	{
		base.OnDisable();
	}

	private new void OnDestroy()
	{
		MyButtonCharacter.A_Select -= OnSelectCharacter;
		MyButtonCharacter.A_Confirm -= OnConfirmCharacter;
		base.OnDestroy();
	}
}
