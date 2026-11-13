using System;
using Actors.Enemies;
using Assets.Scripts.Saves___Serialization.Progression;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MyButtonLog : MyButton
{
	public TextMeshProUGUI t_enemyName;

	public EEnemy eEnemy;

	public static Action<EEnemy> A_EnemySelected;

	public static Action A_ClaimedReward;

	public GameObject claimAlert;

	public GameObject greenLoggedIcon;

	public MaskableGraphic background;

	public Color defaultColor;

	public Color hoverColor;

	public override void StartHover()
	{
		A_EnemySelected?.Invoke(eEnemy);
		if (background != null) background.color = hoverColor;
		isHovering = true;
	}

	public override void StopHover()
	{
		if (background != null) background.color = defaultColor;
		isHovering = false;
	}

	public void SetEnemy(EEnemy enemy, int enemyIndex)
	{
		eEnemy = enemy;
		string name = LogUtility.IsEntryUnlocked(enemy) ? EnumUtility.EnumToReadable(enemy.ToString()) : "------";
		if (t_enemyName != null) t_enemyName.text = enemyIndex.ToString("D3") + " " + name;
		if (greenLoggedIcon != null) greenLoggedIcon.SetActive(LogUtility.HasClaimedAllRewards(enemy));
		if (claimAlert != null) claimAlert.SetActive(LogUtility.HasUnclaimedReward(enemy));
	}

	public void Claim()
	{
		if (!LogUtility.HasUnclaimedReward(eEnemy)) return;
		var logs = SaveManager.Instance?.stats?.enemyLogs;
		if (logs == null || !logs.TryGetValue(eEnemy, out var log) || log == null) return;
		if (claimAlert != null && MenuParticles.Instance != null)
			MenuParticles.Instance.CoinEffect(claimAlert.transform.position);
		log.currentChallengeTier++;
		if (!LogUtility.HasUnclaimedReward(eEnemy) && claimAlert != null) claimAlert.SetActive(false);
		A_ClaimedReward?.Invoke();
	}

	protected override void OnClick()
	{
		Claim();
	}
}
