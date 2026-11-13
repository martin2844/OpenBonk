using UnityEngine;

public class MenuCamera : MonoBehaviour
{
	public Transform defaultTransform;

	public Transform characterSelectionTransform;

	private Transform currentTransform;

	public Camera camera;

	private void Start()
	{
		if (camera != null) camera.useOcclusionCulling = false;
		currentTransform = defaultTransform;
	}

	public void GoToCharacters()
	{
		currentTransform = characterSelectionTransform;
	}

	public void GoToMain()
	{
		currentTransform = defaultTransform;
	}

	private void Update()
	{
		if (currentTransform == null) return;
		float step = Time.deltaTime * 1.5f;
		transform.position = Vector3.Lerp(transform.position, currentTransform.position, step);
		transform.rotation = Quaternion.Lerp(transform.rotation, currentTransform.rotation, step);
	}
}
