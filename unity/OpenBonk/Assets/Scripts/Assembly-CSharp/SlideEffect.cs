using Assets.Scripts.Actors.Player;
using UnityEngine;

public class SlideEffect : MonoBehaviour
{
	public PlayerMovement playerMovement;
	public GameObject parent;
	public GameObject dirtParticles;
	private float minSpeed = 8f;

	private void Awake()
	{
		transform.SetParent(null);
	}

	private void Update()
	{
		MyPlayer player = MyPlayer.Instance;
		if (player == null || player.playerMovement == null || player.playerMovement.GetRb() == null || player.character == ECharacter.TonyMcZoom)
		{
			gameObject.SetActive(false);
			return;
		}
		if (playerMovement == null || parent == null) return;
		if (!playerMovement.IsSliding() || playerMovement.GetSpeed() < minSpeed)
		{
			if (parent.activeInHierarchy) parent.SetActive(false);
			return;
		}
		if (!parent.activeInHierarchy) parent.SetActive(true);
		Rigidbody body = playerMovement.GetRb();
		if (body == null) return;
		Vector3 direction = body.velocity.normalized;
		transform.position = playerMovement.GetRbFeetPosition() + direction * .8f;
		transform.rotation = Quaternion.LookRotation(direction);
		if (dirtParticles != null && dirtParticles.activeInHierarchy != playerMovement.grounded)
			dirtParticles.SetActive(playerMovement.grounded);
	}
}
