using System.Collections.Generic;
using Actors.Enemies;
using Assets.Scripts.Saves___Serialization.Progression;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LogsDisplayEnemy : MonoBehaviour
{
	public TextMeshProUGUI t_enemyName;

	public TextMeshProUGUI t_enemyDescription;

	public TextMeshProUGUI t_enemyStats;

	public TextMeshProUGUI t_enemyMaps;

	public TextMeshProUGUI t_killsCounter;

	public TextMeshProUGUI t_challengeCounter;

	public TextMeshProUGUI t_reward;

	public RawImage enemyRenderer;

	public RawImage barProgress;

	public RawImage i_rewardCoin;

	public List<RawImage> challengeProgress;

	public Color dotColorIncomplete;

	public Color dotColorComplete;

	public Material rainbow;

	public Texture unknownTexture;

	public Texture renderTexture;

	private EEnemy eEnemy;

	private void Awake()
	{
		MyButtonLog.A_EnemySelected += SetEnemy;
		MyButtonLog.A_ClaimedReward += OnClaimedReward;
	}

	private void OnDestroy()
	{
		MyButtonLog.A_EnemySelected -= SetEnemy;
		MyButtonLog.A_ClaimedReward -= OnClaimedReward;
	}

	private void OnClaimedReward()
	{
		SetEnemy(eEnemy);
	}

	public void SetEnemy(EEnemy eEnemy)
	{
		this.eEnemy = eEnemy;
		if (!LogUtility.IsEntryUnlocked(eEnemy))
		{
			if (t_enemyName != null) t_enemyName.text = "------";
			if (t_enemyDescription != null) t_enemyDescription.text = "------";
			if (t_enemyStats != null) t_enemyStats.text = "------";
			if (t_enemyMaps != null) t_enemyMaps.text = "------";
			if (enemyRenderer != null) enemyRenderer.texture = unknownTexture;
			if (barProgress != null) barProgress.transform.localScale = new Vector3(0f, 1f, 1f);
			if (t_challengeCounter != null) t_challengeCounter.text = "";
			if (t_killsCounter != null) t_killsCounter.text = "Kills: 0";
			if (t_reward != null) t_reward.text = "";
			if (i_rewardCoin != null) i_rewardCoin.gameObject.SetActive(false);
			if (challengeProgress != null)
				foreach (var dot in challengeProgress) if (dot != null) dot.color = dotColorIncomplete;
			return;
		}
		bool complete = LogUtility.HasClaimedAllRewards(eEnemy);
		var data = DataManager.Instance != null ? DataManager.Instance.GetEnemyData(eEnemy) : null;
		if (data == null)
		{
			if (t_enemyName != null) t_enemyName.text = "ERROR: Enemy data not found for: " + eEnemy;
			return;
		}
		if (t_enemyName != null) t_enemyName.text = data.GetName();
		if (enemyRenderer != null) enemyRenderer.texture = renderTexture;
		if (t_reward != null) t_reward.text = complete ? "" : LogUtility.GetReward(eEnemy).ToString();
		if (i_rewardCoin != null) i_rewardCoin.gameObject.SetActive(!complete);
		LogUtility.GetChallengeProgress(eEnemy, out float progress, out int claimed, out int kills, out int threshold);
		if (challengeProgress != null)
			for (int i = 0; i < challengeProgress.Count; i++)
				if (challengeProgress[i] != null) challengeProgress[i].color = i < claimed ? dotColorComplete : dotColorIncomplete;
		if (barProgress != null)
		{
			barProgress.transform.localScale = new Vector3(progress, 1f, 1f);
			barProgress.material = !complete && progress >= 1f ? rainbow : null;
		}
		if (t_challengeCounter != null) t_challengeCounter.text = $"{Mathf.Min(kills, threshold)}/{threshold}";
		if (t_killsCounter != null) t_killsCounter.text = $"Kills: {kills}";
	}
}
