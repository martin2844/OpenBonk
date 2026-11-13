using System.Collections.Generic;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Managers;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Objects.Pooling;
using Assets.Scripts.Utility;
using UnityEngine;

public class XpDamager : MonoBehaviour
{
	public Pickup pickup;

	private bool isEnabled;

	private float damage;

	private float radius = .4f;

	private Vector3 dir;

	private static readonly RaycastHit[] _hits = new RaycastHit[EnemyManager.maxNumEnemiesPooled];

	private Vector3 lastPos;

	private Dictionary<Collider, float> enemyHitCooldowns = new Dictionary<Collider, float>(256);

	private float hitCooldown = .2f;

	private static string damageSource = EItem.ShatteredWisdom.ToString();

	private static DamageContainer reuseDc = new DamageContainer(1f, damageSource);

	private void OnEnable()
	{
		var player = MyPlayer.Instance;
		isEnabled = player != null && player.inventory != null && player.inventory.itemInventory != null && player.inventory.itemInventory.items.ContainsKey(EItem.ShatteredWisdom);
		if (pickup != null) OnValueUpdated(pickup.GetValue());
	}

	private void Awake()
	{
		if (pickup != null) pickup.A_ValueUpdated += OnValueUpdated;
	}

	private void OnDestroy()
	{
		if (pickup != null) pickup.A_ValueUpdated -= OnValueUpdated;
	}

	private void OnValueUpdated(int value)
	{
		damage = pickup.GetValue() * ItemShatteredWisdom.GetDamage();
	}

	private void FixedUpdate()
	{
		if (isEnabled) StepMovement();
	}

	protected virtual void StepMovement()
	{
		dir = transform.position - lastPos;
		if (GameManager.Instance != null)
		{
			int count = Physics.SphereCastNonAlloc(transform.position, radius, dir.normalized, _hits, dir.magnitude, GameManager.Instance.whatIsEnemy);
			for (int i = 0; i < count; i++) HitEnemy(_hits[i].collider);
		}
		lastPos = transform.position;
	}

	private void HitEnemy(Collider collider)
	{
		if (enemyHitCooldowns.TryGetValue(collider, out float lastHit) && MyTime.time - lastHit < hitCooldown) return;
		if (EnemyManager.Instance == null || !EnemyManager.Instance.GetEnemy(collider, out var enemy) || enemy == null) return;
		enemyHitCooldowns[collider] = MyTime.time;
		reuseDc = WeaponUtility.GetDamageContainer(reuseDc, damage, ItemShatteredWisdom.procCoefficient, damageSource, dir, enemy);
		enemy.DamageFromPlayerOther(reuseDc);
		if (EffectManager.Instance != null && PoolManager.Instance != null)
			EffectManager.Instance.EnemyHitEffect(transform.position, dir, true, (EWeapon)(-1), null, true);
	}
}
