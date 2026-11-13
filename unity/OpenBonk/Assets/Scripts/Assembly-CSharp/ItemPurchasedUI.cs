using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Assets.Scripts.Saves___Serialization.Progression.Achievements;
using Assets.Scripts.Saves___Serialization.SaveFiles;
using Assets.Scripts.UI.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemPurchasedUI : MonoBehaviour
{
	[CompilerGenerated]
	private sealed class _003CShowNewItem_003Ed__24 : IEnumerator<object>, IEnumerator, IDisposable
	{
		private int _003C_003E1__state;

		private object _003C_003E2__current;

		public ItemPurchasedUI _003C_003E4__this;

		public UnlockableBase unlockable;

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
		public _003CShowNewItem_003Ed__24(int _003C_003E1__state)
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

	[CompilerGenerated]
	private sealed class _003CShowNewItem_003Ed__25 : IEnumerator<object>, IEnumerator, IDisposable
	{
		private int _003C_003E1__state;

		private object _003C_003E2__current;

		public ItemPurchasedUI _003C_003E4__this;

		public MyAchievement achievement;

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
		public _003CShowNewItem_003Ed__25(int _003C_003E1__state)
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

	public GameObject content;

	public RawImage background;

	public RawImage itemDisplay;

	public TextMeshProUGUI itemNameText;

	public TextMeshProUGUI extraText;

	public ParticleSystem ps;

	private bool hasClaimedAchievement;

	private float fadeInTime = .6f;

	private float fadeOutTime = .2f;

	private float cardDesiredScale;

	private float textDesiredScale;

	private float desiredAlpha = .99f;

	private float yRotation = 1000f;

	private float animatorTime;

	private float animatorSpeed = .8f;

	public AudioSource sfx;

	public Texture silverIcon;

	private void Awake()
	{
		UnlocksFooter.A_Purchased += OnItemPurchased;
		ProgressionSaveFile.A_AchievementClaimed += OnAchievementClaimed;
		SkinContainer.A_Purchased += OnSkinPurchased;
	}

	private void OnDestroy()
	{
		UnlocksFooter.A_Purchased -= OnItemPurchased;
		ProgressionSaveFile.A_AchievementClaimed -= OnAchievementClaimed;
		SkinContainer.A_Purchased -= OnSkinPurchased;
	}

	private void OnSkinPurchased(SkinData skinData)
	{
		StartCoroutine(ShowNewItem(skinData));
	}

	private void OnItemPurchased(UnlockableBase unlockable)
	{
		StartCoroutine(ShowNewItem(unlockable));
	}

	private void OnAchievementClaimed(MyAchievement achievement)
	{
	}

	private void Update()
	{
		Animate();
	}

	private void Test()
	{
	}

	private void Animate()
	{
		float card = UnityEngine.UIElements.Experimental.Easing.OutElastic(Mathf.Clamp01(animatorTime * .85f));
		float cardTurn = UnityEngine.UIElements.Experimental.Easing.OutElastic(Mathf.Clamp01(animatorTime * .5f));
		float textTime = Mathf.Clamp01((animatorTime - .1f) * .5f);
		float textScale = UnityEngine.UIElements.Experimental.Easing.OutPower(textTime, 20);
		float textTurn = UnityEngine.UIElements.Experimental.Easing.OutElastic(textTime);
		float extraScale = UnityEngine.UIElements.Experimental.Easing.OutPower(Mathf.Clamp01(animatorTime - .5f), 10);
		if (itemDisplay != null) { itemDisplay.transform.localScale = Vector3.one * card; itemDisplay.transform.localRotation = Quaternion.Euler(0f, (cardTurn - 1f) * 10f, 0f); }
		if (itemNameText != null) { itemNameText.transform.localScale = Vector3.one * textScale; itemNameText.transform.localRotation = Quaternion.Euler(0f, (textTurn - 1f) * 5f, 0f); }
		if (extraText != null) extraText.transform.localScale = Vector3.one * extraScale;
		yRotation = Mathf.Lerp(yRotation, 0f, Time.deltaTime * 3f);
		animatorTime += Time.deltaTime * animatorSpeed;
	}

	private IEnumerator ShowNewItem(UnlockableBase unlockable)
	{
		yield return new WaitForSeconds(.1f);
		if (unlockable == null) yield break;
		ShowContent();
		if (itemDisplay != null) itemDisplay.texture = unlockable.GetIcon();
		if (itemNameText != null) { itemNameText.text = unlockable.GetName(); itemNameText.transform.localScale = Vector3.zero; }
		if (extraText != null) extraText.text = LocalizationUtility.GetLocalizedString("MainMenuOther", "ITEM_PURCHASED", new Dictionary<string, string> { ["item"] = unlockable.GetUnlockableTypeDisplayString() }) + " <size=130%><sprite name=\"Check\" color=#04ff00>";
		yRotation = 720f;
		animatorTime = 0f;
	}

	private IEnumerator ShowNewItem(MyAchievement achievement)
	{
		yield return new WaitForSeconds(.1f);
		if (achievement == null) yield break;
		ShowContent();
		if (itemDisplay != null) itemDisplay.texture = silverIcon;
		if (itemNameText != null) { itemNameText.text = achievement.GetRewardString() ?? ""; itemNameText.transform.localScale = Vector3.zero; }
		if (extraText != null) extraText.text = LocalizationUtility.GetLocalizedString("MainMenuOther", "JUICE") + " <size=130%><sprite name=\"Check\" color=#04ff00>";
		yRotation = 720f;
		animatorTime = 0f;
	}

	private void ShowContent()
	{
		if (content != null) content.SetActive(true);
		if (sfx != null) sfx.Play();
		if (ps != null) ps.Play();
		if (background != null) { background.CrossFadeAlpha(0f, 0f, true); background.CrossFadeAlpha(desiredAlpha, fadeInTime, false); }
	}

	public void Close()
	{
		StopAllCoroutines();
		if (content != null) content.SetActive(false);
		if (ps != null) ps.Stop();
	}
}
