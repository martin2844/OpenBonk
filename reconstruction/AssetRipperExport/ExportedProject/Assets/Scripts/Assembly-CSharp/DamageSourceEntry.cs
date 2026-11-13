using Assets.Scripts.Saves___Serialization.Progression.Stats;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Assets.Scripts.UI.Localization;
using Assets.Scripts.Utility;

public class DamageSourceEntry : MonoBehaviour
{
	public RawImage icon;

	public TextMeshProUGUI t_sourceName;

	public TextMeshProUGUI t_lvl;

	public TextMeshProUGUI t_dmg;

	public TextMeshProUGUI t_dps;

	public void Set(DamageSource dmgSource)
	{
		if (dmgSource == null) return;
		if (icon != null) icon.texture = dmgSource.GetIcon();
		if (t_sourceName != null) t_sourceName.text = LocalizationUtility.GetLocalizedDamageSource(dmgSource.damageSource);
		if (t_dmg != null) t_dmg.text = FormatNumberWithSpaces(dmgSource.damage);
		if (t_lvl != null) t_lvl.text = string.Format("{0}", dmgSource.GetLevel());
		float elapsed = MyTime.time - dmgSource.addedAtTime;
		if (t_dps != null) t_dps.text = FormatNumberWithSpaces(elapsed > 0f ? dmgSource.damage / elapsed : 0f);
	}

	private string FormatNumberWithSpaces(float num)
	{
		return DamageNumbers.FormatDamageNumber(num, "0.0");
	}
}
