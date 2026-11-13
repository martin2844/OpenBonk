using UnityEngine;
using Assets.Scripts.Actors.Player;

public class DisableOutOfCameraRange : MonoBehaviour
{
	private float repeatTime = .5f;

	public float range = 50f;

	private float sqrRange;

	private bool isVisible;

	public bool useXZVector;

	public GameObject[] objectsToUse;

	private void Start()
	{
		Hide();
		sqrRange = range * range;
		InvokeRepeating(nameof(SlowUpdate), Random.Range(0f, repeatTime), repeatTime);
	}

	private void SlowUpdate()
	{
		CheckVisibility();
	}

	private void CheckVisibility()
	{
		if (MyPlayer.Instance == null) return;
		Vector3 difference = MyPlayer.Instance.transform.position - transform.position;
		if (useXZVector) difference.y = 0f;
		float distance = difference.sqrMagnitude;
		if (isVisible && distance > sqrRange) Hide();
		else if (!isVisible && distance < sqrRange) Show();
	}

	private void Hide()
	{
		isVisible = false;
		if (objectsToUse != null) foreach (var obj in objectsToUse) if (obj != null) obj.SetActive(false);
	}

	private void Show()
	{
		isVisible = true;
		if (objectsToUse != null) foreach (var obj in objectsToUse) if (obj != null) obj.SetActive(true);
	}
}
