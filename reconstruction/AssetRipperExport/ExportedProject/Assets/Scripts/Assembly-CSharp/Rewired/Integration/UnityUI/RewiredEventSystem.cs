#if !OPENBONK_NO_REWIRED || UNITY_EDITOR
using UnityEngine;
using UnityEngine.EventSystems;

namespace Rewired.Integration.UnityUI
{
	[AddComponentMenu("Rewired/Rewired Event System")]
	public class RewiredEventSystem : EventSystem
	{
		[Tooltip("If enabled, the Event System will be updated every frame even if other Event Systems are enabled. Otherwise, only EventSystem.current will be updated.")]
		[SerializeField]
		private bool _alwaysUpdate;

		public bool alwaysUpdate
		{
			get
			{
				return false;
			}
			set
			{
			}
		}

		protected override void Update()
		{
		}
	}
}

#endif
