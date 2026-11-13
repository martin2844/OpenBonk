using Assets.Scripts.UI.InGame.Rewards;
using Assets.Scripts.Managers;
using Assets.Scripts.Actors.Enemies;
using Actors.Enemies;
using UnityEngine;
using UnityEngine.Localization;

public class InteractableGravestone : BaseInteractable
{
	public LocalizedString localizationName;

	private bool done;

	public GameObject fx;

	public EffectStat[] statEffects;

	private static int numInteractions;

	public static string debugName = "Gravestones";

	private void Awake()
	{
		numInteractions = 0;
	}

	public override bool Interact()
	{
		if (done) return false;
		done = true;
		gameObject.SetActive(false);
		if (fx != null) { fx.SetActive(true); fx.transform.SetParent(null); }
		if (statEffects != null)
		{
			foreach (var effect in statEffects)
			{
				if (effect == null) continue;
				effect.ApplyEffect();
				string name = effect.GetEffectName(), number = effect.GetEffectNumber();
				if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(number) && UiManager.Instance != null && UiManager.Instance.scoreUi != null)
					UiManager.Instance.scoreUi.AddScore(name, number, true, true, 1f);
			}
		}
		EEnemy ghost = GetGhostForInteraction(numInteractions);
		if (EnemyManager.Instance != null && DataManager.Instance != null)
		{
			var data = DataManager.Instance.GetEnemyData(ghost);
			if (data != null) EnemyManager.Instance.SpawnEnemy(data, transform.position, 0, false, EEnemyFlag.None, true, 1f);
		}
		numInteractions++;
		return true;
	}

	private static EEnemy GetGhostForInteraction(int index)
	{
		return index == 0 ? EEnemy.Ghost : index == 1 ? EEnemy.GreaterGhost :
			index == 2 ? EEnemy.GhostPurple : EEnemy.GhostRed;
	}

	public override bool CanInteract()
	{
		return !done;
	}

	public override string GetInteractString()
	{
		return localizationName != null ? localizationName.GetLocalizedString() : "";
	}

	public override bool ShowInDebug()
	{
		return true;
	}

	public override string GetDebugName()
	{
		return debugName;
	}
}
