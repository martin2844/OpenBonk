using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Game.Spawning.New;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.UI.Localization;
using Assets.Scripts.Utility;

public class FinalSwarmSilverUi : MonoBehaviour
{
	public Transform coin;

	public TextMeshProUGUI t_silverMultiplier;

	public TextMeshProUGUI t_difficulty;

	public RawImage progressBar;

	public Image outlineGlow;

	private Material outlineGlowMat;

	private string modifierName = "Swarm Silver Multiplier";

	public GameObject contentParent;

	private float maxTime = 240f;

	private float maxMultiplier = 8f;

	private float lastMultiplier;

	private float nextCheckTime;

	private float checkInterval = 1f;

	public Gradient colorGradient;

	public float testTime;

	private void Start()
	{
		if (outlineGlow != null) outlineGlowMat = outlineGlow.material;
		RemoveSilverMultiplier();
		MyPlayer.A_PlayerInventoryInitialized += OnPlayerInventoryInitialized;
		SummonerController.A_FinalSwarmStarted += OnFinalSwarmStarted;
		SummonerController.A_FinalSwarmStopped += OnFinalSwarmStopped;
	}

	private void OnDestroy()
	{
		RemoveSilverMultiplier();
		MyPlayer.A_PlayerInventoryInitialized -= OnPlayerInventoryInitialized;
		SummonerController.A_FinalSwarmStarted -= OnFinalSwarmStarted;
		SummonerController.A_FinalSwarmStopped -= OnFinalSwarmStopped;
	}

	private void RemoveSilverMultiplier()
	{
		MyPlayer.Instance?.inventory?.statInventory?.RemoveMovingStat(modifierName);
	}

	private void OnFinalSwarmStarted()
	{
		if (contentParent == null) return;
		contentParent.SetActive(true);
		contentParent.transform.localScale = Vector3.zero;
	}

	private void OnFinalSwarmStopped()
	{
		if (contentParent != null) contentParent.SetActive(false);
	}

	private void Update()
	{
		if (contentParent == null || !contentParent.activeInHierarchy) return;
		UpdateMultiplier();
		contentParent.transform.localScale = Vector3.Lerp(contentParent.transform.localScale, Vector3.one, Time.deltaTime * 10f);
		if (coin != null) coin.localScale = Vector3.Lerp(coin.localScale, Vector3.one, Time.deltaTime * 8f);
		if (progressBar != null) progressBar.transform.localScale = new Vector3(GetTimerLerp(), 1f, 1f);
	}

	private void UpdateMultiplier()
	{
		if (nextCheckTime > MyTime.time) return;
		nextCheckTime = MyTime.time + checkInterval;
		float lerp = GetTimerLerp();
		float multiplier = (float)System.Math.Round(Mathf.Lerp(1f, maxMultiplier, lerp), 2);
		if (multiplier > lastMultiplier)
		{
			var inventory = MyPlayer.Instance?.inventory?.statInventory;
			if (inventory == null) return;
			inventory.ChangeMovingStat(modifierName, new StatModifier { stat = EStat.SilverIncreaseMultiplier, modifyType = EStatModifyType.Multiplication, modification = multiplier });
			if (coin != null) coin.localScale = Vector3.one * 1.5f;
			if (t_silverMultiplier != null) t_silverMultiplier.text = MyStringUtil.ShowOnlyDecimals(multiplier) + "x";
			if (t_difficulty != null) t_difficulty.text = GetDifficultyText(lerp);
			Color color = GetColor(lerp);
			if (outlineGlowMat != null) outlineGlowMat.SetColor("_Color", Color.Lerp(color, Color.white, .5f));
			if (t_difficulty != null) t_difficulty.color = Color.Lerp(color, Color.white, .5f);
			if (progressBar != null) progressBar.color = color;
		}
		lastMultiplier = multiplier;
	}

	private Color GetColor(float lerpValue)
	{
		return colorGradient != null ? colorGradient.Evaluate(lerpValue) : Color.white;
	}

	private float GetSwarmTime()
	{
		return MyTime.finalSwarmTimer;
	}

	private float GetTimerLerp()
	{
		return Mathf.Clamp01(GetSwarmTime() / maxTime);
	}

	private string GetDifficultyText(float lerpValue)
	{
		string key = lerpValue < .25f ? "DIFFICULTY_EASY" : lerpValue < .5f ? "DIFFICULTY_NORMAL" : lerpValue < .75f ? "DIFFICULTY_HARD" : "DIFFICULTY_COOKED";
		return LocalizationUtility.GetLocalizedString("Other", key);
	}

	private void OnPlayerInventoryInitialized(PlayerInventory pInv)
	{
		RemoveSilverMultiplier();
	}
}
