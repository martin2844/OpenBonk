using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UI;
using Assets.Scripts.UI.Animation;
using Assets.Scripts.Managers;
using Assets.Scripts.Saves___Serialization.Progression.Challenges;

public class DeathScreen : MonoBehaviour
{
	[CompilerGenerated]
	private sealed class _003CDoTransition_003Ed__29 : IEnumerator<object>, IEnumerator, IDisposable
	{
		private int _003C_003E1__state;

		private object _003C_003E2__current;

		public DeathScreen _003C_003E4__this;

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
		public _003CDoTransition_003Ed__29(int _003C_003E1__state)
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

	public DeathScreenBlocksUI blocksUi;

	public GameObject deadUiWindow;

	public GameObject statsWindow;

	public GameObject background;

	public GameObject leaderboardsWindow;

	public GameObject victoryScreen;

	public UiAnimation t_dead;

	public UiAnimation b_continue;

	public AudioSource audio;

	public RawImage playerOnlyRender;

	public CanvasGroup canvasGroup;

	private float fadeInTime = 3f;

	private float fadeTimer = -0.2f;

	private bool hasNewRecord;

	private bool tierVictory;

	public GameObject restartBtn;

	public int newRecordRank { get; private set; }

	public string newRecordLbName { get; private set; }

	private void Awake()
	{
		newRecordRank = int.MaxValue;
		InteractableBossSpawner.A_BossDefeated += OnBossDefeated;
	}

	private void OnDestroy()
	{
		InteractableBossSpawner.A_BossDefeated -= OnBossDefeated;
	}

	private void OnLeaderboardScoreUploaded(string lbName, int rank)
	{
	}

	private void OnBossDefeated(bool canSpawnPortal)
	{
		if (MapController.IsTierFinalStage()) tierVictory = true;
	}

	public void PlayAudio()
	{
		if (audio != null) audio.Play();
	}

	public void StartDeathScreen()
	{
		StartCoroutine(DoTransition());
	}

	private IEnumerator DoTransition()
	{
		if (blocksUi != null) blocksUi.StartTransition(2f);
		if (playerOnlyRender != null)
		{
			playerOnlyRender.gameObject.SetActive(true);
			if (PlayerCamera.Instance != null) playerOnlyRender.texture = PlayerCamera.Instance.deathRenderTexture;
		}
		yield return new WaitForSeconds(2f);
		if (blocksUi != null) blocksUi.gameObject.SetActive(false);
		if (deadUiWindow != null) deadUiWindow.SetActive(true);
		if (background != null) background.SetActive(true);
		if (canvasGroup != null) { canvasGroup.gameObject.SetActive(true); canvasGroup.transform.localScale = Vector3.zero; }
		yield return new WaitForSeconds(1.2f);
		if (b_continue != null)
		{
			b_continue.gameObject.SetActive(true);
			b_continue.ScaleIn(2f, EEasing.OutCirc);
			ButtonManager.ForceHoverButton(b_continue.GetComponent<MyButton>());
		}
	}

	private void Update()
	{
		if (canvasGroup == null || !canvasGroup.gameObject.activeInHierarchy) return;
		fadeTimer += Time.deltaTime / fadeInTime;
		float progress = Mathf.Clamp01(fadeTimer);
		canvasGroup.alpha = progress;
		float eased = Mathf.Sqrt(1f - (progress - 1f) * (progress - 1f));
		canvasGroup.transform.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, eased);
	}

	public void ShowLeaderboard()
	{
		leaderboardsWindow.SetActive(true);
	}

	public void HideVictoryScreen()
	{
		victoryScreen.SetActive(false);
		ShowStats();
	}

	public void ShowStats()
	{
		if (tierVictory) { victoryScreen.SetActive(true); tierVictory = false; return; }
		if (hasNewRecord) { leaderboardsWindow.SetActive(true); hasNewRecord = false; return; }
		leaderboardsWindow.SetActive(false);
		PlayerCamera.Instance.HideDeathCamera();
		canvasGroup.gameObject.SetActive(true);
		b_continue.gameObject.SetActive(false);
		statsWindow.SetActive(true);
		if (ChallengesTracker.HasChallenge()) restartBtn.SetActive(true);
	}

	public void GoToMenu()
	{
		TransitionUI.Instance.LoadMenu();
	}

	public void Restart()
	{
		MapController.RestartRun();
	}
}
