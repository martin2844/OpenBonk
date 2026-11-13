using Assets.Scripts.Game.MapGeneration;
using Assets.Scripts.Game.Spawning.New.Timelines;
using Assets.Scripts.MapGeneration.ProceduralTiles;
using UnityEngine;
using UnityEngine.Localization;

[CreateAssetMenu(menuName = "Me/Mapping/StageData")]
public class StageData : ScriptableObject
{
	[Header("Meta")]
	public LocalizedString localizedName;

	[Header("Meta")]
	public LocalizedString localizedDescription;

	[Header("Visuals")]
	public MapEdgeFillType mapEdgeFillType;

	public Material waterMaterial;

	public GameObject waterSplashFx;

	[Header("Grass")]
	public Material grassMaterial;

	public int grassPerChunk;

	[Header("TileTextures")]
	public Material[] flatMaterials;

	public Material m_fillMiddle;

	public Material m_fillTop;

	public Material m_fillMiddleEdge;

	public Material m_fillTopEdge;

	public Material m_stairs;

	public Material triplanarMaterial;

	public GameObject particles;

	[Header("Fog And Sky")]
	public Material skybox;

	public float fogIntensity;

	public Color fogColor;

	[Header("Lighting")]
	public Color ambienceColor;

	public Color lightColor;

	public float lightIntensity;

	[Header("Gameplay")]
	public StageTimeline stageTimeline;

	public bool isWaterDamage;

	[Header("Map Generation")]
	public MapParameters mapParameters;

	public RandomMapObject[] randomMapObjects;

	public StageTilePrefabs stageTilePrefabs;

	[Header("Procedural Mesh Data")]
	public TerrainData proceduralTerrainData;

	public NoiseData proceduralNoiseData;

	public float proceduralMapScale;

	public float farClipPlane;

	[Header("Challenges")]
	public ChallengeData[] challenges;

	public string GetName()
	{
		return localizedName.GetLocalizedString();
	}

	public string GetDescription()
	{
		return localizedDescription.GetLocalizedString();
	}

	public Material GetSideMaterial(EFillType eFillType, bool useEdgeTextures = false)
	{
		bool top = (((int)eFillType | 2) == 3);
		return top ? (useEdgeTextures ? m_fillTopEdge : m_fillTop)
			: (useEdgeTextures ? m_fillMiddleEdge : m_fillMiddle);
	}

	public Material GetTopMaterial()
	{
		return flatMaterials[Random.Range(0, flatMaterials.Length)];
	}

	public void ApplyFogAndSky(Light sunLight)
	{
		RenderSettings.skybox = skybox;
		RenderSettings.ambientLight = ambienceColor;
		if (fogIntensity > 0f)
		{
			RenderSettings.fog = true;
			RenderSettings.fogDensity = fogIntensity;
			RenderSettings.fogColor = fogColor;
		}
		else RenderSettings.fog = false;
		sunLight.color = lightColor;
		sunLight.intensity = lightIntensity;
	}

	public GameObject SpawnParticles()
	{
		return particles != null ? Instantiate(particles) : null;
	}
}
