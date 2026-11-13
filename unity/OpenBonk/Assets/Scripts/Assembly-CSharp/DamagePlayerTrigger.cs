using Assets.Scripts.Actors.Enemies;
using UnityEngine;

public class DamagePlayerTrigger : MonoBehaviour
{
	private Enemy enemy;

	public float activeTime;

	private float stopTime;

	private bool done;

	private void Awake()
	{
	}

	public void Set(Enemy enemy)
	{
	}

	private void OnTriggerEnter(Collider other)
	{
	}
}
