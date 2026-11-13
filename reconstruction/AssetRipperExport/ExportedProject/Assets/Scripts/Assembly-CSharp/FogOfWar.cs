using UnityEngine;
using Assets.Scripts.Actors.Player;

public class FogOfWar : MonoBehaviour
{
	private void Update()
	{
		if (MyPlayer.Instance != null) transform.position = MyPlayer.Instance.transform.position;
	}
}
