using UnityEngine;
using Assets.Scripts.Actors.Player;

[CreateAssetMenu(menuName = "Me/Progression/Challenge/ChallengeModifiers/Smol boi", order = 1)]
public class ChallengeModifierSmolBoi : ChallengeModifier
{
	public override void Init(ChallengeData challengeData)
	{
		MyPlayer.A_PlayerInventoryInitialized += OnPlayerInit;
	}

	public override void Cleanup()
	{
		MyPlayer.A_PlayerInventoryInitialized -= OnPlayerInit;
	}

	private void OnPlayerInit(PlayerInventory obj)
	{
		var player = MyPlayer.Instance;
		player.RefreshSize(DataManager.Instance.GetCharacterData(player.character), player.spawnDir, 0.5f);
	}
}
