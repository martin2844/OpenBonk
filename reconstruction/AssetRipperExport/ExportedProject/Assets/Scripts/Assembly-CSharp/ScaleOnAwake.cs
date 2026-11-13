using UnityEngine;

public class ScaleOnAwake : MonoBehaviour
{
	private Vector3 defaultScale;

	public float scaleTime = 0.5f;

	private float t;

	private void OnEnable()
	{
		if (defaultScale == Vector3.zero) FindScale();
		transform.localScale = Vector3.zero;
		t = 0f;
	}

	private void FindScale()
	{
		defaultScale = transform.localScale;
	}

	private void Update()
	{
		if (t >= 1f) return;
		t += Time.deltaTime / scaleTime;
		float eased = UnityEngine.UIElements.Experimental.Easing.InOutCirc(Mathf.Clamp01(t));
		transform.localScale = Vector3.Lerp(transform.localScale, defaultScale, eased);
	}
}
