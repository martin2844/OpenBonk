using UnityEngine;
using System.Collections.Generic;

public class EnemyStatusSymbols : MonoBehaviour
{
	public GameObject elite;

	public GameObject boss;

	public GameObject challenge;

	public GameObject bossMinimapIcon;

	public float size;

	public float padding;

	public void Set(bool isElite, bool isBoss, bool isChallenge)
	{
		elite.SetActive(isElite);
		boss.SetActive(isBoss);
		challenge.SetActive(isChallenge);
		bossMinimapIcon.SetActive(isBoss);
		var visible = new List<Transform>(3);
		if (isElite) visible.Add(elite.transform);
		if (isBoss) visible.Add(boss.transform);
		if (isChallenge) visible.Add(challenge.transform);
		for (int i = 0; i < visible.Count; i++)
			visible[i].localPosition = new Vector3((i - (visible.Count - 1) * 0.5f) * (size + padding), 0f, 0f);
	}
}
