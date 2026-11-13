using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Assets.Scripts.Saves___Serialization.Progression.Achievements;
using UnityEngine;
using UnityEngine.UI;
using Assets.Scripts.Saves___Serialization.Progression.Stats;

public class QuestsCompletedUi : MonoBehaviour
{
	[CompilerGenerated]
	private sealed class _003CAnimateQuests_003Ed__6 : IEnumerator<object>, IEnumerator, IDisposable
	{
		private int _003C_003E1__state;

		private object _003C_003E2__current;

		public List<MyAchievement> achievements;

		public QuestsCompletedUi _003C_003E4__this;

		private int _003Cindex_003E5__2;

		private List<MyAchievement>.Enumerator _003C_003E7__wrap2;

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
		public _003CAnimateQuests_003Ed__6(int _003C_003E1__state)
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
	}

	public GameObject prefab;

	public Transform contentParent;

	public ScrollRect scrollRect;

	public AudioSource source;

	private float delay = .35f;

	private void Start()
	{
		if (RunStats.achievements != null) StartCoroutine(AnimateQuests(RunStats.achievements));
	}

	private IEnumerator AnimateQuests(List<MyAchievement> achievements)
	{
		yield return new WaitForSeconds(.4f);
		int index = 0;
		foreach (var achievement in achievements)
		{
			TestAddItem(achievement);
			if (source != null) { source.pitch = 1f + .5f * index / achievements.Count; source.Play(); }
			yield return new WaitForSeconds(delay);
			index++;
		}
	}

	private void TestAddItem(MyAchievement ach)
	{
		if (prefab == null || contentParent == null) return;
		var item = Instantiate(prefab, contentParent);
		item.SetActive(true);
		var entry = item.GetComponent<QuestsCompletedEntry>();
		if (entry != null) entry.Set(ach);
		Canvas.ForceUpdateCanvases();
		if (scrollRect != null) scrollRect.verticalNormalizedPosition = 0f;
		Canvas.ForceUpdateCanvases();
	}
}
