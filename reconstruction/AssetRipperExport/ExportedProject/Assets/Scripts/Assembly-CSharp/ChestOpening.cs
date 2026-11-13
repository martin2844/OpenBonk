using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Linq;
using Assets.Scripts.Inventory__Items__Pickups.Interactables;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Saves___Serialization.Progression.Stats;
using UnityEngine;
using UnityEngine.UI;

public class ChestOpening : MonoBehaviour
{
	[CompilerGenerated]
	private sealed class _003CAnimateEffects_003Ed__54 : IEnumerator<object>, IEnumerator, IDisposable
	{
		private int _003C_003E1__state;

		private object _003C_003E2__current;

		public ChestOpening _003C_003E4__this;

		public ItemData itemData;

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
		public _003CAnimateEffects_003Ed__54(int _003C_003E1__state)
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
	private sealed class _003CAnimateOpening_003Ed__48 : IEnumerator<object>, IEnumerator, IDisposable
	{
		private int _003C_003E1__state;

		private object _003C_003E2__current;

		public ChestOpening _003C_003E4__this;

		public ItemData itemData;

		private float _003Ctimer_003E5__2;

		private float _003CwaitTime_003E5__3;

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
		public _003CAnimateOpening_003Ed__48(int _003C_003E1__state)
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

	public SkinnedMeshRenderer chestRenderer;

	public Animator chestAnimator;

	public AudioSource sfxOpen;

	public AudioSource sfxBuildup;

	public AudioSource sfxBuildupIntro;

	private bool spinning;

	private bool opened;

	public AudioClip buildupCommon;

	public AudioClip buildupRare;

	public AudioClip buildupEpic;

	public AudioClip buildupLegendary;

	public AudioClip skipCommon;

	public AudioClip skipRare;

	public AudioClip skipEpic;

	public AudioClip skipLegendary;

	public GameObject backgroundParticles;

	public ParticleSystem itemShine;

	public ParticleSystem backgroundGlow;

	public ParticleSystem[] psChestEmission;

	public GameObject particlesCoinsParent;

	public Mesh meshNormal;

	public Mesh meshFree;

	public Mesh meshEvil;

	public Material matNormal;

	public Material matFree;

	public Material matEvil;

	public Material matFreeCrypt;

	public Material matGhost;

	public Camera cam;

	public static Action<ItemData> A_ChestFinished;

	private ItemData itemData;

	private List<ItemData> rollingItems;

	public RawImage itemIcon;

	private int index;

	private const float updateRate = 0.06f;

	private float nextIconUpdate;

	private Vector3 desiredPosition = new Vector3(0f, 2.5f, 0f);

	public GameObject fxCommon;

	public GameObject fxRare;

	public GameObject fxEpic;

	public GameObject fxLegendary;

	public GameObject fxCorrupted;

	public GameObject fxFinal;

	public Texture[] testSpinTextures;

	private bool canSkip;

	private bool skipped;

	private bool immediateAndroidSkip;

	private int openingFrame;

	private float desiredFov = 40f;

	private float defaultFov = 40f;

	private float timeBetweenTiers = 0.32f;

	public void SetChest(EChest chestType)
	{
		gameObject.SetActive(true);
		SetRender(chestType);
		spinning = false;
		opened = false;
		itemIcon.transform.localPosition = Vector3.zero;
		itemIcon.transform.localScale = Vector3.zero;
		fxFinal.SetActive(false);
		backgroundParticles.SetActive(false);
		itemShine.gameObject.SetActive(false);
		chestAnimator.Play("Idle", 0, 0f);
	}

	private void SetRender(EChest chest)
	{
		switch (chest)
		{
			case EChest.Normal: chestRenderer.sharedMesh = meshNormal; chestRenderer.sharedMaterial = matNormal; break;
			case EChest.Corrupt: chestRenderer.sharedMesh = meshEvil; chestRenderer.sharedMaterial = matEvil; break;
			case EChest.Free: chestRenderer.sharedMesh = meshFree; chestRenderer.sharedMaterial = matFree; break;
			case EChest.FreeCrypt: chestRenderer.sharedMesh = meshFree; chestRenderer.sharedMaterial = matFreeCrypt; break;
			case EChest.Ghost: chestRenderer.sharedMesh = meshNormal; chestRenderer.sharedMaterial = matGhost; break;
		}
	}

	public void OpenChest(ItemData itemData)
	{
		InitializeSkipState(Application.platform == RuntimePlatform.Android, Time.frameCount);
		this.itemData = itemData;
		chestAnimator.Play("Open");
		StartCoroutine(AnimateOpening(itemData));
		rollingItems = new List<ItemData>(RunUnlockables.availableItems[itemData.rarity]);
		if (rollingItems.Count >= 3) rollingItems.Remove(itemData);
		rollingItems = rollingItems.OrderBy(_ => global::Utility.MyRandom.random.Next()).ToList();
	}

