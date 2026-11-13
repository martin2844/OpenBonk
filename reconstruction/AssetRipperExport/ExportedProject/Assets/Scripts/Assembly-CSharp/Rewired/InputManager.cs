#if !OPENBONK_NO_REWIRED || UNITY_EDITOR
using System.ComponentModel;
using Rewired.Utils.Interfaces;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Rewired
{
	[AddComponentMenu("Rewired/Input Manager")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public sealed class InputManager : InputManager_Base
	{
		private bool ignoreRecompile;

		protected override void OnInitialized()
		{
		}

		protected override void OnDeinitialized()
		{
		}

		protected override void DetectPlatform()
		{
		}

		protected override void CheckRecompile()
		{
		}

		protected override IExternalTools GetExternalTools()
		{
			return null;
		}

		private bool CheckDeviceName(string searchPattern, string deviceName, string deviceModel)
		{
			return false;
		}

		private void SubscribeEvents()
		{
		}

		private void UnsubscribeEvents()
		{
		}

		private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
		{
		}
	}
}

#endif
