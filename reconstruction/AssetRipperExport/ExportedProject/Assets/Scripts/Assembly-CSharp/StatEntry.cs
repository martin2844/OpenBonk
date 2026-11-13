using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Actors.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StatEntry : MonoBehaviour
{
	public RawImage icon;

	public TextMeshProUGUI t_stat;

	public TextMeshProUGUI t_value;

	private EStat stat;

	public ToolTipObject toolTipObject;

	private void OnEnable()
	{
	}

	private void RefreshValues()
	{
	}

	public void Set(EStat stat)
	{
		this.stat = stat;
		t_stat.text = EnumUtility.EnumToReadable(stat);
		bool preciseExternalPassive = MyPlayer.Instance != null && MyPlayer.Instance.inventory != null && MyPlayer.Instance.inventory.passiveAbility is OpenBonkLevelPassive;
		t_value.text = StatsUi.FormatStat(stat, Assets.Scripts.Inventory.Stats.PlayerStats.GetStat(stat), preciseExternalPassive);
	}
}
