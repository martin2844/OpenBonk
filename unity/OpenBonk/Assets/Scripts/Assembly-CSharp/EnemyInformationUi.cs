using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Assets.Scripts.Game.Combat;
using Assets.Scripts.Inventory.Stats;
using Assets.Scripts.Managers;
using Assets.Scripts.Utility;

public class EnemyInformationUi : MonoBehaviour
{
	public GameObject parent;

	public TextMeshProUGUI t_enemies;

	public RawImage i_enemies;

	public ScalingEntry scalingEntryHp;

	public ScalingEntry scalingEntryDmg;

	public ScalingEntry scalingEntrySpeed;

	private float updateInterval = .5f;

	private float nextUpdateTime;

	private void Awake()
	{
		CurrentSettings.A_SettingUpdated += OnSettingUpdate;
	}

	private void OnDestroy()
	{
		CurrentSettings.A_SettingUpdated -= OnSettingUpdate;
	}

	private void Start()
	{
		Refresh();
	}

	private void OnSettingUpdate(string name, object oldVal, object newVal)
	{
		if (name == "debug_enemy_scaling") Refresh();
	}

	private void Refresh()
	{
		if (parent != null && SaveManager.Instance?.config?.cfGameSettings != null)
			parent.SetActive(SaveManager.Instance.config.cfGameSettings.debug_enemy_scaling == 1);
	}

	private void FixedUpdate()
	{
		if (GameManager.Instance == null || !GameManager.Instance.IsPlaying() || !PlayerStats.HasStats() ||
			parent == null || !parent.activeInHierarchy || MyTime.time < nextUpdateTime) return;
		var manager = EnemyManager.Instance;
		var summoner = manager != null && manager.summonerController != null ? manager.summonerController.stageSummoner : null;
		if (summoner == null) return;
		nextUpdateTime = MyTime.time + updateInterval;
		int count = manager.numEnemies;
		int target = summoner.GetNumTargetEnemies();
		if (t_enemies != null) t_enemies.text = string.Format("{0} / {1}", count, target);
		if (i_enemies != null) i_enemies.transform.localScale = new Vector3(Mathf.Clamp01((float)count / target), 1f, 0f);
		float total = CombatScaling.GetHpMultiplierAddition(out float time, out float swarm, out float stage);
		if (scalingEntryHp != null) scalingEntryHp.Set(string.Format("{0:N1}x", total), time, stage, swarm);
		total = CombatScaling.GetDamageMultiplierAddition(out time, out swarm, out stage);
		if (scalingEntryDmg != null) scalingEntryDmg.Set(string.Format("{0:N1}x", total), time, stage, swarm);
		total = CombatScaling.GetSpeedMultiplierAddition(out time, out swarm, out stage);
		if (scalingEntrySpeed != null) scalingEntrySpeed.Set(string.Format("{0:N1}x", total), time, stage, swarm);
	}
}
