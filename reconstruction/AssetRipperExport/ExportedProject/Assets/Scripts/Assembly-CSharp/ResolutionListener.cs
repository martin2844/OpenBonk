using System;
using TMPro;
using UnityEngine;

public class ResolutionListener : MonoBehaviour
{
	private float timeout;

	public TextMeshProUGUI timerText;

	private Action<int> revertAction;

	private Action<int> acceptAction;

	private int oldValue;

	private int newValue;

	public GameObject content;

	public static ResolutionListener Instance;

	private void Awake()
	{
	}

	public void NewResolution(int newResolution, int oldResolution, Action<int> revert, Action<int> accept)
	{
	}

	private void Update()
	{
	}

	public void Response(bool r)
	{
	}

	private bool IsActive()
	{
		return false;
	}
}
