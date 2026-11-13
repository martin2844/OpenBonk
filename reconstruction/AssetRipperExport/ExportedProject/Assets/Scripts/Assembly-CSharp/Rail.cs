using UnityEngine;
using UnityEngine.Splines;
using Assets.Scripts.Utility;

public class Rail : MonoBehaviour
{
	private bool isRailingPlayer;

	private float stopRailTime;

	public SplineContainer splineContainer;

	private float restoreCollisionAtTime;

	private bool isIgnoringCollision;

	public Collider renderCollider;

	private Collider playerCollider;

	public bool IsOnCooldown()
	{
		return restoreCollisionAtTime > MyTime.time;
	}

	public void Cooldown(Collider playerCollider)
	{
		isIgnoringCollision = true;
		this.playerCollider = playerCollider;
		Physics.IgnoreCollision(renderCollider, playerCollider, true);
		restoreCollisionAtTime = MyTime.time + 0.5f;
	}

	private void FixedUpdate()
	{
		if (!isIgnoringCollision || MyTime.time <= restoreCollisionAtTime) return;
		Physics.IgnoreCollision(renderCollider, playerCollider, false);
		isIgnoringCollision = false;
	}

	public bool IsValidPosition()
	{
		for (int i = 0; i < 20; i++)
		{
			Vector3 start = splineContainer.transform.TransformPoint(splineContainer.Spline.EvaluatePosition(i / 20f)) + Vector3.up * 0.5f;
			Vector3 end = splineContainer.transform.TransformPoint(splineContainer.Spline.EvaluatePosition((i + 1) / 20f)) + Vector3.up * 0.5f;
			Vector3 delta = end - start;
			foreach (RaycastHit hit in Physics.RaycastAll(start, delta.normalized, delta.magnitude, GameManager.Instance.whatIsBlockingRails))
			{
				if (hit.collider != renderCollider)
					return false;
			}
		}
		return true;
	}

	private void OnValidate()
	{
	}
}
