#if !OPENBONK_NO_REWIRED || UNITY_EDITOR
using Rewired.Interfaces;
using UnityEngine;

namespace Rewired.Localization
{
	public abstract class LocalizedStringProviderBase : MonoBehaviour, ILocalizedStringProvider
	{
		[SerializeField]
		[Tooltip("Determines if localized strings should be fetched immediately in bulk when available. If false, strings will be fetched when queried.")]
		private bool _prefetch;

		public virtual bool prefetch
		{
			get
			{
				return false;
			}
			set
			{
			}
		}

		protected abstract bool initialized { get; }

		protected virtual void OnEnable()
		{
		}

		protected virtual void OnDisable()
		{
		}

		protected virtual void Update()
		{
		}

		protected virtual void TrySetLocalizedStringProvider()
		{
		}

		protected abstract bool Initialize();

		public virtual void Reload()
		{
		}

		protected abstract bool TryGetLocalizedString(string key, out string result);

		bool ILocalizedStringProvider.TryGetLocalizedString(string key, out string result)
		{
			result = null;
			return false;
		}
	}
}

#endif
