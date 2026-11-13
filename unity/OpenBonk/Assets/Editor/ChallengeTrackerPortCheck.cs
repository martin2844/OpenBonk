using System;
using Assets.Scripts.Game.Other;
using Assets.Scripts.Managers;
using Assets.Scripts.Saves___Serialization.Progression.Challenges;
using Assets.Scripts._Data.Progression.Achievements.Challenges.ChallengeWinConditions;
using Assets.Scripts.Game.Spawning.New;
using UnityEngine;

public class ChallengeTrackerTestModifier : ChallengeModifier
{
	public int initialized;
	public int ticks;
	public int cleaned;
	public override void Init(ChallengeData data) { initialized++; }
	public override void Cleanup() { cleaned++; }
	public override void Tick() { ticks++; }
}

public class ChallengeTrackerTestCondition : ChallengeWinCondition
{
	public int initialized;
	public int cleaned;
	public override void Init(ChallengeData data) { initialized++; }
	public override void Cleanup() { cleaned++; }
}

public static class ChallengeTrackerPortCheck
{
	public static void Run()
	{
		RunConfig oldConfig = MapController.runConfig;
		Action oldRun = MapController.A_NewRunStarted;
		Action<ChallengeData> oldCompleted = ChallengesTracker.A_ChallengeCompleted;
		ChallengeData data = ScriptableObject.CreateInstance<ChallengeData>();
		ChallengeTrackerTestModifier source = ScriptableObject.CreateInstance<ChallengeTrackerTestModifier>();
		ChallengeTrackerTestCondition condition = ScriptableObject.CreateInstance<ChallengeTrackerTestCondition>();
		ChallengeModifier clone = null;
		ChallengeWinCondition conditionClone = null;
		ChallengeWinConditionEscapeFirstCrypt crypt = ScriptableObject.CreateInstance<ChallengeWinConditionEscapeFirstCrypt>();
		ChallengeWinConditionReachFinalSwarm swarm = ScriptableObject.CreateInstance<ChallengeWinConditionReachFinalSwarm>();
		try
		{
			MapController.A_NewRunStarted = null;
			source.internalName = "crypt";
			source.statModifiers = Array.Empty<Assets.Scripts.Inventory__Items__Pickups.Stats.StatModifier>();
			data.internalName = "test_challenge";
			data.challengeModifiers = new ChallengeModifier[] { source };
			data.winCondition = condition;
			MapController.runConfig = new RunConfig { challenge = data };
			ChallengesTracker.Init();
			MapController.A_NewRunStarted();
			clone = ChallengesTracker.challengeModifiers[0];
			conditionClone = (ChallengeWinCondition)typeof(ChallengesTracker).GetField("winCondition", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic).GetValue(null);
			Check(clone != source && ((ChallengeTrackerTestModifier)clone).initialized == 1, "cloned initialized modifier");
			Check(conditionClone != condition && ((ChallengeTrackerTestCondition)conditionClone).initialized == 1, "cloned initialized win condition");
			Check(ChallengesTracker.GetCurrentChallenge() == data && ChallengesTracker.HasChallenge(), "current challenge");
			Check(ChallengesTracker.HasChallengeModifier("crypt") && !ChallengesTracker.HasChallengeModifier("no_xp"), "modifier membership");
			var names = ChallengesTracker.GetChallengeModifiers();
			names.Clear();
			Check(ChallengesTracker.HasChallengeModifier("crypt"), "returned list is independent");
			ChallengesTracker.Tick();
			Check(((ChallengeTrackerTestModifier)clone).ticks == 1, "modifier tick");
			int completed = 0;
			ChallengesTracker.A_ChallengeCompleted = c => { if (c == data) completed++; };
			conditionClone.ChallengeCompleted();
			conditionClone.ChallengeCompleted();
			Check(conditionClone.completed && completed == 1, "win condition completes once");
			data.targetValue = 30;
			crypt.Init(data);
			InteractableCryptLeave.A_FirstDungeonCompleted?.Invoke(31f);
			Check(!crypt.completed, "crypt over time limit");
			InteractableCryptLeave.A_FirstDungeonCompleted?.Invoke(30f);
			Check(crypt.completed && completed == 2, "crypt time limit boundary");
			crypt.Cleanup();
			swarm.Init(data);
			SummonerController.A_FinalSwarmStarted?.Invoke();
			Check(swarm.completed && completed == 3, "final swarm win condition");
			swarm.Cleanup();
			MapController.runConfig = new RunConfig();
			MapController.A_NewRunStarted();
			Check(((ChallengeTrackerTestModifier)clone).cleaned == 1 && ((ChallengeTrackerTestCondition)conditionClone).cleaned == 1, "cleanup on next run");
			Check(!ChallengesTracker.HasChallenge() && !ChallengesTracker.HasChallengeModifier("crypt"), "clear challenge for normal run");
			Debug.Log("ChallengeTrackerPortCheck passed");
		}
		finally
		{
			ChallengesTracker.Cleanup();
			MapController.runConfig = oldConfig;
			MapController.A_NewRunStarted = oldRun;
			ChallengesTracker.A_ChallengeCompleted = oldCompleted;
			UnityEngine.Object.DestroyImmediate(data); UnityEngine.Object.DestroyImmediate(source); UnityEngine.Object.DestroyImmediate(condition);
			if (clone != null) UnityEngine.Object.DestroyImmediate(clone);
			if (conditionClone != null) UnityEngine.Object.DestroyImmediate(conditionClone);
			crypt.Cleanup(); swarm.Cleanup();
			UnityEngine.Object.DestroyImmediate(crypt); UnityEngine.Object.DestroyImmediate(swarm);
		}
	}
	private static void Check(bool value, string description) { if (!value) throw new Exception("ChallengeTrackerPortCheck failed: " + description); }
}
