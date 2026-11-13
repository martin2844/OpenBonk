using System;
using System.Runtime.CompilerServices;
using UnityEngine;

public class UpdatableData : ScriptableObject
{
	public bool autoUpdate;

	public event Action OnValuesUpdated
	{
		[CompilerGenerated]
		add
		{
		}
		[CompilerGenerated]
		remove
		{
		}
	}
}
