using System;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Utility;
using UnityEngine;

public class Water : MonoBehaviour
{
	public MeshRenderer waterUpper;

	public MeshRenderer waterUnder;

	private bool isCameraUnder;

	private bool isPlayerUnder;

	private bool isDamageZone;

	private MapData mapData;

	private StageData stageData;

	public static Action<Water> A_CameraEnterWater;

	public static Action<Water> A_CameraExitWater;

	public static Action<Water> A_PlayerEnterWater;

	public static Action<Water> A_PlayerExitWater;

	private GameObject splashFx;

	public GameObject lavaSplashFx;

	public Material lavaMaterial;

	private float nextDamageTime;

	private float damageInterval = 1f;

	private float damage = 1f;

	private float threshold = 0.15f;

	private float nextSplashTime;

	private float splashInterval = 1f;

	public Color color { get; private set; }

	public void Set(MapData mapData, StageData stageData)
	{
		this.mapData = mapData;
		this.stageData = stageData;
		if (stageData.waterMaterial == null) return;
		waterUpper.sharedMaterial = stageData.waterMaterial;
		waterUnder.sharedMaterial = stageData.waterMaterial;
		color = stageData.waterMaterial.GetColor("_MidColor");
		isDamageZone = mapData.isWaterDamaging;
		splashFx = stageData.waterSplashFx;
	}

	public void SetFloorIsLava()
	{
		splashFx = lavaSplashFx;
		isDamageZone = true;
		waterUpper.sharedMaterial = lavaMaterial;
		waterUnder.sharedMaterial = lavaMaterial;
		color = lavaMaterial.GetColor("_MidColor");
	}

	private void FixedUpdate()
	{
		if (!isDamageZone || !isPlayerUnder || MyTime.time < nextDamageTime) return;
		damage *= 1.1f;
		MyPlayer.Instance.inventory.playerHealth.DamagePlayerExternal(damage, 0f, Vector3.zero, false, "Drowning", (DcFlags)7);
		nextDamageTime = MyTime.time + damageInterval;
	}

	private void Update()
	{
		if (PlayerCamera.Instance == null || MyPlayer.Instance == null) return;
		float surface = transform.position.y;
		float cameraY = PlayerCamera.Instance.transform.position.y;
		if (isCameraUnder && cameraY > surface)
		{
			isCameraUnder = false;
			A_CameraExitWater?.Invoke(this);
		}
		else if (!isCameraUnder && cameraY < surface)
		{
			isCameraUnder = true;
			A_CameraEnterWater?.Invoke(this);
		}

		MyPlayer player = MyPlayer.Instance;
		float feetY = player.GetFeetPosition().y;
		if (isPlayerUnder && feetY > surface - threshold)
		{
			isPlayerUnder = false;
			A_PlayerExitWater?.Invoke(this);
			if (MyTime.time > nextSplashTime && player.playerMovement.GetFallSpeed() > 1f) SpawnSplash(player);
		}
		else if (!isPlayerUnder && feetY < surface - threshold)
		{
			if (MyTime.time > nextDamageTime + 4f)
			{
				float maxHealth = player.inventory.playerHealth.maxHp;
				damage = Mathf.FloorToInt(Mathf.Clamp(maxHealth * 0.005f - 1f, 0f, PlayerHealth.maxMaxHp) + 1f);
			}
			isPlayerUnder = true;
			if (MyTime.time > nextSplashTime && player.playerMovement.GetFallSpeed() < -1f) SpawnSplash(player);
			A_PlayerEnterWater?.Invoke(this);
		}
	}

	private void SpawnSplash(MyPlayer player)
	{
		if (splashFx == null) return;
		Instantiate(splashFx, player.GetFeetPosition(), Quaternion.identity);
		nextSplashTime = MyTime.time + splashInterval;
	}
}
