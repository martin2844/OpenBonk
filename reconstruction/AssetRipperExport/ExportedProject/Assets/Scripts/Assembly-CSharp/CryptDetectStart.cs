using UnityEngine;
using Assets.Scripts.Actors.Player;

public class CryptDetectStart : MonoBehaviour
{
	private void OnTriggerEnter(Collider other)
	{
		if (other.gameObject != MyPlayer.Instance.gameObject || GameManager.Instance == null) return;
		GameManager.Instance.StartDungeonTimer();
	}
}
