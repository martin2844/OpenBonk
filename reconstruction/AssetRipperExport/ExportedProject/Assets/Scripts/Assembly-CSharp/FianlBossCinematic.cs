using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using MilkShake;
using UnityEngine;

public class FianlBossCinematic : MonoBehaviour
{
	[CompilerGenerated]
	private sealed class _003CAnimateCinematic_003Ed__14 : IEnumerator<object>, IEnumerator, IDisposable
	{
		private int _003C_003E1__state;

		private object _003C_003E2__current;

		public FianlBossCinematic _003C_003E4__this;

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
		public _003CAnimateCinematic_003Ed__14(int _003C_003E1__state)
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

	public bool skipIntro = true;

	public MapGenerationFinalBoss mapGeneration;

	public Transform cameraCircling;

	public Camera cameraCirclingCamera;

	public FinalFightController finalFightController;

	public GameObject meteor;

	public ShakePreset impactShake;

	public Shaker shaker;

	private float cameraRotationSpeed;

	private float fovSpeed = 0.5f;

	private float desiredFov = 10f;

	private Transform target;

	public GameObject finalPortal;

	public void Start()
	{
		StartCoroutine(AnimateCinematic());
		FinalFightController.A_BossDefeated += OnStageBossDied;
	}

	private void OnDestroy()
	{
		FinalFightController.A_BossDefeated -= OnStageBossDied;
	}

	private IEnumerator AnimateCinematic()
	{
		GameManager.Instance.cutscene = true;
		UiManager.Instance.cinematicBars.InstaShow();
		PlayerCamera.Instance.gameObject.SetActive(false);
		cameraCircling.gameObject.SetActive(true);
		yield return new WaitForSeconds(1.5f);
		meteor.SetActive(true);
		yield return new WaitForSeconds(4f);
		target = mapGeneration.Spawn().transform;
		yield return new WaitForSeconds(1.5f);
		cameraCircling.parent.gameObject.SetActive(false);
		PlayerCamera.Instance.gameObject.SetActive(true);
		target = null;
		cameraCirclingCamera.fieldOfView = 85f;
		cameraCircling.localRotation = Quaternion.identity;
		finalFightController.SpawnBoss();
		yield return new WaitForSeconds(1f);
		if (MusicController.Instance != null) MusicController.Instance.PlayStageMusic();
	}

	public void Impact()
	{
		shaker.Shake(impactShake);
	}

	private void OnStageBossDied(bool idk)
	{
		Invoke(nameof(SpawnPortal), 10f);
	}

	private void SpawnPortal()
	{
		finalPortal.SetActive(true);
	}

	private void Update()
	{
		cameraCircling.localPosition = Vector3.Lerp(cameraCircling.localPosition, new Vector3(0f, 0f, -180f), Time.deltaTime * 0.25f);
		if (target != null && target.gameObject.activeInHierarchy)
		{
			cameraCirclingCamera.fieldOfView = Mathf.Lerp(cameraCirclingCamera.fieldOfView, desiredFov, Time.deltaTime * 3f);
			cameraCircling.rotation = Quaternion.Lerp(cameraCircling.rotation,
				Quaternion.LookRotation(target.position - cameraCircling.position), Time.deltaTime * 3f);
		}
	}
}
