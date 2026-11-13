using UnityEngine;
using Assets.Scripts.Actors.Player;

public class LookTowardsPlayer : MonoBehaviour
{
	public bool onlyXZ;

	public bool lerp;

	private void Update()
	{
		if (!MyPlayer.Instance)
			return;

		if (!onlyXZ)
		{
			transform.LookAt(MyPlayer.Instance.transform);
			return;
		}

		Vector3 target = MyPlayer.Instance.transform.position;
		target.y = transform.position.y;
		if (lerp)
			transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(target - transform.position), Time.deltaTime * 2f);
		else
			transform.LookAt(target);
	}
}
