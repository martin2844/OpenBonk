using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Managers;
using Assets.Scripts.Saves___Serialization.Progression.Stats;
using Assets.Scripts.Steam;
using Assets.Scripts.UI.Localization;
using Assets.Scripts.Utility;
using UnityEngine;

public class InteractablePortalFinal : BaseInteractable
{
	[CompilerGenerated]
	private sealed class _003CDoFinishGame_003Ed__3 : IEnumerator<object>, IEnumerator, IDisposable
	{
		private int _003C_003E1__state;

		private object _003C_003E2__current;

		public InteractablePortalFinal _003C_003E4__this;

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
		public _003CDoFinishGame_003Ed__3(int _003C_003E1__state)
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

	private bool done;

	public GameObject cameraCircling;

	public override bool Interact()
	{
		if (done) return false;
		done = true;
		StartCoroutine(DoFinishGame());
		gameObject.tag = "Untagged";
		return true;
	}

	private IEnumerator DoFinishGame()
	{
		GameManager.Instance.cutscene = true;
		MyPlayer.Instance.TeleportPlayerNextStage();
		GameManager.Instance.OnTeleportAway();
		yield return new WaitForSeconds(2f);
		MyTime.Pause();
		TransitionUI.Instance.StartTransition(() =>
		{
			PlayerCamera.Instance.gameObject.SetActive(false);
			cameraCircling.SetActive(true);
			UiManager.Instance.hud.SetActive(false);
		}, 0.5f, 0.5f);
		int kills = RunStats.GetStat(EMyStat.kills);
		Leaderboards.UploadScore(kills);
		if (SaveManager.Instance != null && SaveManager.Instance.progression != null && MapController.currentMap != null && MapController.runConfig != null)
		{
			var progress = SaveManager.Instance.progression.menuMeta.GetMapProgress(MapController.currentMap.eMap);
			progress.SetKills(kills);
			progress.OnRunFinished(MyPlayer.Instance.character, true, MapController.runConfig.mapTierIndex);
			#if UNITY_ANDROID && !UNITY_EDITOR
			SaveManager.Instance.SaveProgression();
			SaveManager.Instance.SaveStats();
			#endif
		}
		yield return new WaitForSeconds(3f);
		UiManager.Instance.deathScreen.ShowStats();
	}

	public override string GetInteractString()
	{
		return LocalizationUtility.GetLocalizedString("Game_Interactables", "FINAL_PORTAL");
	}
}
