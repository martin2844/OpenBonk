using UnityEngine;
using Assets.Scripts.Utility;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Objects.Pooling;

public class SilverFlying : MonoBehaviour
{
	private bool pickedUp;

	private Transform target;

	private float speed;

	private Vector3 randomDir;

	private float rndSpeed;

	private float rndMaxSpeed = 40f;

	private float rndTime;

	public void Set(Vector3 pos)
	{
		gameObject.SetActive(true);
		transform.position = pos;
		speed = -20f;
		target = MyPlayer.Instance.transform;
		pickedUp = false;
		randomDir = (Random.insideUnitSphere + Vector3.up).normalized;
		rndSpeed = rndMaxSpeed;
		rndTime = 0f;
	}

	private void Update()
	{
		if (target == null || pickedUp) return;
		transform.position += (target.position - transform.position).normalized * MyTime.deltaTime * speed;
		if (speed < 49f) speed = Mathf.Lerp(speed, 50f, 2f * MyTime.deltaTime);
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
		if (!pickedUp && target != null && (target.position - transform.position).sqrMagnitude < 0.5f) Pickup();
	}

	private void Pickup()
	{
		pickedUp = true;
		if (AudioManager.Instance != null) AudioManager.Instance.PlaySilver();
		GameManager.Instance.GetPlayerInventory().AddSilver(1);
		PoolManager.Instance.silverPool.Release(gameObject);
	}
}
