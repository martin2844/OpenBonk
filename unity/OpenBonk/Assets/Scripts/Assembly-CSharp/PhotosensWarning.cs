using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using UnityEngine;
using Assets.Scripts.Managers;
using UnityEngine.UIElements.Experimental;

public class PhotosensWarning : MonoBehaviour
{
	[CompilerGenerated]
	private sealed class _003CShowWarning_003Ed__6 : IEnumerator<object>, IEnumerator, IDisposable
	{
		private int _003C_003E1__state;

		private object _003C_003E2__current;

		public PhotosensWarning _003C_003E4__this;

		private float _003Ct_003E5__2;

		private float _003CfadeOverTime_003E5__3;

		object IEnumerator<object>.Current
		{
			[DebuggerHidden]
			get
			{
				return null;
			}
		}

		object IEnumerator.Current
		{
			[DebuggerHidden]
			get
			{
				return null;
			}
		}

		[DebuggerHidden]
		public _003CShowWarning_003Ed__6(int _003C_003E1__state)
		{
		}

		[DebuggerHidden]
		void IDisposable.Dispose()
		{
		}

		private bool MoveNext()
		{
			return false;
		}

		bool IEnumerator.MoveNext()
		{
			//ILSpy generated this explicit interface implementation from .override directive in MoveNext
			return this.MoveNext();
		}

		[DebuggerHidden]
		void IEnumerator.Reset()
		{
		}
	}

	public GameObject window;

	public CanvasGroup cg;

	public MyButton btn;

	private void Start()
	{
		if (SaveManager.loaded) OnSavesLoaded();
		SaveManager.A_SavesLoaded += OnSavesLoaded;
	}

	private void OnDestroy()
	{
		SaveManager.A_SavesLoaded -= OnSavesLoaded;
	}

	private void OnSavesLoaded()
	{
		var settings = SaveManager.Instance?.config?.otherSettings;
		if (settings != null && !settings.hasAcceptedPhotoSensitivity) StartCoroutine(ShowWarning());
	}

	[IteratorStateMachine(typeof(_003CShowWarning_003Ed__6))]
	private IEnumerator ShowWarning()
	{
		yield return new WaitForSeconds(.5f);
		ButtonManager.SetNull();
		if (AlwaysUi.Instance != null && AlwaysUi.Instance.selectionArrow != null) AlwaysUi.Instance.selectionArrow.Hide();
		if (window == null || cg == null) yield break;
		window.SetActive(true);
		cg.alpha = 0f;
		float elapsed = 0f;
		const float duration = .55f;
		while (elapsed < duration)
		{
			elapsed += Time.unscaledDeltaTime;
			cg.alpha = Easing.InOutQuad(Mathf.Clamp01(elapsed / duration));
			yield return null;
		}
		yield return new WaitForSeconds(.8f);
		if (btn != null) { btn.gameObject.SetActive(true); ButtonManager.ForceHoverButton(btn); }
	}

	public void Confirm()
	{
		var save = SaveManager.Instance;
		if (save != null && save.config != null && save.config.otherSettings != null)
		{
			save.config.otherSettings.hasAcceptedPhotoSensitivity = true;
#if UNITY_ANDROID && !UNITY_EDITOR
			save.SaveConfig();
#endif
		}
		if (window != null) window.SetActive(false);
	}
}
