using Assets.Scripts.Actors.Player;
using Assets.Scripts.Movement;
using Assets.Scripts.Utility;
using UnityEngine;

public class PlayerSfxs : MonoBehaviour
{
	public static PlayerSfxs Instance;

	public RandomSfx sourceEvade;

	public RandomSfx sourceFlex;

	public RandomSfx slideStart;

	public AudioSource slideLoop;

	public AudioClip evade;

	public AudioClip evadePhantom;

	public AudioSource windAudio;

	private float maxVolume = 0.32f;

	private float maxPitch = 1.8f;

	private float minSpeed = 7f;

	private float maxSpeed = 55f;

	public GameObject grindFx;

	public RandomSfx sourceGrindAction;

	public RandomSfx sourceGrindLoop;

	public AudioClip grindStart;

	public AudioClip grindStop;

	private float avgSpeed;

	private bool wasPlayingGrind;

	private void Start()
	{
		MyTime.A_Pause += OnPause;
	}

	private void OnDestroy()
	{
		MyTime.A_Pause -= OnPause;
	}

	private void Update()
	{
		var player = MyPlayer.Instance;
		if (MyTime.paused || player == null || player.playerMovement == null || !player.playerMovement.HasRigidbody) return;
		UpdateSliding();
		UpdateWind();
	}

	public void Evade(bool phantom)
	{
		if (sourceEvade == null) return;
		sourceEvade.sounds = new[] { phantom ? evadePhantom : evade };
		sourceEvade.Play();
	}

	public void Flex()
	{
		if (sourceFlex != null) sourceFlex.Play();
	}

	public void StartGrind()
	{
		sourceGrindAction.sounds = new[] { grindStart };
		sourceGrindAction.Play();
		sourceGrindLoop.Play();
		grindFx.SetActive(true);
	}

	public void StopGrind()
	{
		sourceGrindAction.sounds = new[] { grindStop };
		sourceGrindAction.Play();
		sourceGrindLoop.Stop();
		grindFx.SetActive(false);
	}

	private void UpdateSliding()
	{
		var player = MyPlayer.Instance;
		if (player == null || player.inventory == null || player.character == ECharacter.TonyMcZoom || slideLoop == null) return;
		if (player.inventory.playerHealth == null) return;
		if (player.IsDead())
		{
			if (slideLoop.isPlaying) slideLoop.Stop();
			return;
		}
		EMovementState state = player.playerMovement.GetMovementState();
		bool sliding = (state & EMovementState.Sliding) != 0 && (state & EMovementState.Airborne) == 0;
		slideLoop.volume = Mathf.Lerp(slideLoop.volume, sliding ? 0.35f : 0f, Time.deltaTime * 12f);
		if (sliding && !slideLoop.isPlaying)
		{
			if (slideStart != null) slideStart.Play();
			slideLoop.Play();
		}
		else if (!sliding && slideLoop.volume <= 0.05f)
		{
			slideLoop.volume = 0f;
			slideLoop.Stop();
		}
	}

	private void UpdateWind()
	{
		var player = MyPlayer.Instance;
		if (player == null || player.playerMovement == null || !player.playerMovement.HasRigidbody || windAudio == null || maxSpeed <= 0f) return;
		float speed = Mathf.Clamp(player.playerMovement.GetVelocity().magnitude - minSpeed, 0f, maxSpeed);
		avgSpeed = Mathf.Lerp(avgSpeed, speed, Time.deltaTime * 4f);
		float ratio = avgSpeed / maxSpeed;
		windAudio.volume = maxVolume * ratio;
		windAudio.pitch = 1f + maxPitch * ratio;
	}

	private void OnPause(bool pause)
	{
		if (sourceGrindLoop != null && sourceGrindLoop.s != null)
		{
			if (pause) sourceGrindLoop.s.Pause(); else sourceGrindLoop.s.UnPause();
		}
		if (windAudio != null)
		{
			if (pause) windAudio.Pause(); else windAudio.UnPause();
		}
	}
}
