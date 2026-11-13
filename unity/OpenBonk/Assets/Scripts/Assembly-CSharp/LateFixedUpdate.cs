using System;
using UnityEngine;

public class LateFixedUpdate : MonoBehaviour
{
	public static Action A_LateUpdate;

	private void FixedUpdate()
	{
		A_LateUpdate?.Invoke();
	}
}
