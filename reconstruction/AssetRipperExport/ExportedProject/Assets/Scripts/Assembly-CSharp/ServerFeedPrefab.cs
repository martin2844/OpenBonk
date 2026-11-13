using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ServerFeedPrefab : MonoBehaviour
{
	[CompilerGenerated]
	private sealed class _003CShow_003Ed__9 : IEnumerator<object>, IEnumerator, IDisposable
	{
		private int _003C_003E1__state;

		private object _003C_003E2__current;

		public ServerFeedPrefab _003C_003E4__this;

		private float _003Ctimer_003E5__2;

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
		public _003CShow_003Ed__9(int _003C_003E1__state)
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

	public CanvasGroup canvasGroup;

	public RawImage i_icon;

	public TextMeshProUGUI t_info;

	private float currentTime;

	private float fadeTime = .5f;

	private float startFadeTime = 8.5f;

	private float destroyTime;

	private Action<ServerFeedPrefab> timeoutAction;

	public void SetFeed(string f, float duration, Action<ServerFeedPrefab> timeoutAction, Texture icon = null)
	{
		this.timeoutAction = timeoutAction;
		t_info.gameObject.SetActive(true);
		t_info.text = f;
		i_icon.enabled = icon != null;
		if (icon != null) i_icon.texture = icon;
		currentTime = 0f;
		startFadeTime = duration;
		StartCoroutine(Show());
	}

	[IteratorStateMachine(typeof(_003CShow_003Ed__9))]
	private IEnumerator Show()
	{
		float timer = 0f;
		canvasGroup.alpha = 0f;
		while (timer < 1f)
		{
			timer += Time.deltaTime * 2f;
			canvasGroup.alpha = Mathf.Clamp01(timer);
			yield return null;
		}
		yield return new WaitForSeconds(startFadeTime);
		timer = 0f;
		canvasGroup.alpha = 1f;
		while (timer < 1f)
		{
			timer += Time.deltaTime * 2f;
			canvasGroup.alpha = 1f - Mathf.Clamp01(timer);
			yield return null;
		}
		timeoutAction?.Invoke(this);
	}
}
