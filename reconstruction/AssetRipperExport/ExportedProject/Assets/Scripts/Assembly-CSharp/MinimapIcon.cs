using UnityEngine;
using Assets.Scripts.Actors.Player;

public class MinimapIcon : MonoBehaviour
{
	public bool isStatic;

	public bool alignToCamera;

	private void Start()
	{
		transform.rotation = Quaternion.Euler(90f, 0f, 0f);
		if (isStatic) Destroy(this);
	}

	private void Update()
	{
		if (alignToCamera && MyPlayer.Instance != null && MyPlayer.Instance.minimapCamera != null)
			transform.rotation = Quaternion.Euler(90f, 0f, -MyPlayer.Instance.minimapCamera.transform.eulerAngles.y);
		// The shipped routine resets non-static icons after the optional alignment.
		if (!isStatic) transform.rotation = Quaternion.Euler(90f, 0f, 0f);
	}
}
