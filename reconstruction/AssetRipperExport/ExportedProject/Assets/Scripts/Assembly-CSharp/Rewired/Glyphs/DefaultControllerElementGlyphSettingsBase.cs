#if !OPENBONK_NO_REWIRED || UNITY_EDITOR
using UnityEngine;

namespace Rewired.Glyphs
{
	public abstract class DefaultControllerElementGlyphSettingsBase : MonoBehaviour
	{
		[Tooltip("The Controller element glyph options.")]
		[SerializeField]
		private ControllerElementGlyphSelectorOptions _options;

		[Tooltip("The prefab used for each glyph or text object.")]
		[SerializeField]
		private GameObject _glyphOrTextPrefab;

		public ControllerElementGlyphSelectorOptions options
		{
			get
			{
				return null;
			}
			set
			{
			}
		}

		public GameObject glyphOrTextPrefab
		{
			get
			{
				return null;
			}
			set
			{
			}
		}

		protected virtual void OnEnable()
		{
		}

		protected virtual void OnDisable()
		{
		}

		protected virtual void SetDefaults()
		{
		}

		protected virtual void SetDefaultOptions()
		{
		}

		protected abstract void SetDefaultGlyphOrTextPrefab();
	}
}

#endif
