using System.Collections.Generic;
using Assets.Scripts.Utility;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class AnimatedMesh : MonoBehaviour
{
	public delegate void AnimationEndEvent(string Name);

	[SerializeField]
	private AnimatedMeshScriptableObject AnimationSO;

	private MeshFilter Filter;

	[Header("Debug")]
	[SerializeField]
	private int Tick;

	[SerializeField]
	private int AnimationIndex;

	[SerializeField]
	private string AnimationName;

	private List<Mesh> AnimationMeshes;
	private string currentClip;

	public bool paused;

	private float LastTickTime;

	private float tickInterval;

	public bool testing;

	public event AnimationEndEvent OnAnimationEnd;

	public void SetAnimation(AnimatedMeshScriptableObject animation)
	{
		Tick = 1;
		AnimationIndex = 0;
		AnimationSO = animation;
		AnimationMeshes = animation.Animations[0].Meshes;
		currentClip = animation.Animations[0].Name;
		Filter.sharedMesh = AnimationMeshes[0];
		tickInterval = animation.AnimationFPS;
		paused = false;
	}

	public void SelectClip(string name)
	{
		if (currentClip == name) return;
		foreach (var candidate in AnimationSO.Animations)
		{
			if (candidate.Name != name || candidate.Meshes.Count == 0) continue;
			AnimationMeshes = candidate.Meshes;
			currentClip = name;
			AnimationIndex = 0;
			LastTickTime = MyTime.time;
			Filter.sharedMesh = AnimationMeshes[0];
			break;
		}
	}

	private void Awake()
	{
		Filter = GetComponent<MeshFilter>();
		if (testing) SetAnimation(AnimationSO);
	}

	public void Pause()
	{
		paused = true;
	}

	public void UnPause()
	{
		paused = false;
	}

	private void Update()
	{
		if (AnimationMeshes == null || paused) return;
		if (MyTime.time >= LastTickTime + 1f / tickInterval)
		{
			Filter.sharedMesh = AnimationMeshes[AnimationIndex];
			AnimationIndex++;
			if (AnimationIndex >= AnimationMeshes.Count) AnimationIndex = 0;
			LastTickTime = MyTime.time;
		}
		Tick++;
	}
}
