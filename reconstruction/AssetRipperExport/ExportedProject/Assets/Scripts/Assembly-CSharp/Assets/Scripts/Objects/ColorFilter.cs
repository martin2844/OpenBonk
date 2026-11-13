using System;
using UnityEngine;

namespace Assets.Scripts.Objects
{
	public class ColorFilter : MonoBehaviour
	{
		public Color color;

		public static Action<ColorFilter> A_FilterEnter;

		public static Action<ColorFilter> A_FilterExit;

		private void OnTriggerEnter(Collider c)
		{
			if (c.gameObject.CompareTag("MainCamera")) A_FilterEnter?.Invoke(this);
		}

		private void OnTriggerExit(Collider c)
		{
			if (c.gameObject.CompareTag("MainCamera")) A_FilterExit?.Invoke(this);
		}
	}
}