	private void Update()
	{
		if (CanReadSkipInput(canSkip, immediateAndroidSkip, openingFrame, Time.frameCount) &&
			(MyInputManager.GetButtonDown(MyInputManager.UISubmit) || Input.GetMouseButtonDown(0))) skipped = true;
		if (opened && itemData != null)
		{
			float speed = itemData.rarity == EItemRarity.Common ? 2f : itemData.rarity == EItemRarity.Rare ? 1.4f : 1f;
			itemIcon.transform.localPosition = Vector3.Lerp(itemIcon.transform.localPosition, desiredPosition, Time.deltaTime * speed);
			itemIcon.transform.localScale = Vector3.Lerp(itemIcon.transform.localScale, Vector3.one, Time.deltaTime * speed);
		}
		if (spinning && Time.time > nextIconUpdate && rollingItems != null && rollingItems.Count > 0)
		{
			nextIconUpdate = Time.time + updateRate;
			itemIcon.texture = rollingItems[index++ % rollingItems.Count].GetIcon();
		}
		desiredFov = Mathf.Lerp(desiredFov, defaultFov, Time.deltaTime * 14f);
		cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, desiredFov, Time.deltaTime * 15f);
	}

	private IEnumerator AnimateOpening(ItemData itemData)
	{
		yield return AnimateEffects(itemData);
		particlesCoinsParent.SetActive(true);
		foreach (ParticleSystem effect in psChestEmission) effect.enableEmission = true;
		canSkip = immediateAndroidSkip || MyStats.GetStat(EMyStat.chestsOpened.ToString()) > 10f;
		float[] rollTimes = { 1.5f, 2.26f, 3.005f, 3.005f, 3.005f };
		int rarityIndex = (int)itemData.rarity;
		float wait = rarityIndex >= 0 && rarityIndex < rollTimes.Length ? rollTimes[rarityIndex] : 1.5f;
		float timer = 0f;
		while (timer < wait && !skipped) { timer += Time.deltaTime; yield return null; }
		if (skipped)
		{
			sfxBuildup.Stop();
			sfxBuildup.clip = itemData.rarity == EItemRarity.Common ? skipCommon : itemData.rarity == EItemRarity.Rare ? skipRare : itemData.rarity == EItemRarity.Epic ? skipEpic : skipLegendary;
			sfxBuildup.Play();
		}
		canSkip = false;
		fxFinal.SetActive(true);
		spinning = false;
		itemShine.startColor = Assets.Scripts.Utility.MyColorUtility.GetItemRarityColor(itemData.rarity);
		itemShine.gameObject.SetActive(true);
		fxCommon.SetActive(false);
		itemIcon.texture = itemData.icon;
		foreach (ParticleSystem effect in psChestEmission) effect.enableEmission = false;
		yield return new WaitForSeconds(0.1f);
		itemIcon.transform.localScale = Vector3.one * 1.75f;
		fxCommon.SetActive(false); fxRare.SetActive(false); fxEpic.SetActive(false); fxLegendary.SetActive(false); fxCorrupted.SetActive(false);
		chestAnimator.Play("OpenedBounce", 0, 0f);
		backgroundGlow.enableEmission = false;
		desiredFov = 10f;
		A_ChestFinished?.Invoke(itemData);
		yield return new WaitForSeconds(1f);
	}

	private IEnumerator AnimateEffects(ItemData itemData)
	{
		sfxOpen.Play();
		yield return WaitForChestEffect(0.4f);
		if (itemData.rarity == EItemRarity.Corrupted)
		{
			fxCorrupted.SetActive(true);
			chestAnimator.Play("OpenedBounce", 0, 0f);
			desiredFov = 35f;
		}
		else
		{
			fxCommon.SetActive(true);
			chestAnimator.Play("OpenedBounce", 0, 0f);
			desiredFov = 35f;
			if (itemData.rarity != EItemRarity.Common) yield return WaitForChestEffect(timeBetweenTiers);
			if (itemData.rarity > EItemRarity.Common)
			{
				fxCommon.SetActive(false); fxRare.SetActive(true);
				chestAnimator.Play("OpenedBounce", 0, 0f); desiredFov = 35f;
				if (itemData.rarity != EItemRarity.Rare) yield return WaitForChestEffect(timeBetweenTiers);
			}
			if (itemData.rarity >= EItemRarity.Epic)
			{
				fxRare.SetActive(false); fxEpic.SetActive(true);
				chestAnimator.Play("OpenedBounce", 0, 0f); desiredFov = 35f;
				if (itemData.rarity != EItemRarity.Epic) yield return WaitForChestEffect(timeBetweenTiers);
			}
			if (itemData.rarity >= EItemRarity.Legendary)
			{
				fxEpic.SetActive(false); fxLegendary.SetActive(true);
				chestAnimator.Play("OpenedBounce", 0, 0f); desiredFov = 35f;
			}
		}
		sfxBuildup.clip = itemData.rarity == EItemRarity.Common ? buildupCommon : itemData.rarity == EItemRarity.Rare ? buildupRare : itemData.rarity == EItemRarity.Epic ? buildupEpic : buildupLegendary;
		sfxBuildup.Play();
		spinning = true; opened = true;
		backgroundParticles.SetActive(true);
		backgroundGlow.enableEmission = true;
		yield return null;
	}

	private IEnumerator WaitForChestEffect(float duration)
	{
		if (!immediateAndroidSkip)
		{
			yield return new WaitForSeconds(duration);
			yield break;
		}
		float elapsed = 0f;
		while (elapsed < duration && !skipped)
		{
			elapsed += Time.deltaTime;
			yield return null;
		}
	}

	private static bool CanReadSkipInput(bool enabled, bool android, int startFrame, int currentFrame)
	{
		return enabled && (!android || currentFrame > startFrame);
	}

	private void InitializeSkipState(bool android, int frame)
	{
		immediateAndroidSkip = android;
		openingFrame = frame;
		canSkip = android;
		skipped = false;
	}
}
