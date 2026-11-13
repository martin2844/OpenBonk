using UnityEngine;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Managers;
using Assets.Scripts.Utility;

[CreateAssetMenu(menuName = "Me/Progression/Challenge/ChallengeModifiers/Speedrun", order = 1)]
public class ChallengeModifierSpeedrun : ChallengeModifier
{
	public float timeLimitMinutes;

	private bool isFinalBossKilled;

	public float timeLeftOnStage { get; private set; }

	public override void Init(ChallengeData challengeData)
	{
		GameManager.A_StageStarted += OnStageStarted;
		InteractableBossSpawner.A_BossDefeated += OnStageBossDied;
		FinalFightController.A_BossDefeated += OnFinalBossDefeated;
	}

	public override void Cleanup()
	{
		GameManager.A_StageStarted -= OnStageStarted;
		InteractableBossSpawner.A_BossDefeated -= OnStageBossDied;
		FinalFightController.A_BossDefeated -= OnFinalBossDefeated;
	}

	private void OnFinalBossDefeated(bool obj)
	{
		isFinalBossKilled = true;
	}

	private void OnStageBossDied(bool obj)
	{
		if (MapController.IsTierFinalStage()) isFinalBossKilled = true;
	}

	private void OnStageStarted()
	{
		isFinalBossKilled = false;
		timeLeftOnStage = Mathf.RoundToInt(timeLimitMinutes * 60f - MyTime.runTimer);
	}

	public override void Tick()
	{
		if (GameManager.Instance == null || !GameManager.Instance.IsPlaying() || isFinalBossKilled || MyPlayer.Instance == null || MyPlayer.Instance.inventory == null) return;
		var health = MyPlayer.Instance.inventory.playerHealth;
		if (!health.IsDead() && MyTime.runTimer >= timeLimitMinutes * 60f) health.KillPlayer();
	}
}
