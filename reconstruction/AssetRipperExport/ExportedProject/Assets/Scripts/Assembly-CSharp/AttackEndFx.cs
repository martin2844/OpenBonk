using UnityEngine;

public class AttackEndFx : MonoBehaviour
{
	public ParticleSystem ps;

	private void OnEnable()
	{
		if (ps != null) ps.Play();
	}
}
