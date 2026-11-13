using Assets.Scripts.Inventory__Items__Pickups.Pickups;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Game.Spawning;
using Assets.Scripts.Inventory__Items__Pickups.GoldAndMoney;
using Assets.Scripts.Objects.Pooling;
using Assets.Scripts.UI.Localization;
using Assets.Scripts.Utility;
using Inventory__Items__Pickups;
using UnityEngine;

public class InteractableTumbleWeed : BaseInteractable
{
	public const int maxTumbleWeeds = 25;

	private bool broken;

	public Rigidbody rb;

	private Vector3 desiredVelocity;

	private float speed = 5f;

	private float actualSpeed = 5f;

	private Vector3 lastPos;

	public AudioSource audio;

	private float defaultVolume;

	private float startTime;

	private float stopTime;

	private float scaleMultiplier;

	public override bool Interact()
	{
		if (broken) return false;
		broken = true;
		if ((float)global::Utility.MyRandom.random.NextDouble() > 0.75f)
			MoneyUtility.SpawnSilver(transform.position);
		else
		{
			int value = Mathf.Max(5, Mathf.CeilToInt(XpUtility.XpToNextLevelTotal(MyPlayer.Instance.inventory.playerXp.xp) * 0.1f));
			SpawnXp(EPickup.Xp, value, 0.25f);
		}
		Despawn();
		return true;
	}

	private void Despawn()
	{
		if (Vector3.Distance(MyPlayer.Instance.transform.position, transform.position) < 40f)
		{
			GameObject effect = PoolManager.Instance.tumbleweedBreakPool.Get();
			if (effect != null) effect.transform.position = transform.position;
		}
		gameObject.SetActive(false);
		PoolManager.Instance.tumbleweedPool.Release(gameObject);
	}

	private void OnEnable()
	{
		broken = false;
		Spawn();
	}

	private void SpawnXp(EPickup ePickup, int value, float pickupDelay)
	{
		for (int i = 0; value > 0; i++)
		{
			int amount = i < 10 ? 1 : value;
			Pickup pickup = PickupManager.Instance.SpawnPickup(ePickup, transform.position + Vector3.up, amount, true, pickupDelay);
			if (pickup != null) pickup.StartFollowingPlayer(GameManager.Instance.GetPlayer().transform);
			value -= amount;
		}
	}

	public override string GetInteractString()
	{
		return LocalizationUtility.GetLocalizedString("Game_Interactables", "BREAK_TUMBLEWEED");
	}

	private void Spawn()
	{
		Vector3 position = SpawnPositions.GetRandomSpawnPositionOnMap(999f) + Vector3.up;
		transform.position = position;
		rb.MovePosition(position);
		speed = Random.Range(4f, 10f);
		FindNewDir();
		startTime = MyTime.time;
		stopTime = startTime + Random.Range(45f, 60f);
	}

	private void FindNewDir()
	{
		Vector3 direction = Random.insideUnitSphere;
		direction.y = 0f;
		desiredVelocity = direction.normalized * speed;
		actualSpeed = 5f;
	}

	private void FixedUpdate()
	{
		if (MyTime.paused || MyTime.fixedDeltaTime <= 0f) return;
		rb.velocity = new Vector3(desiredVelocity.x, rb.velocity.y, desiredVelocity.z);
		Vector3 position = rb.position;
		position.y = 0f;
		actualSpeed = Mathf.Lerp(actualSpeed, Vector3.Distance(position, lastPos) / MyTime.fixedDeltaTime, 0.1f);
		if (actualSpeed < 3f) FindNewDir();
		lastPos = position;
		if (MyTime.time > stopTime) Despawn();
	}
}
