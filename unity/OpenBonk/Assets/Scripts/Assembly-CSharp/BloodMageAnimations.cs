using UnityEngine;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Utility;

public class BloodMageAnimations : MonoBehaviour
{
	public AudioSource audioHoverLoop;

	private float defaultVolume;

	private float currentPitch;

	private void Awake()
	{
		defaultVolume = audioHoverLoop.volume;
	}

	private void Update()
	{
		if (MyPlayer.Instance == null) return;
		float speed = Mathf.Clamp01(MyPlayer.Instance.playerMovement.GetSpeedHorizontal() / 50f);
		currentPitch = Mathf.Lerp(currentPitch, 1f + speed * 0.04999995231628418f, MyTime.deltaTime * 2f);
		audioHoverLoop.pitch = currentPitch;
		float pulse = 1f + 0.15f - Mathf.Abs(Mathf.Repeat(MyTime.time * 0.6f, 0.3f) - 0.15f);
		audioHoverLoop.volume = defaultVolume * (speed * 0.7f + 0.3f) * pulse;
	}
}
