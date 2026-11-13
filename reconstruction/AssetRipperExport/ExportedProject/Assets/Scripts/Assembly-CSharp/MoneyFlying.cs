using UnityEngine;
using Assets.Scripts.Utility;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Objects.Pooling;
using Assets.Scripts.Inventory__Items__Pickups.GoldAndMoney;

public class MoneyFlying : MonoBehaviour
{
	public static int maxMoneyObjects = 30;

	public int value = 1;

	private bool pickedUp;

	private Transform target;

	private float speed;

	public ParticleSystem ps;

	public Color colorTier1;

	public Color colorTier2;

	public Color colorTier3;

	private float currentSize;

	private Vector3 randomDir;

	private float rndSpeed;

	private float rndMaxSpeed = 40f;

	private float rndTime;

	private Vector3 lastPosition;

	public void Set(int value, Vector3 pos)
	{
		gameObject.SetActive(true);
		transform.position = lastPosition = pos;
		this.value = value;
		pickedUp = false;
		speed = -20f;
		target = MyPlayer.Instance.transform;
		randomDir = (Random.insideUnitSphere + Vector3.up).normalized;
		rndSpeed = rndMaxSpeed;
		rndTime = 0f;
		RefreshVisuals();
	}

	public void AddValue(int value)
	{
		this.value += value;
		RefreshVisuals();
	}

	private void RefreshVisuals()
	{
		float size = value >= MoneyUtility.moneyTiers[0] ? 1f : value >= MoneyUtility.moneyTiers[1] ? 0.8f : 0.4f;
		if (ps != null && size != currentSize)
		{
			var main = ps.main;
			main.startColor = size == 1f ? colorTier3 : size == 0.8f ? colorTier2 : colorTier1;
			ps.transform.localScale = Vector3.one * size;
			currentSize = size;
		}
	}

	private void Update()
	{
		if (target == null || pickedUp) return;
		lastPosition = transform.position;
		transform.position += (target.position - transform.position).normalized * MyTime.deltaTime * speed;
		bool fast = MyTime.timeScale > 10f;
		float desiredSpeed = fast ? 150f : 50f;
		if (speed < desiredSpeed) speed = Mathf.Lerp(speed, desiredSpeed, (fast ? 4f : 2f) * MyTime.deltaTime);
		if (rndTime < 1f)
		{
			rndTime += Time.deltaTime;
			float t = Mathf.Clamp01(rndTime);
			float eased = Mathf.Sqrt(1f - (t - 1f) * (t - 1f));
			rndSpeed = Mathf.Lerp(rndMaxSpeed, 0f, eased);
			transform.position += randomDir * (rndSpeed * MyTime.deltaTime);
		}
	}

	private void FixedUpdate()
	{
		if (!pickedUp && target != null && DistancePointToSegment(target.position, lastPosition, transform.position) <= (MyTime.timeScale > 10f ? 2f : 0.25f)) Pickup();
	}

	private float DistancePointToSegment(Vector3 p, Vector3 a, Vector3 b)
	{
		Vector3 delta = b - a;
		float t = delta.sqrMagnitude > 0f ? Mathf.Clamp01(Vector3.Dot(p - a, delta) / delta.sqrMagnitude) : 0f;
		return Vector3.Distance(p, a + delta * t);
	}

	private void Pickup()
	{
		pickedUp = true;
		if (AudioManager.Instance != null) AudioManager.Instance.PlayGold();
		GameManager.Instance.GetPlayerInventory().ChangeGold(value);
		gameObject.SetActive(false);
		PoolManager.Instance.goldPool.Release(gameObject);
	}
}
