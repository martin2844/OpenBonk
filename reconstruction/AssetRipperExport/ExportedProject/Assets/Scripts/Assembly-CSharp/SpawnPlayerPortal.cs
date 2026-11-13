using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Managers;
using Assets.Scripts.Utility;
using UnityEngine;
using UnityEngine.UIElements.Experimental;

public class SpawnPlayerPortal : MonoBehaviour
{
	[CompilerGenerated]
	private sealed class _003CDoPortal_003Ed__12 : IEnumerator<object>, IEnumerator, IDisposable
	{
		private int _003C_003E1__state;

		private object _003C_003E2__current;

		public SpawnPlayerPortal _003C_003E4__this;

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
		public _003CDoPortal_003Ed__12(int _003C_003E1__state)
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

	public AudioSource audioOpen;

	public AudioSource audioLoop;

	public AudioSource audioClose;

	public AudioSource audioPassing;

	public GameObject blockObjectSpawns;

	public Transform portalRender;

	private Vector3 portalScale;

	public ParticleSystem passingFx;

	public static Action A_PortalOpen;

	public static Action A_PortalClosed;

	private bool movePlayer;

	private float moveTime = 1f;

	private float moveTimer;

	private Vector3 playerStartPosition;

	private Vector3 desiredPosition;

	private float openTime = 0.5f;

	private float scaleTimer;

	private bool open;

	public void StartPortal()
	{
		if (CanSkipPortalAnimation() && SaveManager.Instance.config.cfGameSettings.skip_portal_animation == 1)
		{
			MyPlayer.Instance.Spawn(transform.position, transform.forward);
			Vector3 direction = -transform.forward;
			MyPlayer.Instance.playerInput.SetSpawnDirection(new Vector3(direction.x, 0f, direction.z));
			PlayerCamera.Instance.transform.position = transform.position - transform.forward * 5f + Vector3.up * 5f;
			PlayerCamera.Instance.transform.LookAt(transform);
			GameManager.Instance.StartPlaying();
			if (UiManager.Instance != null) UiManager.Instance.mapTile.StartAnimation();
			PlayerCamera.Instance.StopPortalCamera();
			blockObjectSpawns.transform.SetParent(null);
			blockObjectSpawns.AddComponent<DestroyObject>().time = 1f;
			A_PortalClosed?.Invoke();
			Destroy(gameObject);
		}
		else StartCoroutine(DoPortal());
	}

	private bool CanSkipPortalAnimation()
	{
		return MapController.IsFirstStage();
	}

	private IEnumerator DoPortal()
	{
		A_PortalOpen?.Invoke();
		MyPlayer.Instance.Spawn(transform.position - transform.forward * 3f, transform.forward);
		playerStartPosition = MyPlayer.Instance.transform.position;
		desiredPosition = playerStartPosition + transform.forward * 6f;
		PlayerCamera.Instance.StartPortalCamera(portalRender);
		MyPlayer.Instance.gameObject.SetActive(false);
		GameManager.Instance.cutscene = true;
		if (UiManager.Instance != null) UiManager.Instance.cinematicBars.InstaShow();
		portalScale = portalRender.localScale;
		open = true;
		audioLoop.Play();
		yield return new WaitForSeconds(0.5f);
		yield return new WaitForSeconds(1f);
		yield return new WaitForSeconds(openTime);
		MyPlayer.Instance.gameObject.SetActive(true);
		movePlayer = true;
		MyPlayer.Instance.playerRenderer.ForceMoving(true);
		MyPlayer.Instance.PauseInventory(true);
		float passingDelay = moveTime * 0.2f;
		audioPassing.PlayDelayed(passingDelay);
		Invoke(nameof(PassShake), passingDelay + 0.1f);
		passingFx.gameObject.SetActive(true);
		passingFx.Play();
		yield return new WaitForSeconds(moveTime);
		movePlayer = false;
		MyPlayer.Instance.playerRenderer.ForceMoving(false);
		PlayerCamera.Instance.isPortalCameraFocusingPlayer = true;
		open = false;
		audioClose.Play();
		yield return new WaitForSeconds(openTime * 2f);
		audioLoop.Stop();
		A_PortalClosed?.Invoke();
		PlayerCamera.Instance.StopPortalCamera();
		GameManager.Instance.StartPlaying();
		MyPlayer.Instance.PauseInventory(false);
		if (UiManager.Instance != null) UiManager.Instance.mapTile.StartAnimation();
		GameManager.Instance.cutscene = false;
		if (UiManager.Instance != null) UiManager.Instance.cinematicBars.Hide();
		yield return new WaitForSeconds(2f);
		Destroy(gameObject);
	}

	private void PassShake()
	{
		ControllerShaker.Shake(0, 0.4f, 0.25f);
	}

	private void MovePlayer()
	{
		if (!movePlayer) return;
		moveTimer = Mathf.Clamp01(moveTimer + MyTime.deltaTime / moveTime);
		MyPlayer.Instance.transform.position = Vector3.Lerp(playerStartPosition, desiredPosition, moveTimer);
	}

	private void Update()
	{
		MovePlayer();
		if (open && scaleTimer < 1f) scaleTimer = Mathf.Clamp01(scaleTimer + Time.deltaTime / openTime);
		else if (!open && scaleTimer > 0f)
		{
			scaleTimer = Mathf.Clamp01(scaleTimer - Time.deltaTime / openTime);
			portalRender.localScale = Vector3.Lerp(Vector3.zero, portalScale, Easing.InOutCirc(scaleTimer));
		}
		audioLoop.volume = Mathf.Lerp(0f, 0.5f, Easing.InOutCirc(scaleTimer));
	}

	private void ClosePortal()
	{
		audioClose.Play();
	}
}
