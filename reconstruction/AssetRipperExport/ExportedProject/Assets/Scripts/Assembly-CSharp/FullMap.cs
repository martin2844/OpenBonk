using System.Collections.Generic;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory.Stats;
using Assets.Scripts.MapGeneration;
using Assets.Scripts.Saves___Serialization.Progression.Challenges;
using Assets.Scripts.UI.InGame.FullMap;
using Assets.Scripts.Utility;
using UnityEngine;

public class FullMap : MonoBehaviour
{
	public Camera mapCamera;
	private float worldSize;
	private int textureSize = 64;
	private float revealRadius = 110f;
	private Texture2D fogTexture;
	private Color32[] pixels;
	public Material mapMaterial;
	public Transform mapDisplayTransform;
	public GameObject statsWindow;
	private int mapsOpen;
	private bool[] fullyRevealed;
	private bool[] visitedCords;
	private HashSet<int> revealedIndices = new HashSet<int>();
	private Vector3 lastPos;

	private void Awake()
	{
		MapGenerationController.A_GenerationComplete += OnGenerationComplete;
		FullMapUi.A_Toggle += OnMapToggle;
	}
	private void OnDestroy()
	{
		MapGenerationController.A_GenerationComplete -= OnGenerationComplete;
		FullMapUi.A_Toggle -= OnMapToggle;
		if (fogTexture != null) Destroy(fogTexture);
	}
	private void OnMapToggle(bool on) { mapsOpen += on ? 1 : -1; }
	private void OnGenerationComplete()
	{
		mapCamera.transform.SetParent(null);
		worldSize = MapInfo.mapSize.x;
		mapCamera.transform.position = Vector3.up * 1000f;
		mapCamera.orthographicSize = worldSize * .5f;
		InitFogTexture();
	}
	private void InitFogTexture()
	{
		fullyRevealed = new bool[textureSize * textureSize];
		visitedCords = new bool[textureSize * textureSize];
		if (fogTexture != null) Destroy(fogTexture);
		fogTexture = new Texture2D(textureSize, textureSize, TextureFormat.R8, false) { filterMode = FilterMode.Bilinear };
		pixels = new Color32[textureSize * textureSize];
		for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color32(0, 0, 0, 255);
		fogTexture.SetPixels32(pixels);
		fogTexture.Apply();
		mapMaterial.SetTexture("_FogTex", fogTexture);
		revealedIndices.Clear();
	}
	private void Update()
	{
		if (!PlayerStats.HasStats()) return;
		bool show = MyInputManager.GetButton(MyInputManager.MapOverlay) && !MyTime.paused;
		if (mapDisplayTransform.gameObject.activeInHierarchy == show) return;
		mapDisplayTransform.gameObject.SetActive(show);
		statsWindow.SetActive(show);
	}
	private void FixedUpdate()
	{
		if (fogTexture == null || MyPlayer.Instance == null || ChallengesTracker.HasChallengeModifier("blind")) return;
		QueueRevealFog(MyPlayer.Instance.transform.position);
		if (IsMapOpen()) RevealFog();
	}
	private void QueueRevealFog(Vector3 worldPos)
	{
		if (Vector3.Distance(MyPlayer.Instance.transform.position, lastPos) < 1f) return;
		lastPos = MyPlayer.Instance.transform.position;
		float half = worldSize * .5f;
		if (worldSize <= 0f || worldPos.x < -half || worldPos.x > half || worldPos.z < -half || worldPos.z > half) return;
		int x = (int)((worldPos.x + half) / worldSize * textureSize);
		int y = (int)((worldPos.z + half) / worldSize * textureSize);
		int index = y * textureSize + x;
		if (index < 0 || index >= textureSize * textureSize || visitedCords[index]) return;
		visitedCords[index] = true;
		revealedIndices.Add(index);
	}
	private bool IsMapOpen() { return mapsOpen > 0; }
	private void RevealFog()
	{
		if (revealedIndices.Count == 0) return;
		int radius = Mathf.CeilToInt(revealRadius / worldSize * textureSize);
		bool changed = false;
		foreach (int center in revealedIndices)
		{
			int centerX = center % textureSize, centerY = center / textureSize;
			for (int dy = -radius; dy <= radius; dy++)
				for (int dx = -radius; dx <= radius; dx++)
				{
					int x = centerX + dx, y = centerY + dy;
					if (x < 0 || y < 0 || x >= textureSize || y >= textureSize) continue;
					int index = y * textureSize + x;
					int distanceSquared = dx * dx + dy * dy;
					if (fullyRevealed[index] || distanceSquared > radius * radius) continue;
					float t = radius == 0 ? 0f : Mathf.Clamp01(Mathf.Sqrt(distanceSquared) / radius);
					float ease = Mathf.Pow(t, 5f);
					byte value = (byte)(ease < .01f ? 255 : (1f - ease) * 255f);
					if (value <= pixels[index].r) continue;
					pixels[index] = new Color32(value, value, value, 255);
					if (value == 255) fullyRevealed[index] = true;
					changed = true;
				}
		}
		revealedIndices.Clear();
		if (changed) { fogTexture.SetPixels32(pixels); fogTexture.Apply(); }
	}
}
