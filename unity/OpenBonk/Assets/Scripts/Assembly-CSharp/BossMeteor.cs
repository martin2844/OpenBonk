using UnityEngine;

public class BossMeteor : MonoBehaviour
{
	public Transform destination;

	public float speed;

	public GameObject explosionFx;

	public FianlBossCinematic cinematic;

	private void Update()
	{
		if (destination == null) return;
		transform.position = Vector3.MoveTowards(transform.position, destination.position, speed * Time.deltaTime);
		if (transform.position.y > destination.position.y) return;
		if (explosionFx != null)
		{
			explosionFx.SetActive(true);
			explosionFx.transform.SetParent(null);
		}
		Destroy(gameObject);
		if (cinematic != null && cinematic.shaker != null) cinematic.Impact();
		ControllerShaker.Shake(0, .8f, 1f);
	}
}
