#if !OPENBONK_NO_REWIRED || UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Rewired.Glyphs
{
	[Serializable]
	public class GlyphSetCollection : ScriptableObject
	{
		[CompilerGenerated]
		private sealed class _003CIterateSetsRecursively_003Ed__9 : IEnumerable<GlyphSet>, IEnumerable, IEnumerator<GlyphSet>, IEnumerator, IDisposable
		{
			private int _003C_003E1__state;

			private GlyphSet _003C_003E2__current;

			private int _003C_003El__initialThreadId;

			private List<GlyphSetCollection> processedCollections;

			public List<GlyphSetCollection> _003C_003E3__processedCollections;

			public GlyphSetCollection _003C_003E4__this;

			private int _003CsetCount_003E5__2;

			private int _003CcollectionCount_003E5__3;

			private int _003Ci_003E5__4;

			private IEnumerator<GlyphSet> _003C_003E7__wrap4;

			GlyphSet IEnumerator<GlyphSet>.Current
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
			public _003CIterateSetsRecursively_003Ed__9(int _003C_003E1__state)
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

			private void _003C_003Em__Finally1()
			{
			}

			[DebuggerHidden]
			void IEnumerator.Reset()
			{
			}

			[DebuggerHidden]
			IEnumerator<GlyphSet> IEnumerable<GlyphSet>.GetEnumerator()
			{
				return null;
			}

			[DebuggerHidden]
			IEnumerator IEnumerable.GetEnumerator()
			{
				return null;
			}
		}

		[Tooltip("The list of glyph sets.")]
		[SerializeField]
		private List<GlyphSet> _sets;

		[Tooltip("The list of glyph set collections.")]
		[SerializeField]
		private List<GlyphSetCollection> _collections;

		public List<GlyphSet> sets
		{
			get
			{
				return null;
			}
			set
			{
			}
		}

		public List<GlyphSetCollection> collections
		{
			get
			{
				return null;
			}
			set
			{
			}
		}

		public virtual IEnumerable<GlyphSet> IterateSetsRecursively()
		{
			return null;
		}

		[IteratorStateMachine(typeof(_003CIterateSetsRecursively_003Ed__9))]
		protected virtual IEnumerable<GlyphSet> IterateSetsRecursively(List<GlyphSetCollection> processedCollections)
		{
			return null;
		}

		private static void LogCircularDependency()
		{
		}
	}
}

#endif
