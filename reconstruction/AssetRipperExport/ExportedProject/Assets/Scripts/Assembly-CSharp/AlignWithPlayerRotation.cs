using UnityEngine;
using Assets.Scripts.Actors.Player;

public class AlignWithPlayerRotation : MonoBehaviour
{
	private void Update()
	{
		if (MyPlayer.Instance != null && MyPlayer.Instance.playerRenderer != null)
			transform.up = MyPlayer.Instance.playerRenderer.transform.up;
	}
}
