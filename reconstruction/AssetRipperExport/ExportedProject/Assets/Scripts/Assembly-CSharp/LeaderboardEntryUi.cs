using Assets.Scripts.Menu.Shop.Leaderboards;
using Assets.Scripts.Steam;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LeaderboardEntryUi : MyButton
{
	public RawImage playerIcon;

	public RawImage characterIcon;

	public TextMeshProUGUI playerName;

	public TextMeshProUGUI rank;

	public TextMeshProUGUI score;

	public RawImage localHighlight;

	public MaskableGraphic outlineColor;

	public Color colorDefault;

	public Color colorGold;

	public GameObject hoverOverlay;

	private LeaderboardEntry entry;

	private new void Awake()
	{
	}

	public override void StartHover()
	{
	}

	public override void StopHover()
	{
	}

	protected override void OnClick()
	{
	}

	private void OnDestroy()
	{
	}

	public void Set(LeaderboardEntry entry, int rankIndex, ELeaderboardCategory category = ELeaderboardCategory.Kills)
	{
	}

	public void Clear()
	{
	}

	private void OnPlayerInformationArrived(ulong steamid)
	{
	}

	private void LoadAvatar()
	{
	}
}
