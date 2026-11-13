using Assets.Scripts.Saves___Serialization.Progression.Achievements;
using UnityEngine;

public class RequirementsContainer : MonoBehaviour
{
	public RequirementPrefab[] reqContainers;

	public void Set(UnlockableBase unlockable)
	{
		if (reqContainers == null) return;
		foreach (var requirement in reqContainers) if (requirement != null) requirement.gameObject.SetActive(false);
		if (unlockable == null) return;
		int index = 0;
		var achievement = unlockable.GetUnlockRequirement();
		if (achievement != null && index < reqContainers.Length && reqContainers[index] != null)
		{
			reqContainers[index].gameObject.SetActive(true);
			reqContainers[index++].Set(achievement);
		}
		var requiredUnlockable = unlockable.GetUnlockableRequirement();
		if (requiredUnlockable != null && index < reqContainers.Length && reqContainers[index] != null)
		{
			reqContainers[index].gameObject.SetActive(true);
			reqContainers[index].Set(requiredUnlockable);
		}
	}

	public void HideBar()
	{
		if (reqContainers == null) return;
		foreach (var requirement in reqContainers) if (requirement != null) requirement.HideBar();
	}
}
