using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MapPrefabUi : MonoBehaviour, ISelectHandler, IEventSystemHandler
{
	public RawImage i_map;

	public TextMeshProUGUI t_name;

	private MapData mapData;

	public static Action<MapData> A_SelectMap;

	public void SetMap(MapData mapData)
	{
	}

	public void OnSelect(BaseEventData eventData)
	{
	}
}
