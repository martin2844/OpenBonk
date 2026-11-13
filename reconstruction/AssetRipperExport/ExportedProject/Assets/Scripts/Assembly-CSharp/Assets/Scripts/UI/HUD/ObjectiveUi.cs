using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using UnityEngine;
using UnityEngine.Localization;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Managers;
using Assets.Scripts._Data.MapsAndStages;

namespace Assets.Scripts.UI.HUD
{
	public class ObjectiveUi : MonoBehaviour
	{
		[CompilerGenerated]
		private sealed class _003CAnimateObjective_003Ed__20 : IEnumerator<object>, IEnumerator, IDisposable
		{
			private int _003C_003E1__state;

			private object _003C_003E2__current;

			public ObjectiveUi _003C_003E4__this;

			public LocalizedString objective;

			public bool canComplete;

			public EObjective eObjective;

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
			public _003CAnimateObjective_003Ed__20(int _003C_003E1__state)
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

		public GameObject objectivePrefab;

		private ObjectivePrefabUi currentObjective;

		public AudioSource a_new;

		public AudioSource a_complete;

		public LocalizedString findBoss;

		public LocalizedString defeatBoss;

		public LocalizedString defeatBossFinal;

		public LocalizedString survive;

		public LocalizedString enterPortal;

		public LocalizedString graveyardCryptEscape;

		public LocalizedString graveyardCryptKeys;

		public LocalizedString graveyardFindCrypt;

		private void Awake()
		{
			InteractableBossSpawner.A_BossSpawned += OnBossSpawned;
			InteractableBossSpawner.A_BossDefeated += OnBossDefeated;
			EnemyManager.A_StageBossDied += OnStageBossDied;
			ItemInventory.A_ItemAdded += OnItemAdded;
		}

		private void OnDestroy()
		{
			InteractableBossSpawner.A_BossSpawned -= OnBossSpawned;
			InteractableBossSpawner.A_BossDefeated -= OnBossDefeated;
			EnemyManager.A_StageBossDied -= OnStageBossDied;
			ItemInventory.A_ItemAdded -= OnItemAdded;
		}

		private void Start()
		{
			if (MapController.IsFinalBossStage()) Invoke(nameof(FinalBossObjective), 0.5f);
			else Invoke(nameof(FirstObjective), 4f);
		}

		private void FirstObjective()
		{
			AddObjective(MapController.currentMap.eMap == EMap.Graveyard ? graveyardCryptEscape : findBoss, true);
		}

		public void GraveyardKeys()
		{
			AddObjective(graveyardCryptKeys, true, EObjective.CryptKeys);
		}

		private void FinalBossObjective()
		{
			AddObjective(defeatBossFinal, true);
		}

		public void AddObjective(LocalizedString localizedString, bool canComplete, EObjective eObjective = EObjective.Generic)
		{
			CancelInvoke(nameof(FirstObjective));
			if (currentObjective != null) { currentObjective.Complete(); a_complete.Play(); }
			StartCoroutine(AnimateObjective(localizedString, canComplete, eObjective));
		}

		public void CompleteCurrentObjective()
		{
			if (currentObjective == null) return;
			currentObjective.Complete();
			a_complete.Play();
			currentObjective = null;
		}

		private IEnumerator AnimateObjective(LocalizedString objective, bool canComplete, EObjective eObjective)
		{
			yield return new WaitForSeconds(1f);
			currentObjective = Instantiate(objectivePrefab, objectivePrefab.transform.parent).GetComponent<ObjectivePrefabUi>();
			currentObjective.Set(objective, canComplete, eObjective);
			a_new.Play();
		}

		public void OnBossSpawned()
		{
			AddObjective(defeatBoss, true);
		}

		private void OnBossDefeated(bool isOpeningPortal)
		{
			AddObjective(isOpeningPortal ? enterPortal : survive, isOpeningPortal);
		}

		private void OnStageBossDied()
		{
			if (MapController.IsFinalBossStage()) CompleteCurrentObjective();
		}

		private void OnItemAdded(EItem eItem)
		{
			if (eItem == EItem.CryptKey && MapController.currentMap.eMap == EMap.Graveyard && MyPlayer.Instance.inventory.itemInventory.GetAmount(EItem.CryptKey) == 4)
				AddObjective(graveyardFindCrypt, true);
		}
	}
}
