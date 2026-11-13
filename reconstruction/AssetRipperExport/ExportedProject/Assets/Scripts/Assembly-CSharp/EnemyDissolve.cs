using System;
using Assets.Scripts.Utility;
using Assets.Scripts.Actors.Enemies;
using UnityEngine;

public class EnemyDissolve : MonoBehaviour
{
	public Material dissolveMaterial;

	private float dissolveDuration = 0.3f;

	private float dissolveAmount;

	private float displaceAmount;

	private float displaceTarget = 0.15f;

	private bool isDissolving;

	public Renderer enemyRenderer;

	private MaterialPropertyBlock mpb;

	public Enemy enemy;

	public Action A_DissolveFinished;

	private void Update()
	{
		if (!isDissolving || MyTime.paused) return;
		dissolveAmount += MyTime.deltaTime / dissolveDuration;
		mpb.SetFloat("_DissolveAmount", dissolveAmount);
		mpb.SetFloat("_DisplacementStrength", displaceTarget * dissolveAmount);
		enemyRenderer.SetPropertyBlock(mpb);
		if (dissolveAmount >= 1f) { isDissolving = false; dissolveAmount = 1f; A_DissolveFinished?.Invoke(); }
	}

	public void StartDissolve()
	{
		if (isDissolving) return;
		isDissolving = true;
		dissolveAmount = 0f;
		if (enemyRenderer.sharedMaterial != dissolveMaterial) enemyRenderer.sharedMaterial = dissolveMaterial;
		mpb.SetFloat("_DissolveAmount", 0f);
	}

	public void Reset()
	{
#if UNITY_ANDROID && !UNITY_EDITOR
		// Some Android GLES drivers fail to draw the instanced dissolve
		// variant. Per-renderer property blocks still work without batching.
		if (dissolveMaterial != null) dissolveMaterial.enableInstancing = false;
#endif
		if (mpb == null) mpb = new MaterialPropertyBlock();
		isDissolving = false;
		dissolveAmount = displaceAmount = 0f;
		mpb.SetFloat("_DissolveAmount", 0f);
		mpb.SetFloat("_DisplacementStrength", displaceTarget * dissolveAmount);
	}
}
