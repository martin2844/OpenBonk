using System;
using UnityEngine;

namespace Assets.Scripts.UI.InGame.FullMap
{
	public class FullMapUi : MonoBehaviour
	{
		public static Action<bool> A_Toggle;

		private void OnEnable()
		{
			A_Toggle?.Invoke(true);
		}

		private void OnDisable()
		{
			A_Toggle?.Invoke(false);
		}
	}
}
