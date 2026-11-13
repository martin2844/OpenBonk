using UnityEngine;
using UnityEngine.Localization;
using Assets.Scripts.Saves___Serialization.Progression.Challenges;

[CreateAssetMenu(menuName = "Me/Progression/Challenge/", order = 1)]
public abstract class ChallengeWinCondition : ScriptableObject
{
	public LocalizedString localizedString;

	public bool completed { get; private set; }

	public abstract void Init(ChallengeData challengeData);

	public abstract void Cleanup();

	public void ChallengeCompleted()
	{
		if (completed) return;
		completed = true;
		ChallengesTracker.CompleteChallenge();
	}

	public string GetDescription()
	{
		return localizedString != null ? localizedString.GetLocalizedString() : string.Empty;
	}
}
