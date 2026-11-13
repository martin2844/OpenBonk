using MilkShake;
using UnityEngine;

public class CameraMovement : MonoBehaviour
{
	public Shaker shaker;

	public ShakePreset preset;

	private void Start()
	{
		if (shaker != null && preset != null)
			shaker.Shake(preset);
	}
}
