using System.Collections.Generic;
using UnityEngine;

public class MenuEnts : MonoBehaviour
{
	private class EntData
	{
		public GameObject obj;

		public int dir;

		public float speed;
	}

	public GameObject entPrefab;

	public Transform leftPos;

	public Transform rightPos;

	private float baseSpeed = 1.4f;

	private List<EntData> ents;

	private void Start()
	{
		ents = new List<EntData>();
		if (entPrefab == null || leftPos == null || rightPos == null) return;
		int count = Random.Range(1, 7);
		for (int i = 0; i < count; i++)
		{
			Vector3 position = Vector3.Lerp(leftPos.position, rightPos.position, Random.value);
			GameObject obj = Instantiate(entPrefab, position, Quaternion.identity);
			float scale = Random.Range(0.85f, 1.5f);
			obj.transform.localScale = entPrefab.transform.localScale * scale;
			obj.transform.position += Vector3.forward * Random.Range(0f, 50f);
			Animator animator = obj.GetComponent<Animator>();
			if (animator != null)
			{
				animator.Play("Walking_Menu", 0, Random.value);
				animator.speed = Random.Range(0.85f, 1.15f);
			}
			int dir = Random.value < 0.5f ? -1 : 1;
			FaceDirection(obj.transform, dir);
			ents.Add(new EntData { obj = obj, dir = dir, speed = baseSpeed * scale });
		}
		entPrefab.SetActive(false);
	}

	private void Update()
	{
		if (ents == null || leftPos == null || rightPos == null) return;
		foreach (EntData ent in ents)
		{
			if (ent.obj == null) continue;
			Transform tr = ent.obj.transform;
			Vector3 position = tr.position + Vector3.right * ent.dir * ent.speed * Time.deltaTime;
			if (ent.dir > 0 && position.x > rightPos.position.x) position.x = leftPos.position.x;
			else if (ent.dir < 0 && position.x < leftPos.position.x) position.x = rightPos.position.x;
			tr.position = position;
		}
	}

	private void FaceDirection(Transform tr, int dir)
	{
		if (tr != null && entPrefab != null) tr.rotation = Quaternion.LookRotation(entPrefab.transform.forward * -dir, Vector3.up);
	}
}
