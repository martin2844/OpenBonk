using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace RetroArsenal
{
	public class RetroTarget : MonoBehaviour
	{
		[CompilerGenerated]
		private sealed class _003CRespawn_003Ed__14 : IEnumerator<object>, IEnumerator, IDisposable
		{
			private int _003C_003E1__state;

			private object _003C_003E2__current;

			public RetroTarget _003C_003E4__this;

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
			public _003CRespawn_003Ed__14(int _003C_003E1__state)
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
		private sealed class _003CSquashAndStretch_003Ed__16 : IEnumerator<object>, IEnumerator, IDisposable
		{
			private int _003C_003E1__state;

			private object _003C_003E2__current;

			public RetroTarget _003C_003E4__this;

			private float _003CtimeElapsed_003E5__2;

			private Vector3 _003CstartScale_003E5__3;

			private Vector3 _003CendScale_003E5__4;

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
			public _003CSquashAndStretch_003Ed__16(int _003C_003E1__state)
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

		public TargetEffects effects;

		[Header("General Settings")]
		public int hitsToDestroy;

		public float respawnTime;

		[Header("Squash & Stretch")]
		public bool enableSquashAndStretch;

		public float duration;

		public Vector3 squashScale;

		public Vector3 stretchScale;

		private Renderer targetRenderer;

		private Collider targetCollider;

		private AudioSource audioSource;

		private int currentHits;

		private Vector3 originalScale;

		private void Start()
		{
		}

		private void SpawnTarget()
		{
		}

		[IteratorStateMachine(typeof(_003CRespawn_003Ed__14))]
		private IEnumerator Respawn()
		{
			return null;
		}

		public void OnHit()
		{
		}

		[IteratorStateMachine(typeof(_003CSquashAndStretch_003Ed__16))]
		private IEnumerator SquashAndStretch()
		{
			return null;
		}

		private void DestroyTarget()
		{
		}
	}
}
