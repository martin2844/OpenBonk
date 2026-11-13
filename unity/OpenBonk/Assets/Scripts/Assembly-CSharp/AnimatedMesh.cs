using System.Collections.Generic;
using System.Runtime.CompilerServices;
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

	public bool paused;

	private float LastTickTime;

	private float tickInterval;

	public bool testing;

	public event AnimationEndEvent OnAnimationEnd
	{
		[CompilerGenerated]
		add
		{
		}
		[CompilerGenerated]
		remove
		{
		}
	}

	public void SetAnimation(AnimatedMeshScriptableObject animation)
	{
		Tick = 1;
		AnimationIndex = 0;
		AnimationSO = animation;
		AnimationMeshes = animation.Animations[0].Meshes;
		Filter.mesh = AnimationMeshes[0];
		tickInterval = animation.AnimationFPS;
		paused = false;
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
			Filter.mesh = AnimationMeshes[AnimationIndex];
			AnimationIndex++;
			if (AnimationIndex >= AnimationMeshes.Count) AnimationIndex = 0;
			LastTickTime = MyTime.time;
		}
		Tick++;
	}
}
