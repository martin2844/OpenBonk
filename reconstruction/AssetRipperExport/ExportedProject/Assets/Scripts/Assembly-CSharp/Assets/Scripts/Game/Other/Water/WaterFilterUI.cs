using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.Game.Other.Water
{
	public class WaterFilterUI : MonoBehaviour
	{
		public CanvasGroup group;

		public RawImage i_color;

		private bool usingFilter;

		private float fadeSeconds = 0.06f;

		private void Awake()
		{
			global::Water.A_CameraEnterWater += OnFilterEnter;
			global::Water.A_CameraExitWater += OnFilterExit;
		}

		private void OnDestroy()
		{
			global::Water.A_CameraEnterWater -= OnFilterEnter;
			global::Water.A_CameraExitWater -= OnFilterExit;
		}

		private void OnFilterEnter(global::Water filter)
		{
			Color tint = filter.color;
			i_color.color = new Color(tint.r * 0.4f, tint.g * 0.4f, tint.b * 0.4f, 0.6f);
			usingFilter = true;
		}

		private void OnFilterExit(global::Water filter)
		{
			usingFilter = false;
		}

		private void Update()
		{
			if (usingFilter && group.alpha < 1f) group.alpha += Time.deltaTime / fadeSeconds;
			else if (!usingFilter && group.alpha > 0f) group.alpha -= Time.deltaTime / fadeSeconds;
		}
	}
}
