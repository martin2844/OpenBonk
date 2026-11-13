using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using UnityEngine;

public class LanguageStartup : MonoBehaviour
{
	[CompilerGenerated]
	private sealed class _003CSetLanguageCoroutine_003Ed__3 : IEnumerator<object>, IEnumerator, IDisposable
	{
		private int _003C_003E1__state;

		private object _003C_003E2__current;

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
		public _003CSetLanguageCoroutine_003Ed__3(int _003C_003E1__state)
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

	private void Awake()
	{
	}

	private void OnDestroy()
	{
	}

	private void OnSavesLoaded()
	{
	}

	[IteratorStateMachine(typeof(_003CSetLanguageCoroutine_003Ed__3))]
	private IEnumerator SetLanguageCoroutine()
	{
		return null;
	}

	private static void CheckSteamLanguage()
	{
	}

	public static void SetSystemLanguage()
	{
	}

	private static void SetLocale(string loc)
	{
	}

	private static string MapSteamLangToLocale(string steamLang)
	{
		return null;
	}

	private static string MapSystemLangToLocale(SystemLanguage lang)
	{
		return null;
	}
}
