using System.Collections.Generic;
using Assets.Scripts.Utility;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class AnimatedMesh : MonoBehaviour
{
	public delegate void AnimationEndEvent(string Name);

	[SerializeField] private AnimatedMeshScriptableObject AnimationSO;
	private MeshFilter Filter;

	[Header("Debug")]
	[SerializeField] private int Tick;
	[SerializeField] private int AnimationIndex;
	[SerializeField] private string AnimationName;

	private List<Mesh> AnimationMeshes;
	private string currentClip;
	public bool paused;
	private float LastTickTime;
	private float tickInterval;
	private double framePosition;
	private float playbackSpeed = 1f;
	private bool preserveLocomotionPhase;
	public bool testing;
	public event AnimationEndEvent OnAnimationEnd;

	public void SetAnimation(AnimatedMeshScriptableObject animation)
	{
		if (Filter == null) Filter = GetComponent<MeshFilter>();
		Tick = 1;
		AnimationIndex = 0;
		AnimationSO = animation;
		AnimationMeshes = animation.Animations[0].Meshes;
		currentClip = AnimationName = animation.Animations[0].Name;
		tickInterval = Mathf.Max(1, animation.AnimationFPS);
		// Pool reuse must not inherit the previous occupant's speed, phase or clock.
		framePosition = 0;
		playbackSpeed = 1f;
		preserveLocomotionPhase = false;
		LastTickTime = MyTime.time;
		paused = false;
		DisplayFrame();
	}

	// Presentation-only variation, independent of both Unity and combat RNG.
	// Called only for external map enemies after their normal pooled initialization.
	public void ConfigureExternalPlayback(uint seed)
	{
		preserveLocomotionPhase = true;
		uint hash = unchecked(seed + 0x9e3779b9u);
		hash = unchecked((hash ^ (hash >> 16)) * 0x85ebca6bu);
		hash = unchecked((hash ^ (hash >> 13)) * 0xc2b2ae35u);
		hash ^= hash >> 16;
		framePosition = (hash & 0x00ffffffu) / 16777216.0 * AnimationMeshes.Count;
		LastTickTime = MyTime.time;
		DisplayFrame();
	}

	public void SetPlaybackSpeed(float speed)
	{
		// Account for elapsed time at the old rate before changing it.
		Advance(MyTime.time);
		playbackSpeed = float.IsNaN(speed) || float.IsInfinity(speed) ? 1f : Mathf.Clamp(speed, .25f, 2f);
	}

	public void SelectClip(string name)
	{
		if (currentClip == name || AnimationSO == null) return;
		foreach (var candidate in AnimationSO.Animations)
		{
			if (candidate.Name != name || candidate.Meshes.Count == 0) continue;
			Advance(MyTime.time);
			bool locomotion = preserveLocomotionPhase && IsLocomotion(currentClip) && IsLocomotion(name);
			double phase = locomotion ? framePosition / AnimationMeshes.Count : 0;
			AnimationMeshes = candidate.Meshes;
			currentClip = AnimationName = name;
			framePosition = phase * AnimationMeshes.Count;
			// Charge/attack clips begin at their authored first pose and normal rate.
			playbackSpeed = 1f;
			LastTickTime = MyTime.time;
			DisplayFrame();
			break;
		}
	}

	private static bool IsLocomotion(string name) => name == "walk" || name == "idle";

	private void Awake()
	{
		Filter = GetComponent<MeshFilter>();
		if (testing) SetAnimation(AnimationSO);
	}

	public void Pause()
	{
		Advance(MyTime.time);
		paused = true;
	}

	public void UnPause()
	{
		// Keep the fractional frame, but never catch up through a local freeze.
		LastTickTime = MyTime.time;
		paused = false;
	}

	private void Update()
	{
		Advance(MyTime.time);
		Tick++;
	}

	private void Advance(float now)
	{
		double elapsed = (double)now - LastTickTime;
		LastTickTime = now;
		if (AnimationMeshes == null || AnimationMeshes.Count == 0 || paused || MyTime.paused || elapsed <= 0) return;
		// Keep fractional time and skip directly to the correct shared mesh after
		// a slow frame. No per-frame allocations or per-enemy mesh deformation.
		framePosition = (framePosition + elapsed * tickInterval * playbackSpeed) % AnimationMeshes.Count;
		DisplayFrame();
	}

	private void DisplayFrame()
	{
		if (AnimationMeshes == null || AnimationMeshes.Count == 0) return;
		// Float game-clock rounding must not leave an exact boundary one frame late.
		AnimationIndex = (int)(framePosition + .00001) % AnimationMeshes.Count;
		if (Filter.sharedMesh != AnimationMeshes[AnimationIndex]) Filter.sharedMesh = AnimationMeshes[AnimationIndex];
	}
}
