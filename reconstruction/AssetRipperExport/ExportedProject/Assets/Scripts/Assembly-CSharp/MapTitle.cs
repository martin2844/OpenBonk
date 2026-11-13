using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Assets.Scripts.Game.Other;
using Assets.Scripts._Data.MapsAndStages;
using Assets.Scripts.Managers;
using Assets.Scripts.UI.Localization;
using Assets.Scripts.Utility;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;

public class MapTitle : MonoBehaviour
{
	[CompilerGenerated]
	private sealed class _003CTypeDescription_003Ed__22 : IEnumerator<object>, IEnumerator, IDisposable
	{
		private int _003C_003E1__state;

		private object _003C_003E2__current;

		public MapTitle _003C_003E4__this;

		private string _003Cdescription_003E5__2;

		private int _003Ci_003E5__3;

		object IEnumerator<object>.Current
		{
			[DebuggerHidden]
			get
			{
				return null;
			}
		}

		object IEnumerator.Current
		{
			[DebuggerHidden]
			get
			{
				return null;
			}
		}

		[DebuggerHidden]
		public _003CTypeDescription_003Ed__22(int _003C_003E1__state)
		{
		}

		[DebuggerHidden]
		void IDisposable.Dispose()
		{
		}

		private bool MoveNext()
		{
			return false;
		}

		bool IEnumerator.MoveNext()
		{
			//ILSpy generated this explicit interface implementation from .override directive in MoveNext
			return this.MoveNext();
		}

		[DebuggerHidden]
		void IEnumerator.Reset()
		{
		}
	}

	private float delay = 0.5f;

	private float visibleTime = 2f;

	private float fadeTime = 2f;

	public TextMeshProUGUI t_title;

	public TextMeshProUGUI t_description;

	public TextMeshProUGUI t_tier;

	public CanvasGroup titleCanvasGroup;

	private float alphaTimer;

	public StageData testStage;

	public LocalizedString cryptTitle;

	public LocalizedString cryptDesc;

	private bool started;

	private RunConfig runConfig;

	private int numTimesShowed;

	private float totalTimer;

	private bool hasPlayedSfx;

	private bool isFading;

	private bool isTyping;

	private int numDescriptionsShowed;

	public AudioSource textSfx;

	public RandomSfx letterSfx;

	private bool done;

	public void StartAnimation()
	{
		if (MapController.IsFinalBossStage()) return;
		gameObject.SetActive(true);
		started = true;
		done = false;
		alphaTimer = 0f;
		totalTimer = 0f;
		hasPlayedSfx = false;
		isFading = false;
		isTyping = false;
		t_description.text = "";
		runConfig = MapController.runConfig;
		if (runConfig == null) return;
		if (MapController.currentMap.eMap == EMap.Graveyard) SetGraveyardText();
		else SetMapText();
		titleCanvasGroup.transform.localScale = Vector3.one * .8f;
		titleCanvasGroup.alpha = 0f;
	}

	private void SetGraveyardText()
	{
		if (numTimesShowed > 0) SetMapText();
		else
		{
			t_tier.text = "";
			t_title.text = cryptTitle.GetLocalizedString();
		}
		numTimesShowed++;
	}

	private void SetMapText()
	{
		var localized = LocalizationUtility.GetLocalizedStringReference("Other", "TIER_SMART");
		t_tier.text = localized.GetLocalizedString(new Dictionary<string, object> { ["tier"] = (runConfig.mapTierIndex + 1).ToString() });
		t_tier.color = MyColorUtility.GetTierColor(runConfig.mapTierIndex);
		t_title.text = GetTitle();
	}

	private void Update()
	{
		if (done || !started) return;
		totalTimer += MyTime.deltaTime;
		if (totalTimer < delay) return;
		if (!hasPlayedSfx)
		{
			hasPlayedSfx = true;
			if (textSfx != null) textSfx.Play();
		}
		if (alphaTimer < 1f && !isFading)
		{
			alphaTimer = Mathf.Clamp01(alphaTimer + MyTime.deltaTime / fadeTime);
			titleCanvasGroup.alpha = UnityEngine.UIElements.Experimental.Easing.InOutQuad(alphaTimer);
			if (alphaTimer >= .3f && !isTyping) StartCoroutine(TypeDescription());
		}
		titleCanvasGroup.transform.localScale = Vector3.Lerp(titleCanvasGroup.transform.localScale, Vector3.one, MyTime.deltaTime * .75f);
		if (totalTimer <= delay + fadeTime + visibleTime) return;
		isFading = true;
		if (alphaTimer > 0f)
		{
			alphaTimer = Mathf.Clamp01(alphaTimer - MyTime.deltaTime / fadeTime);
			titleCanvasGroup.alpha = UnityEngine.UIElements.Experimental.Easing.InOutQuad(alphaTimer);
		}
		if (alphaTimer <= 0f) { done = true; gameObject.SetActive(false); }
	}

	private IEnumerator TypeDescription()
	{
		isTyping = true;
		string description = GetDescription();
		t_description.text = "";
		t_description.gameObject.SetActive(true);
		for (int i = 0; i < description.Length; i++)
		{
			t_description.text += description[i];
			if (letterSfx != null) letterSfx.Play();
			yield return new WaitForSeconds(.05f);
		}
	}

	private string GetTitle()
	{
		if (MapController.IsFinalBossStage())
		{
			var localized = LocalizationUtility.GetLocalizedStringReference("Maps", "MAP_RIFT");
			return localized.GetLocalizedString(new Dictionary<string, object> { ["mapname"] = runConfig.mapData.GetName() });
		}
		return StageTitle(MapController.currentMap, MapController.currentStage);
	}

	private string GetDescription()
	{
		if (MapController.currentMap.eMap == EMap.Graveyard && numDescriptionsShowed++ == 0)
			return cryptDesc.GetLocalizedString();
		if (MapController.IsFinalBossStage()) return "???";
		return StageDescription(MapController.currentMap, MapController.currentStage);
	}

	public static string StageTitle(MapData map, StageData stage) => map is OpenBonkExternalMapData ? map.GetName() : stage.GetName();
	public static string StageDescription(MapData map, StageData stage) => map is OpenBonkExternalMapData ? map.GetDescription() : stage.GetDescription();
}
