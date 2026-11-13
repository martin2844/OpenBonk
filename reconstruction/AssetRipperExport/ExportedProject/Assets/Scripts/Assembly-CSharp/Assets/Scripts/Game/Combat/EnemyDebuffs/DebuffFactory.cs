using System.Collections.Generic;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using UnityEngine.Pool;
using Assets.Scripts.Game.Combat.EnemyDebuffs.Implementations;
using Assets.Scripts.Managers;

namespace Assets.Scripts.Game.Combat.EnemyDebuffs
{
	public static class DebuffFactory
	{
		private static Dictionary<EDebuff, ObjectPool<EnemyDebuff>> debuffPools = new Dictionary<EDebuff, ObjectPool<EnemyDebuff>>();

		private static int created;

		private static int returned;

		private static int getted;

		public static void Init()
		{
			GameManager.A_StageStarted += Reset;
		}

		public static void Cleanup()
		{
			GameManager.A_StageStarted -= Reset;
		}

		public static void Reset()
		{
			foreach (ObjectPool<EnemyDebuff> pool in debuffPools.Values) pool.Clear();
			debuffPools.Clear();
		}

		public static EnemyDebuff GetDebuff(EDebuff eDebuff, Enemy enemy, DamageContainer dc, float duration, int stacks)
		{
			if (!debuffPools.TryGetValue(eDebuff, out ObjectPool<EnemyDebuff> pool))
			{
				pool = CreatePool(eDebuff, EnemyManager.maxNumEnemiesPooled * 2);
				debuffPools.Add(eDebuff, pool);
			}
			EnemyDebuff debuff = pool.Get();
			debuff?.Set(enemy, dc, duration, stacks);
			return debuff;
		}

		private static ObjectPool<EnemyDebuff> CreatePool(EDebuff eDebuff, int maxObjects)
		{
			ObjectPool<EnemyDebuff> pool = null;
			pool = new ObjectPool<EnemyDebuff>(() => CreatePooledItem(eDebuff, pool, maxObjects),
				OnTakeFromPool, OnReturnedToPool, OnDestroyPoolObject, true, maxObjects, maxObjects);
			return pool;
		}

		private static EnemyDebuff CreatePooledItem(EDebuff eDebuff, ObjectPool<EnemyDebuff> pool, int maxSize)
		{
			return pool.CountAll < maxSize ? CreateDebuff(eDebuff) : null;
		}

		public static void ReturnDebuff(EDebuff eDebuff, EnemyDebuff enemyDebuff)
		{
			debuffPools[eDebuff].Release(enemyDebuff);
		}

		private static EnemyDebuff CreateDebuff(EDebuff eDebuff)
		{
			switch (eDebuff)
			{
				case EDebuff.Poison: return new DebuffPoison();
				case EDebuff.Burn: return new DebuffFire();
				case EDebuff.Freeze: return new DebuffIce();
				case EDebuff.Stun: return new DebuffStun();
				case EDebuff.Echo: return new DebuffEcho();
				case EDebuff.Bloodmark: return new DebuffBloodmark();
				default: return null;
			}
		}

		private static void OnTakeFromPool(EnemyDebuff obj)
		{
			obj?.ResetState();
		}

		private static void OnReturnedToPool(EnemyDebuff obj)
		{
		}

		private static void OnDestroyPoolObject(EnemyDebuff obj)
		{
		}
	}
}
