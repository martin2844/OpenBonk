using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class MapSelectionWindow : MonoBehaviour
{
	public GameObject mapEntryPrefab;

	public SelectionGroupToggleSingle selectionGroup;

	private bool initialized;

	public void InitButtons()
	{
		if (initialized || mapEntryPrefab == null || DataManager.Instance == null || DataManager.Instance.maps == null) return;
		initialized = true;
		var maps = SortMaps(DataManager.Instance.maps);
		var mapButtons = new System.Collections.Generic.List<Button>();
		for (int i = 0; i < maps.Count; i++)
		{
			GameObject entryObject = i == 0 ? mapEntryPrefab : Instantiate(mapEntryPrefab, mapEntryPrefab.transform.parent);
			MapEntry entry = entryObject.GetComponent<MapEntry>();
			if (entry != null) entry.Set(maps[i]);
			Button button = entryObject.GetComponent<Button>();
			if (button != null) mapButtons.Add(button);
		}
		// The template has explicit navigation. Instantiated copies inherit its
		// empty down link, so they must be connected after the entries exist.
		for (int i = 0; i < mapButtons.Count; i++)
		{
			Navigation nav = mapButtons[i].navigation;
			nav.mode = Navigation.Mode.Explicit;
			if (i > 0) nav.selectOnUp = mapButtons[i - 1];
			if (i + 1 < mapButtons.Count) nav.selectOnDown = mapButtons[i + 1];
			mapButtons[i].navigation = nav;
		}
		if (selectionGroup != null) selectionGroup.FindButtons();
	}

	public static List<MapData> SortMaps(IEnumerable<MapData> maps)
	{
		return maps.Where(map => map != null)
			.OrderBy(map => map is OpenBonkExternalMapData ? 1 : 0)
			.ThenBy(map => map is OpenBonkExternalMapData ? 0 : map.unlockOrder)
			.ThenBy(map => (map as OpenBonkExternalMapData)?.displayName ?? "", StringComparer.OrdinalIgnoreCase)
			.ThenBy(map => (map as OpenBonkExternalMapData)?.externalId ?? "", StringComparer.Ordinal)
			.ToList();
	}
}
