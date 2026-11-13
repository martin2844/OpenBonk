using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class MapMenu : MonoBehaviour
{
	public Transform mapGridParent;

	public GameObject mapPrefabUi;

	public MyButton b_confirm;

	public BackEscape windowBackEscape;

	private List<MyButtonMap> mapButtons;

	public TextMeshProUGUI t_buttonDisabledText;

	private MyButton selectedButton;

	public MapData selectedMap;

	private void Start()
	{
	}

	private void RefreshArrow()
	{
	}

	private void OnSelectMap(MyButtonMap mapButton)
	{
	}

	private void OnConfirmMap(MyButtonMap mapButton)
	{
	}

	private void Update()
	{
	}

	private void OnDestroy()
	{
	}

	public void StartMap()
	{
	}
}
