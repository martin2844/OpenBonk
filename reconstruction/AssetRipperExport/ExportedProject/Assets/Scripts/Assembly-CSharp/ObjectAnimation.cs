using UnityEngine;
using Assets.Scripts.Utility;

public class ObjectAnimation : MonoBehaviour
{
	public float scale = .2f;

	public float scaleSpeed = 2f;

	private Vector3 defaultScale;

	private void Awake()
	{
		defaultScale = transform.localScale;
	}

	private void Update()
	{
		float factor = 1f + Mathf.PingPong(MyTime.time * scaleSpeed, scale) - scale * .5f;
		transform.localScale = Vector3.Lerp(transform.localScale, defaultScale * factor, MyTime.deltaTime * 10f);
	}
}
