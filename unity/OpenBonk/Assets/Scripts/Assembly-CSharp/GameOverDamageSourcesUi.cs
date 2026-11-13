using UnityEngine;
using System.Linq;
using Assets.Scripts.Saves___Serialization.Progression.Stats;

public class GameOverDamageSourcesUi : MonoBehaviour
{
	public GameObject damageSourcePrefab;

	public Transform damageSourceParent;

	private void Start()
	{
		if (damageSourcePrefab == null || damageSourceParent == null || RunStats.damageSources == null) return;
		foreach (DamageSource source in RunStats.damageSources.Values.OrderByDescending(source => source.damage))
		{
			if (source == null || string.IsNullOrEmpty(source.damageSource) || source.damageSource == "Unknown" || source.damageSource == "Unkown") continue;
			GameObject row = Instantiate(damageSourcePrefab, damageSourceParent);
			row.SetActive(true);
			row.transform.SetAsLastSibling();
			DamageSourceEntry entry = row.GetComponent<DamageSourceEntry>();
			if (entry != null) entry.Set(source);
		}
	}
}
