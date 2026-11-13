using System;
using System.Collections.Generic;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using UnityEngine;

namespace Assets.Scripts.Camera
{
	public class MinimapCamera : MonoBehaviour
	{
		public Transform playerIcon;

		public Transform arrowPrefab;

		public GameObject enemyIconPrefab;

		public bool staticOrientation = true;

		private Vector3 staticRotation = new Vector3(90f, 0f, 0f);

		public UnityEngine.Camera minimapCamera;

		private bool bossSpotted;

		private Transform bossSpawner;

		public static Action<float> A_RotationUpdated;

		private Quaternion lastRotation;

		private Color bossColor = new Color(1f, 0f, 0f, 1f);

		private Color portalColor = new Color(0f, 162f / 255f, 1f, 1f);

		private Dictionary<Transform, MinimapArrow> arrowDict = new Dictionary<Transform, MinimapArrow>();

		private Dictionary<Enemy, GameObject> enemyIconDictionary = new Dictionary<Enemy, GameObject>();

		private void Start()
		{
			TryFindBossSpawner();
			Invoke(nameof(TryFindBossSpawner), 2f);
			Invoke(nameof(TryFindBossSpawner), 5f);
			InteractableBossSpawner.A_BossSpawned += OnBossSpawnerInteract;
			InteractableBossSpawner.A_BossDefeated += OnBossSpawnerCompleted;
			Enemy.A_TargetOfInterestSpawn += OnEnemySpawn;
			Enemy.A_EnemyReleasedFromPool += OnEnemyDied;
		}

		private void OnDestroy()
		{
			InteractableBossSpawner.A_BossSpawned -= OnBossSpawnerInteract;
			InteractableBossSpawner.A_BossDefeated -= OnBossSpawnerCompleted;
			Enemy.A_TargetOfInterestSpawn -= OnEnemySpawn;
			Enemy.A_EnemyReleasedFromPool -= OnEnemyDied;
		}

		private void OnEnemySpawn(Enemy enemy)
		{
			AddArrow(enemy.transform, bossColor);
		}

		private void OnEnemyDied(Enemy enemy)
		{
			if (arrowDict.ContainsKey(enemy.transform)) RemoveArrow(enemy.transform);
			if (enemyIconDictionary.ContainsKey(enemy)) RemoveEnemyMinimapIcon(enemy);
		}

		private void TryFindBossSpawner()
		{
			if (bossSpawner != null) return;
			Component found = FindAnyObjectByType<InteractableBossSpawner>();
			if (found == null) found = FindAnyObjectByType<InteractableBossSpawnerFinal>();
			if (found == null) found = FindAnyObjectByType<InteractableCrypt>();
			if (found != null) bossSpawner = found.transform;
		}

		private void Update()
		{
			if (MyPlayer.Instance != null && minimapCamera != null)
			{
				MyPlayer player = MyPlayer.Instance;
				if (player.inventory != null && player.inventory.playerHealth != null && player.inventory.playerHealth.IsDead())
				{
					if (transform.parent != null) transform.parent = null;
					return;
				}
				if (player.playerInput != null && playerIcon != null)
				{
					playerIcon.rotation = Quaternion.Euler(90f, player.playerInput.cameraRotation.y, 0f);
					if (SaveManager.Instance != null && SaveManager.Instance.config != null && SaveManager.Instance.config.cfGameSettings != null && SaveManager.Instance.config.cfGameSettings.minimap_rotation != 0)
						DynamicOrientation();
					else StaticOrientation();
				}
				TrySpotBossSpawner();
				UpdateArrows();
				UpdateEnemyMinimapIcons();
				lastRotation = transform.rotation;
			}
		}

		private void StaticOrientation()
		{
			transform.rotation = Quaternion.Euler(staticRotation);
			A_RotationUpdated?.Invoke(staticRotation.y);
		}

		private void DynamicOrientation()
		{
			transform.rotation = Quaternion.Euler(90f, MyPlayer.Instance.playerInput.cameraRotation.y, 0f);
			A_RotationUpdated?.Invoke(MyPlayer.Instance.playerMovement.orientation.eulerAngles.y);
		}

		private void UpdateArrows()
		{
			foreach (MinimapArrow arrow in arrowDict.Values)
			{
				Vector3 playerPosition = MyPlayer.Instance.transform.position;
				Vector3 targetPosition = arrow.target.position;
				playerPosition.y = targetPosition.y = 0f;
				float distance = Vector3.Distance(playerPosition, targetPosition);
				float threshold = minimapCamera.orthographicSize * 0.95f;
				if (!arrow.gameObject.activeInHierarchy)
				{
					if (distance > threshold) arrow.gameObject.SetActive(true);
					continue;
				}
				if (distance < threshold)
				{
					arrow.gameObject.SetActive(false);
					return;
				}
				Vector3 direction = arrow.target.position - transform.position;
				direction.y = 0f;
				float angle = 90f - Mathf.Atan2(direction.z, direction.x) * Mathf.Rad2Deg;
				arrow.transform.rotation = Quaternion.Euler(0f, angle, 0f);
				arrow.transform.position = transform.position + direction.normalized * (minimapCamera.orthographicSize * 0.8f) + Vector3.down * 20f;
			}
		}

		private void UpdateEnemyMinimapIcons()
		{
			foreach (var pair in enemyIconDictionary)
				if (pair.Key != null && pair.Value != null) pair.Value.transform.position = pair.Key.renderer.transform.position;
		}

		private void TrySpotBossSpawner()
		{
			if (bossSpawner == null || bossSpotted) return;
			Vector3 playerPosition = MyPlayer.Instance.transform.position;
			Vector3 bossPosition = bossSpawner.position;
			playerPosition.y = bossPosition.y = 0f;
			if (Vector3.Distance(playerPosition, bossPosition) < minimapCamera.orthographicSize)
			{
				AddArrow(bossSpawner, bossColor);
				bossSpotted = true;
			}
		}

		public void AddArrow(Transform target, Color color)
		{
			MinimapArrow arrow = Instantiate(arrowPrefab).GetComponent<MinimapArrow>();
			arrow.Set(target, color);
			arrow.transform.SetParent(null);
			arrow.gameObject.SetActive(true);
			arrowDict.Add(target, arrow);
		}

		public void RemoveArrow(Transform transform)
		{
			if (!arrowDict.TryGetValue(transform, out MinimapArrow arrow)) return;
			Destroy(arrow.gameObject);
			arrowDict.Remove(transform);
		}

		public void AddEnemyMinimapIcon(Enemy enemy, Material iconMaterial, float sizeMultiplier)
		{
			GameObject icon = Instantiate(enemyIconPrefab);
			icon.GetComponent<MeshRenderer>().sharedMaterial = iconMaterial;
			icon.transform.SetParent(null);
			icon.SetActive(true);
			icon.transform.localScale *= sizeMultiplier;
			enemyIconDictionary.Add(enemy, icon);
		}

		public void RemoveEnemyMinimapIcon(Enemy enemy)
		{
			if (!enemyIconDictionary.TryGetValue(enemy, out GameObject icon)) return;
			Destroy(icon);
			enemyIconDictionary.Remove(enemy);
		}

		private void OnBossSpawnerInteract()
		{
			if (bossSpawner != null) RemoveArrow(bossSpawner);
		}

		private void OnBossSpawnerCompleted(bool openedPortal)
		{
			if (bossSpawner != null && openedPortal) AddArrow(bossSpawner, portalColor);
		}
	}
}
