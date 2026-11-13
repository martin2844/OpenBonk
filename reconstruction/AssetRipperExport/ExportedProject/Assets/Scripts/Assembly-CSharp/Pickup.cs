using System;
using System.Collections.Generic;
using Assets.Scripts.Inventory__Items__Pickups.Pickups;
using UnityEngine;
using Assets.Scripts.Utility;
using Assets.Scripts.UI.Localization;

public class Pickup : MonoBehaviour
{
	public LinkedListNode<Pickup> linkedListNode;

	public EPickup ePickup;

	private int value;

	private bool pickedUp;

	private Transform target;

	private float speed;

	public Collider collider;

	public Action<int> A_ValueUpdated;

	public static Action<Pickup> A_PickupTriggered;

	private Vector3 startPosition;

	private float readyForPickupTime;

	private bool ignoreMagnetMultiplier;

	public float floatOffset;

	public float floatSpeed;

	private float floatRandomOffset;

	public float floatRotationSpeed;

	public bool animateRotation;

	public bool animatePosition;

	private void OnEnable()
	{
		A_ValueUpdated?.Invoke(value);
		pickedUp = false;
		target = null;
		speed = 0f;
		startPosition = transform.position;
		collider.enabled = true;
	}

	public void Set(Vector3 pos, int value, float pickupDelay)
	{
		SetValue(value);
		transform.position = startPosition = pos;
		readyForPickupTime = MyTime.time + pickupDelay;
		ignoreMagnetMultiplier = ePickup > EPickup.Gold;
	}

	private void Awake()
	{
		floatRandomOffset = UnityEngine.Random.Range(0f, 100f);
	}

	public void AddValue(int addValue)
	{
		SetValue(value + addValue);
	}

	public void AddValue(Pickup other)
	{
		AddValue(other.value);
	}

	private void SetValue(int v)
	{
		value = v;
		A_ValueUpdated?.Invoke(value);
	}

	private void OnTriggerStay(Collider other)
	{
		if (!CanPickup() || readyForPickupTime > MyTime.time || !other.CompareTag("Player")) return;
		if (ignoreMagnetMultiplier && (other.transform.position - transform.position).sqrMagnitude > 16f) return;
		StartFollowingPlayer(other.transform);
	}

	public void StartFollowingPlayer(Transform target)
	{
		this.target = target;
		pickedUp = true;
		speed = -20f;
		collider.enabled = false;
	}

	private void ApplyPickup()
	{
		if (ePickup == EPickup.Xp)
		{
			AudioManager.Instance.PlayXp();
			GameManager.Instance.GetPlayerInventory().AddXp(value);
		}
		else if (ePickup == EPickup.Silver)
		{
			AudioManager.Instance.PlaySilver();
			GameManager.Instance.GetPlayerInventory().AddSilver(1);
		}
		else if (ePickup == EPickup.Magnet) PickupManager.Instance.PickupAllXp();
		ShowUi(ePickup);
		A_PickupTriggered?.Invoke(this);
		PickupManager.Instance.PickupTriggered(this);
	}

	private void ShowUi(EPickup ePickup)
	{
		if (ePickup < EPickup.Nuke || ePickup > EPickup.Magnet) return;
		string key = ePickup.ToString().ToUpper();
		string name = LocalizationUtility.GetLocalizedString("Game_ScoreUi", key + "_NAME");
		string description = LocalizationUtility.GetLocalizedString("Game_ScoreUi", key + "_DESC");
		UiManager.Instance.scoreUi.AddScore(description, name, true, true, 1f);
	}

	private void Update()
	{
		if (animateRotation) transform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y + Time.deltaTime * floatRotationSpeed, 0f);
		if (animatePosition) transform.position = startPosition + Vector3.up * (Mathf.PingPong((Time.time + floatRandomOffset) * floatSpeed, 2f * floatOffset) - floatOffset * 0.5f);
		if (!pickedUp || target == null) return;
		transform.position += (target.position - transform.position).normalized * MyTime.deltaTime * speed;
		bool fast = MyTime.timeScale > 10f;
		float desiredSpeed = fast ? 150f : 50f;
		if (speed < desiredSpeed - 1f) speed = Mathf.Lerp(speed, desiredSpeed, (fast ? 4f : 2.5f) * MyTime.deltaTime);
	}

	private void FixedUpdate()
	{
		if (pickedUp && target != null && (target.position - transform.position).sqrMagnitude < (speed > 50f ? 1f : 0.25f)) ApplyPickup();
	}

	public virtual bool CanPickup()
	{
		return !pickedUp && (ePickup != EPickup.Health || GameManager.Instance.GetPlayerInventory().playerHealth.CanHeal());
	}

	public int GetValue()
	{
		return value;
	}
}
