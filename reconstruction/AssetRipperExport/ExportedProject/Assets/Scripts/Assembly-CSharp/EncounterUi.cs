using System.Collections.Generic;
using Assets.Scripts.UI.InGame.Rewards;
using Assets.Scripts.UI.Menu.Windows;
using TMPro;
using UnityEngine;
using Assets.Scripts.UI;
using Assets.Scripts.Managers;

public class EncounterUi : BaseEncounterWindow
{
	public TextMeshProUGUI t_name;

	public TextMeshProUGUI t_description;

	public GameObject b_generic;

	private List<EncounterButton> genericButtons = new List<EncounterButton>();

	private List<EncounterButton> rarityButtons = new List<EncounterButton>();

	public GameObject particles;

	public TabsExplicitNavigation tabsExplicitNavigation;

	private EncounterOffer[] offers;

	private float openedAtTime;

	private int rebuildAfterFrames = -1;

	private bool needRebuild;

	public override void Open(EEncounter encounterType)
	{
		openedAtTime = Time.time;
		gameObject.SetActive(true);
		particles.SetActive(true);
		EncounterData data = DataManager.Instance.GetEncounter(encounterType);
		offers = data.GetOffers();
		t_name.text = data.GetName();
		t_description.text = data.GetDescription();
		List<EncounterButton> buttons = data.HasRarity() ? rarityButtons : genericButtons;
		if (buttons.Count == 0)
		{
			buttons.Add(b_generic.GetComponent<EncounterButton>());
			buttons[0].Init(this, 0);
		}
		HideButtons();
		for (int i = 0; i < offers.Length + 1; i++)
		{
			if (i >= buttons.Count)
			{
				var newButton = Instantiate(b_generic, b_generic.transform.parent).GetComponent<EncounterButton>();
				newButton.Init(this, i);
				buttons.Add(newButton);
			}
			buttons[i].gameObject.SetActive(true);
			if (i < offers.Length) buttons[i].SetOffer(offers[i], data.HasRarity());
			else buttons[i].SetDeclineOffer(offers.Length);
		}
		tabsExplicitNavigation.Refresh();
		rebuildAfterFrames = 3;
	}

	private void Update()
	{
		KeyboardInput();
	}

	private void KeyboardInput()
	{
		if (Time.time < openedAtTime + .15) return;
		int index = -1;
		if (Input.GetKeyDown(KeyCode.Alpha1)) index = 0;
		if (Input.GetKeyDown(KeyCode.Alpha2)) index = 1;
		if (Input.GetKeyDown(KeyCode.Alpha3)) index = 2;
		if (Input.GetKeyDown(KeyCode.Alpha4)) index = 3;
		if (Input.GetKeyDown(KeyCode.Alpha5)) index = 4;
		if (index >= 0 && index < offers.Length) ChooseOffer(index);
	}

	private void LateUpdate()
	{
		if (rebuildAfterFrames >= 0 && --rebuildAfterFrames == 0)
		{
			UiUtility.RebuildUi(transform);
			ButtonManager.Refresh();
		}
	}

	private void HideButtons()
	{
		foreach (var button in genericButtons) button.gameObject.SetActive(false);
		foreach (var button in rarityButtons) button.gameObject.SetActive(false);
	}

	public override void OnClose()
	{
		gameObject.SetActive(false);
		particles.SetActive(false);
	}

	public override void ChooseOffer(int index)
	{
		if (index < offers.Length) offers[index].ApplyEffects();
		UiManager.Instance.encounterWindows.RewardFinished();
	}
}
