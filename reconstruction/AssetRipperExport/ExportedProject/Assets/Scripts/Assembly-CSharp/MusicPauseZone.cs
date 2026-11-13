using UnityEngine;
using Assets.Scripts.Actors.Player;

public class MusicPauseZone : MonoBehaviour
{
	public float radius;

	private float nextCheckTime;

	private float checkDelay = .1f;

	private void Update()
	{
		if (Time.time < nextCheckTime) return;
		nextCheckTime = Time.time + checkDelay;
		if (MyPlayer.Instance == null || MusicController.Instance == null) return;
		float influence = Vector3.Distance(transform.position, MyPlayer.Instance.transform.position) < radius ? .12f : 1f;
		MusicController.Instance.RegisterZoneInfluence(this, influence);
	}

	private void OnDrawGizmosSelected()
	{
	}
}
