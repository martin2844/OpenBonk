using System.Collections.Generic;
using Assets.Scripts.Actors.Player;
using UnityEngine;

public class AegisRenderer : MonoBehaviour
{
	public GameObject prefab;

	private List<GameObject> prefabs = new List<GameObject>();

	public float forwardOffset;

	private bool inited;

	private int maxAmount = 30;

	private void TryInit()
	{
		if (inited) return;
		inited = true;
		prefabs.Add(prefab);
	}

	public void SetAmount(int amount)
	{
		amount = Mathf.Min(maxAmount, amount);
		TryInit();
		HideAll();
		for (int i = 0; i < amount; i++)
		{
			if (i >= prefabs.Count) prefabs.Add(Instantiate(prefab, transform));
			prefabs[i].SetActive(true);
		}
		for (int i = 0; i < amount; i++)
		{
			float angle = 360f / amount * i;
			Quaternion rotation = Quaternion.Euler(0f, angle, 0f);
			float radius = forwardOffset > 0f ? forwardOffset : MyPlayer.Instance.width * 3.5f;
			prefabs[i].transform.localPosition = rotation * Vector3.forward * radius;
			prefabs[i].transform.localRotation = rotation;
		}
	}

	private void HideAll()
	{
		foreach (GameObject instance in prefabs) instance.SetActive(false);
	}
}
