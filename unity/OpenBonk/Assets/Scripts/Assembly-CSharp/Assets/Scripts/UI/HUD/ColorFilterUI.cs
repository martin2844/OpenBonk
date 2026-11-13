using Assets.Scripts.Objects;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements.Experimental;

namespace Assets.Scripts.UI.HUD
{
	public class ColorFilterUI : MonoBehaviour
	{
		public CanvasGroup group;

		public RawImage i_color;

		private bool usingFilter;

		private float fadeSeconds = 0.1f;

		private float interpValue;

		public void ResetFilter()
		{
			usingFilter = false;
		}

		private void Awake()
		{
			ColorFilter.A_FilterEnter += OnFilterEnter;
			ColorFilter.A_FilterExit += OnFilterExit;
		}

		private void OnDestroy()
		{
			ColorFilter.A_FilterEnter -= OnFilterEnter;
			ColorFilter.A_FilterExit -= OnFilterExit;
		}

		private void OnFilterEnter(ColorFilter filter)
		{
			Color tint = filter.color;
			i_color.color = new Color(tint.r * 0.5f, tint.g * 0.5f, tint.b * 0.5f, 0.6f);
			usingFilter = true;
		}

		private void OnFilterExit(ColorFilter filter)
		{
			usingFilter = false;
		}

		private void Update()
		{
			if (usingFilter && group.alpha < 1f)
			{
				interpValue = Mathf.Min(1f, interpValue + Time.deltaTime / fadeSeconds);
				group.alpha = Easing.InOutQuad(interpValue);
			}
			else if (!usingFilter && group.alpha > 0f)
			{
				interpValue = Mathf.Max(0f, interpValue - Time.deltaTime / fadeSeconds);
				group.alpha = Easing.InOutQuad(interpValue);
			}
		}
	}
}
