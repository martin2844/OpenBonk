using UnityEngine;
using Assets.Scripts.Actors.Player;

[RequireComponent(typeof(Collider))]
public class Ladder : MonoBehaviour
{
	private void OnTriggerStay(Collider other)
	{
		if (MyPlayer.Instance != null && other.gameObject == MyPlayer.Instance.gameObject)
			MyPlayer.Instance.playerMovement.RefreshLadder(transform);
	}
}
