using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Managers;
using Assets.Scripts.UI.Localization;
using UnityEngine;

public class InteractableBossSpawnerFinal : BaseInteractable
{
	[CompilerGenerated]
	private sealed class _003CDoLoadNextStage_003Ed__4 : IEnumerator<object>, IEnumerator, IDisposable
	{
		private int _003C_003E1__state;

		private object _003C_003E2__current;

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
		public _003CDoLoadNextStage_003Ed__4(int _003C_003E1__state)
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

	public GameObject preventObjectsSpawningHere;

	private new void Start()
	{
		preventObjectsSpawningHere.SetActive(false);
	}

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
		if (!MyPlayer.Instance.IsDead()) MapController.LoadFinalStage();
	}

	public override string GetInteractString()
	{
		return LocalizationUtility.GetLocalizedString("Game_Interactables", "FINAL_BOSS");
	}
}
