using TMPro;
using UnityEngine;
using Assets.Scripts.Utility;
using Assets.Scripts.Managers;
using Assets.Scripts.Saves___Serialization.Progression.Challenges;

public class GameTimer : MonoBehaviour
{
	public TextMeshProUGUI t_timerRun;

	public TextMeshProUGUI t_timerStage;

	public TextMeshProUGUI t_timerSpeedrun;

	private float fontSizeDefault;

	private bool isRed;

	private ChallengeModifierSpeedrun speedrunModifier;

	private int lastRunMinutes;

	private int lastRunSeconds;

	private int lastStageMinutes;

	private int lastStageSeconds;

	private void Start()
	{
		if (t_timerStage != null) fontSizeDefault = t_timerStage.fontSize;
		lastRunMinutes = lastRunSeconds = lastStageMinutes = lastStageSeconds = -1;
		UpdateTimers();
	}

	private void FixedUpdate()
	{
		if (GameManager.Instance != null && GameManager.Instance.IsPlaying()) UpdateTimers();
	}

	private void UpdateTimers()
	{
		UpdateTimer(MyTime.runTimer, t_timerRun, true, ref lastRunMinutes, ref lastRunSeconds, false);
		var manager = EnemyManager.Instance;
		var game = GameManager.Instance;
		if (manager == null || !manager.enabledWaves || game == null || t_timerStage == null) return;
		if (game.isCrypt)
		{
			if (!game.isDungeonTimerStarted) { t_timerStage.text = string.Empty; return; }
			if (!isRed) { isRed = true; t_timerStage.color = Color.red; t_timerStage.fontSize = 50f; }
			float remaining = Mathf.Clamp(game.dungeonTimeToComplete - MyTime.cryptTimer, 0f, game.dungeonTimeToComplete);
			UpdateTimer(remaining, t_timerStage, true, ref lastStageMinutes, ref lastStageSeconds, true);
			return;
		}
		float stageTime;
		if (manager.IsFinalSwarm())
		{
			if (!isRed) { isRed = true; t_timerStage.color = Color.red; }
			stageTime = MyTime.finalSwarmTimer;
		}
		else
		{
			if (isRed) { isRed = false; t_timerStage.color = Color.white; }
			float maximum = game.GetStageTimeMax();
			stageTime = Mathf.Clamp(maximum - MyTime.stageTimer, 0f, maximum);
		}
		UpdateTimer(stageTime, t_timerStage, false, ref lastStageMinutes, ref lastStageSeconds, true);
		if (t_timerStage.fontSize > fontSizeDefault) t_timerStage.fontSize = fontSizeDefault;
	}

	private float GetForcedTime()
	{
		if (!ChallengesTracker.HasChallengeModifier(ChallengesTracker.MODIFIER_SPEEDRUN) || ChallengesTracker.challengeModifiers == null) return -1f;
		foreach (var modifier in ChallengesTracker.challengeModifiers)
			if (modifier is ChallengeModifierSpeedrun speedrun && modifier.internalName == ChallengesTracker.MODIFIER_SPEEDRUN)
				return Mathf.Max(0f, speedrun.timeLimitMinutes * 60f - MyTime.runTimer);
		return -1f;
	}

	private void UpdateTimer(float time, TextMeshProUGUI textMesh, bool useClock, ref int lastMinutes, ref int lastSeconds, bool useSpeedrunModifier)
	{
		if (textMesh == null) return;
		float forcedTime = GetForcedTime();
		if (useSpeedrunModifier && forcedTime >= 0f && forcedTime < time) time = forcedTime;
		int seconds = Mathf.FloorToInt(Mathf.Max(0f, time));
		int minutes = seconds / 60;
		seconds %= 60;
		if (minutes == lastMinutes && seconds == lastSeconds) return;
		lastMinutes = minutes;
		lastSeconds = seconds;
		textMesh.text = useClock ? $"<size=110%><sprite name=clock></size> {minutes}:{seconds:00}" : $"{minutes}:{seconds:00}";
	}
}
