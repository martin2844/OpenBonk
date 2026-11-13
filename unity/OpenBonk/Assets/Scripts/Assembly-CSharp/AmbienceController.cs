using Assets.Scripts.Managers;
using UnityEngine;

public class AmbienceController : MonoBehaviour
{
	public AudioClip dungeonAmbience;

	public AudioSource audioSource;

	private void Start()
	{
		OnDungeonEnded();
		if (GameManager.Instance != null && GameManager.Instance.isCrypt) OnDungeonStarted();
	}

	private void Awake()
	{
		GameManager.A_DungeonStarted += OnDungeonStarted;
		GameManager.A_DungeonEnded += OnDungeonEnded;
	}

	private void OnDestroy()
	{
		GameManager.A_DungeonStarted -= OnDungeonStarted;
		GameManager.A_DungeonEnded -= OnDungeonEnded;
	}

	private void OnDungeonStarted()
	{
		if (audioSource == null) return;
		audioSource.clip = dungeonAmbience;
		audioSource.Play();
	}

	private void OnDungeonEnded()
	{
		if (audioSource == null || MapController.currentMap == null) return;
		audioSource.clip = MapController.currentMap.ambience;
		audioSource.Play();
	}
}
