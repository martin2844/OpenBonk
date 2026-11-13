using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Objects.Pooling;
using Assets.Scripts.Utility;
using UnityEngine;

namespace Assets.Scripts.Game.Combat.EnemySpecialAttacks.Implementations
{
	public abstract class EnemySpecialAttackPrefab : MonoBehaviour
	{
		[CompilerGenerated]
		private sealed class _003CWaitForSecondsCustom_003Ed__18 : IEnumerator<object>, IEnumerator, IDisposable
		{
			private int _003C_003E1__state;

			private object _003C_003E2__current;

			public float time;

			private float _003Ctimer_003E5__2;

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
			public _003CWaitForSecondsCustom_003Ed__18(int _003C_003E1__state)
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

		public GameObject attackEffectPrefab;

		public EEnemyAttack eAttack;

		protected Enemy enemy;

		protected CircleWarning circleWarning;

		private bool isActive;

		public EnemySpecialAttack specialAttack { get; private set; }

		public void Set(EnemySpecialAttack attack, Enemy enemy)
		{
			isActive = true;
			specialAttack = attack;
			this.enemy = enemy;
			Init();
		}

		protected abstract void Init();

		protected void CreateWarningSphere(Vector3 pos, Action completeAction)
		{
			circleWarning = EffectManager.Instance.WarningSphere(pos, specialAttack.attackRadius, specialAttack.attackChargeTime, completeAction);
		}

		protected bool CreateWarningHitscan(Vector3 pos, Vector3 dir, float distance, Action completeAction)
		{
			return EffectManager.Instance.WarningTube(pos, dir, specialAttack.attackRadius, distance, specialAttack.attackChargeTime, completeAction);
		}

		protected GameObject GetEffectPrefab()
		{
			return PoolManager.Instance.GetEnemyAttackFx(this);
		}

		private void Awake()
		{
			Enemy.A_EnemyReleasedFromPool += OnEnemyDied;
		}

		private void OnDestroy()
		{
			Enemy.A_EnemyReleasedFromPool -= OnEnemyDied;
		}

		protected virtual void OnEnemyDied(Enemy enemy)
		{
			if (enemy != this.enemy || !isActive) return;
			ReturnToPool();
		}

		protected void ReturnToPool()
		{
			if (!gameObject.activeSelf || !isActive) return;
			isActive = false;
			CancelInvoke(nameof(ReturnToPool));
			PoolManager.Instance.ReturnEnemyAttack(this);
		}

		protected IEnumerator WaitForSecondsCustom(float time)
		{
			float timer = 0f;
			while (timer < time) { timer += MyTime.deltaTime; yield return null; }
		}

		protected DcFlags GetDamageFlags()
		{
			DcFlags flags = DcFlags.BypassEvade | DcFlags.BossDamage;
			if (enemy.IsFinalBoss() || enemy.enemyData.enemyName == global::Actors.Enemies.EEnemy.GhostKing) flags |= DcFlags.FinalBossDamage;
			return flags;
		}
	}
}
