using UnityEngine;
using Assets.Scripts.Actors.Player;

public class MinimapPlayerIcon : MonoBehaviour
{
	private void Update()
	{
		var player = MyPlayer.Instance;
		if (player == null || player.playerMovement == null || player.playerMovement.orientation == null) return;
		transform.rotation = Quaternion.Euler(90f, player.playerMovement.orientation.eulerAngles.y, 0f);
	}
}
