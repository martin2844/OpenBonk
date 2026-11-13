#if !OPENBONK_NO_REWIRED || UNITY_EDITOR
using System;
using UnityEngine;

namespace Rewired.Glyphs.UnityUI
{
	[AddComponentMenu("Rewired/Glyphs/Unity UI/Unity UI Player Controller Element Glyph")]
	public class UnityUIPlayerControllerElementGlyph : UnityUIPlayerControllerElementGlyphBase
	{
		[Tooltip("The Player id.")]
		[SerializeField]
		private int _playerId;

		[Tooltip("The Action name.")]
		[SerializeField]
		private string _actionName;

		[NonSerialized]
		private int _actionId;

		[NonSerialized]
		private bool _actionIdCached;

		public override int playerId
		{
			get
			{
				return 0;
			}
			set
			{
			}
		}

		public override int actionId
		{
			get
			{
				return 0;
			}
			set
			{
			}
		}

		public string actionName
		{
			get
			{
				return null;
			}
			set
			{
			}
		}

		private void CacheActionId()
		{
		}
	}
}

#endif
