using Assets.Scripts.Game.MapGeneration.MapEvents;
using Assets.Scripts.Managers;
using Assets.Scripts._Data.MapsAndStages;
using UnityEngine;

public class MapEventsManager : MonoBehaviour
{
	private MapEvents mapEvents;

	private void Start()
	{
		if (HasDesertEvents(MapController.currentMap)) mapEvents = new MapEventsDesert();
		mapEvents?.Init();
	}

	public static bool HasDesertEvents(MapData map) => map != null &&
		(map.eMap == EMap.Desert || (map is OpenBonkExternalMapData external && external.enemyPoolMap == EMap.Desert));

	private void OnDestroy()
	{
		mapEvents?.Cleanup();
	}

	private void FixedUpdate()
	{
		mapEvents?.Tick();
	}
}
