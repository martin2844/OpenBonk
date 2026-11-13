using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TransitionUI : MonoBehaviour
{
	[CompilerGenerated]
	private sealed class _003CDoTransition_003Ed__13 : IEnumerator<object>, IEnumerator, IDisposable
	{
		private int _003C_003E1__state;

		private object _003C_003E2__current;

		public TransitionUI _003C_003E4__this;

		public float newTransitionTime;

		public float delay;

		public Action action;

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
		public _003CDoTransition_003Ed__13(int _003C_003E1__state)
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

	[CompilerGenerated]
	private sealed class _003CEndTransition_003Ed__15 : IEnumerator<object>, IEnumerator, IDisposable
	{
		private int _003C_003E1__state;

		private object _003C_003E2__current;

		public TransitionUI _003C_003E4__this;

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
		public _003CEndTransition_003Ed__15(int _003C_003E1__state)
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

	public RawImage overlay;

	private float transitionTime = 0.5f;

	public static TransitionUI Instance;

	public bool isTransitioning;

	public static Action A_transitionEnd;

	public static Action A_transitionStart;

	public static Action A_MapTransitionStart;

	private string sceneMainMenuName = "MainMenu";

	private float fadeInTime = 1f;

	private void Awake()
	{
		if (Instance != null && Instance != this) { Destroy(gameObject); return; }
		Instance = this;
		if (overlay != null) { overlay.enabled = true; overlay.CrossFadeAlpha(0f, 0f, true); }
		SceneManager.sceneLoaded += OnNewSceneLoaded;
	}

	private void OnDestroy()
	{
		SceneManager.sceneLoaded -= OnNewSceneLoaded;
		if (Instance == this) Instance = null;
	}

	private void Start()
	{
		if (LoadingScreen.isLoading || Instance != this) return;
		if (overlay != null) overlay.CrossFadeAlpha(1f, 0f, true);
		StartCoroutine(EndTransition());
	}

	public void LoadMenu()
	{
		StartLoadingMap(sceneMainMenuName);
	}

	public void StartLoadingMap(string mapName)
	{
		StartTransition(() => SceneManager.LoadScene(mapName), transitionTime, 0.5f);
		A_MapTransitionStart?.Invoke();
	}

	public void StartTransition(Action action, float transitionTime = 0.5f, float delay = 0.5f)
	{
		StartCoroutine(DoTransition(action, transitionTime, delay));
	}

	private IEnumerator DoTransition(Action action, float newTransitionTime, float delay = 0.5f)
	{
		A_transitionStart?.Invoke();
		isTransitioning = true;
		if (overlay != null) overlay.CrossFadeAlpha(1f, newTransitionTime, false);
		yield return new WaitForSeconds(newTransitionTime);
		yield return new WaitForSeconds(delay);
		if (isTransitioning) action?.Invoke();
		isTransitioning = false;
		yield return null;
		yield return EndTransition();
	}

	private IEnumerator EndTransition()
	{
		if (overlay != null) overlay.CrossFadeAlpha(0f, fadeInTime, false);
		yield return new WaitForSeconds(fadeInTime);
		A_transitionEnd?.Invoke();
	}

	public void StopTransition()
	{
		isTransitioning = false;
	}

	public bool IsTransitioning()
	{
		return isTransitioning;
	}

	private void OnNewSceneLoaded(Scene arg0, LoadSceneMode arg1)
	{
	}

	public float GetTransitionTime()
	{
		return transitionTime;
	}
}
