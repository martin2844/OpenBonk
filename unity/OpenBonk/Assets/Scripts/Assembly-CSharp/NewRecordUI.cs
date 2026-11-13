using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NewRecordUI : MonoBehaviour
{
	[CompilerGenerated]
	private sealed class _003CShowNewItem_003Ed__20 : IEnumerator<object>, IEnumerator, IDisposable
	{
		private int _003C_003E1__state;

		private object _003C_003E2__current;

		public NewRecordUI _003C_003E4__this;

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
		public _003CShowNewItem_003Ed__20(int _003C_003E1__state)
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

	public GameObject content;

	public RawImage background;

	public RawImage itemDisplay;

	public TextMeshProUGUI itemNameText;

	public TextMeshProUGUI extraText;

	public TextMeshProUGUI t_score;

	public ParticleSystem ps;

	public UiAnimation buttonAnimation;

	private float fadeInTime;

	private float fadeOutTime;

	private float cardDesiredScale;

	private float textDesiredScale;

	private float desiredAlpha;

	private float yRotation;

	private float animatorTime;

	private float animatorSpeed;

	public AudioSource sfx;

	private void Start()
	{
	}

	private void Update()
	{
	}

	private void Animate()
	{
	}

	[IteratorStateMachine(typeof(_003CShowNewItem_003Ed__20))]
	private IEnumerator ShowNewItem()
	{
		return null;
	}
}
