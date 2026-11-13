using System;
using System.Runtime.CompilerServices;
using UnityEngine;

public class UpdateableData : ScriptableObject
{
	public bool autoUpdate;

	public event Action OnValuesUpdate
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
