using System;
using UnityEngine;

public class DestroyObject : MonoBehaviour
{
	public float time;

	public Action OnDestroy;

	private void Start()
	{
		Invoke(nameof(DestroySelf), time);
	}

	private void DestroySelf()
	{
		OnDestroy?.Invoke();
		Destroy(gameObject);
	}
}
