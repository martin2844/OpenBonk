using UnityEngine;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Game.Spawning;
using Assets.Scripts.Utility;

public class Tornado : MonoBehaviour
{
	public float force = 0.85f;

	public Rigidbody rb;

	private Vector3 desiredVelocity;

	private float speed = 5f;

	private float actualSpeed = 5f;

	private Vector3 lastPos;

	public AudioSource audio;

	private float defaultVolume;

	private float startTime;

	private float stopTime;

	private Vector3 defaultScale;

	private float scaleMultiplier;

	private float fadeTime = 2f;

	private void Start()
	{
		rb.isKinematic = true;
		defaultVolume = audio.volume;
		defaultScale = transform.localScale;
		transform.localScale = Vector3.zero;
		Invoke(nameof(Spawn), 1f);
		scaleMultiplier = Random.Range(0.5f, 4f);
		defaultScale *= scaleMultiplier;
	}

	private void Spawn()
	{
		Vector3 position = SpawnPositions.GetRandomSpawnPositionOnMap(999f);
		rb.isKinematic = false;
		rb.MovePosition(position);
		audio.volume = 0f;
		audio.Play();
		transform.localScale = Vector3.zero;
		speed = Random.Range(4f, 10f);
		FindNewDir();
		startTime = MyTime.time;
		stopTime = startTime + Random.Range(30f, 45f);
	}

	private void FindNewDir()
	{
		Vector3 direction = Random.insideUnitSphere;
		direction.y = 0f;
		desiredVelocity = direction.normalized * speed;
		actualSpeed = 5f;
	}

	private void Update()
	{
		if (MyTime.time < startTime + fadeTime)
		{
			float t = Mathf.Clamp01((MyTime.time - startTime) / fadeTime);
			transform.localScale = Vector3.Lerp(Vector3.zero, defaultScale, t);
			audio.volume = Mathf.Lerp(0f, defaultVolume, t);
		}
		if (MyTime.time > stopTime - fadeTime)
		{
			float t = Mathf.Clamp01((MyTime.time - (stopTime - fadeTime)) / fadeTime);
			transform.localScale = Vector3.Lerp(defaultScale, Vector3.zero, t);
			audio.volume = Mathf.Lerp(defaultVolume, 0f, t);
		}
		if (MyTime.time >= stopTime) Spawn();
	}

	private void FixedUpdate()
	{
		if (rb.isKinematic || MyTime.paused || MyTime.fixedDeltaTime <= 0f) return;
		if (Physics.CheckCapsule(transform.position, transform.position + Vector3.up * 10f, scaleMultiplier * 4f, GameManager.Instance.whatIsPlayer))
		{
			PlayerMovement movement = MyPlayer.Instance.playerMovement;
			Rigidbody playerBody = movement.GetRb();
			playerBody.AddForce(Vector3.up * force * playerBody.mass, ForceMode.Acceleration);
			movement.TouchingTornado();
		}
		rb.velocity = new Vector3(desiredVelocity.x, rb.velocity.y, desiredVelocity.z);
		actualSpeed = Mathf.Lerp(actualSpeed, Vector3.Distance(rb.position, lastPos) / MyTime.fixedDeltaTime, 0.1f);
		if (actualSpeed < 2.5f) FindNewDir();
		lastPos = rb.position;
	}

	private void OnDrawGizmosSelected()
	{
	}
}
