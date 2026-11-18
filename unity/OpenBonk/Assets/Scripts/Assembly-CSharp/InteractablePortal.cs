using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Managers;
using Assets.Scripts.Saves___Serialization.Progression.Stats;
using Assets.Scripts.UI.Localization;
using Assets.Scripts.Utility;
using UnityEngine;

public class InteractablePortal : BaseInteractable
{
	[CompilerGenerated]
	private sealed class _003CDoLoadNextStage_003Ed__6 : IEnumerator<object>, IEnumerator, IDisposable
	{
		private int _003C_003E1__state;

		private object _003C_003E2__current;

		public InteractablePortal _003C_003E4__this;

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
		public _003CDoLoadNextStage_003Ed__6(int _003C_003E1__state)
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

	private bool restarted;
	[NonSerialized] internal bool completesExternalRun;

	private void Awake()
	{
		MapController.A_NewRunStarted += OnNewRunStarted;
	}

	private new void OnDestroy()
	{
		MapController.A_NewRunStarted -= OnNewRunStarted;
		base.OnDestroy();
	}

	private void OnNewRunStarted() { restarted = true; }

	public override bool Interact()
	{
		if (done) return false;
		done = true;
		StartCoroutine(DoLoadNextStage());
		gameObject.tag = "Untagged";
		return true;
	}

	private IEnumerator DoLoadNextStage()
	{
		GameManager.Instance.cutscene = true;
		MyPlayer.Instance.TeleportPlayerNextStage();
		yield return new WaitForSeconds(2f);
		if (restarted || MyPlayer.Instance.IsDead()) yield break;
		if (!completesExternalRun)
		{
			MapController.LoadNextStage();
			yield break;
		}
		GameManager.Instance.OnTeleportAway();
		var save = SaveManager.Instance;
		if (save?.progression != null)
		{
			save.progression.menuMeta.GetMapProgress(MapController.currentMap.eMap).SetKills(RunStats.GetStat(EMyStat.kills));
			save.SaveProgression();
			save.SaveStats();
		}
		MyTime.Pause();
		UiManager.Instance.hud.SetActive(false);
		UiManager.Instance.deathScreen.ShowStats();
	}

	public override string GetInteractString() =>
		LocalizationUtility.GetLocalizedString("Game_Interactables", completesExternalRun ? "FINAL_PORTAL" : "ENTER_PORTAL");
}
