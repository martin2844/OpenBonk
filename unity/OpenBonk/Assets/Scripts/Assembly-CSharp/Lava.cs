using System;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Utility;
using UnityEngine;

public class Lava : MonoBehaviour
{
	private bool isCameraUnder;

	private bool isPlayerUnder;

	private bool isDamageZone;

	private MapData mapData;

	private StageData stageData;

	public static Action A_CameraEnterWater;

	public static Action A_CameraExitWater;

	public static Action A_PlayerEnterWater;

	public static Action A_PlayerExitWater;

	public GameObject splashFx;

	private float nextDamageTime = -99f;

	private float damageInterval = 1f;

	private float damage = 1f;

	private string damageSource = "Lava";

	private Collider collider;

	private Bounds bounds;

	private Vector3 lastPos;

	private float threshold = 0.15f;

	private float nextSplashTime;

	private float splashInterval = 1f;

	public Color color { get; private set; }

	private void Start()
	{
		collider = GetComponent<Collider>();
		lastPos = transform.position;
		Invoke(nameof(UpdateBounds), 0.25f);
	}

	private void UpdateBounds()
	{
		bounds = collider.bounds;
		lastPos = transform.position;
	}

	private void CheckBounds()
	{
		if (transform.position != lastPos) UpdateBounds();
	}

	private void FixedUpdate()
	{
		CheckBounds();
		if (!isPlayerUnder || MyTime.time < nextDamageTime) return;
		damage *= 1.15f;
		MyPlayer.Instance.inventory.playerHealth.DamagePlayerExternal(damage, 0f, Vector3.zero, false, damageSource, DcFlags.BypassAll);
		nextDamageTime = MyTime.time + damageInterval;
	}

	private void Update()
	{
		if (PlayerCamera.Instance == null || MyPlayer.Instance == null) return;
		bool cameraInside = bounds.Contains(PlayerCamera.Instance.transform.position);
		if (cameraInside && !isCameraUnder) { isCameraUnder = true; A_CameraEnterWater?.Invoke(); }
		else if (!cameraInside && isCameraUnder) { isCameraUnder = false; A_CameraExitWater?.Invoke(); }

		MyPlayer player = MyPlayer.Instance;
		bool playerInside = bounds.Contains(player.GetFeetPosition() + Vector3.up * 0.5f);
		if (playerInside && !isPlayerUnder)
		{
			if (MyTime.time > nextDamageTime + 4f)
			{
				float maxHealth = player.inventory.playerHealth.maxHp;
				damage = Mathf.FloorToInt(Mathf.Clamp(maxHealth * 0.05f, 0f, PlayerHealth.maxMaxHp) + 1f);
			}
			isPlayerUnder = true;
			if (MyTime.time > nextSplashTime && player.playerMovement.GetFallSpeed() < -1f) SpawnSplash(player);
			A_PlayerEnterWater?.Invoke();
		}
		else if (!playerInside && isPlayerUnder)
		{
			isPlayerUnder = false;
			A_PlayerExitWater?.Invoke();
			if (MyTime.time > nextSplashTime && player.playerMovement.GetFallSpeed() > 1f) SpawnSplash(player);
		}
	}

	private void SpawnSplash(MyPlayer player)
	{
		if (splashFx == null) return;
		Instantiate(splashFx, player.GetFeetPosition(), Quaternion.identity);
		nextSplashTime = MyTime.time + splashInterval;
	}
}
