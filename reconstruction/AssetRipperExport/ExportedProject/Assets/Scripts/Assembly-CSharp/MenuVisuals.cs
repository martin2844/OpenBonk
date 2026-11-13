using Assets.Scripts.Game.Other;
using Assets.Scripts._Data.MapsAndStages;
using UnityEngine;

public class MenuVisuals : MonoBehaviour
{
	public Terrain terrain;

	public GrassChunkManager grassRenderer;

	public StageData defaultStageVisuals;

	public MapData defaultMapVisuals;

	public AudioClip dungeonAmbience;

	public GameObject general;

	public GameObject dungeon;

	private StageData currentStageData;

	public Transform camera;

	public GameObject particles;

	public GameObject forestBg;

	public GameObject desertBg;

	public Light sunLight;

	public AudioSource ambience;

	private void Awake()
	{
		MapSelectionUi.A_RunConfigChanged += OnRunConfigChanged;
	}

	private void OnDestroy()
	{
		MapSelectionUi.A_RunConfigChanged -= OnRunConfigChanged;
	}

	private void OnRunConfigChanged(RunConfig runConfig)
	{
		if (runConfig == null || runConfig.mapData == null || currentStageData == runConfig.stageData) return;
		Set(runConfig.mapData, runConfig.stageData, runConfig.mapData.eMap);
	}

	private void Start()
	{
		Set(defaultMapVisuals, defaultStageVisuals, EMap.Forest);
		Invoke(nameof(StartDelayed), 0.25f);
	}

	private void StartDelayed()
	{
		Set(defaultMapVisuals, defaultStageVisuals, EMap.Forest);
	}

	public void Set(MapData mapData, StageData stageData, EMap map)
	{
		if (stageData == null || mapData == null) return;
		if (particles != null) Destroy(particles);
		if (terrain != null) terrain.materialTemplate = stageData.triplanarMaterial;
		if (sunLight != null) stageData.ApplyFogAndSky(sunLight);
		RenderSettings.fogDensity = 0.007f;
		particles = stageData.SpawnParticles();
		if (particles != null && camera != null)
		{
			particles.transform.position = camera.position;
			particles.transform.SetParent(camera);
		}
		if (grassRenderer != null && stageData.grassMaterial != null && stageData.grassPerChunk > 0)
		{
			grassRenderer.Set(stageData.grassMaterial, stageData.grassPerChunk);
			grassRenderer.gameObject.SetActive(true);
		}
		if (ambience != null)
		{
			ambience.clip = mapData.eMap == EMap.Graveyard ? dungeonAmbience : mapData.ambience;
			ambience.loop = true;
			ambience.Play();
		}
		currentStageData = stageData;
		if (forestBg != null) forestBg.SetActive(map == EMap.Forest);
		if (desertBg != null) desertBg.SetActive(map == EMap.Desert);
		if (map == EMap.Desert) RenderSettings.fogDensity = 0.0048f;
		if (dungeon != null) dungeon.SetActive(map == EMap.Graveyard);
		if (general != null) general.SetActive(map != EMap.Graveyard);
		if (ambience != null) ambience.volume = map == EMap.Graveyard ? 0.45f : 0.3f;
	}
}
