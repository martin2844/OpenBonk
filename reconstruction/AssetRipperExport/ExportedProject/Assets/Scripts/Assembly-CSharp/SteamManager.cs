using System;
using Steamworks;
using UnityEngine;

public class SteamManager : MonoBehaviour
{
	protected static bool initialized;

	protected static SteamManager Instance;

	public static AppId_t APP_ID;

	public static Action A_UpdateComponents;

	public static Action A_Initialized;

	public static ulong steamId;

	public static Action<ulong> A_PlayerInformationArrived;

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void InitOnPlayMode()
	{
	}

	public virtual void Load()
	{
		// The game's Steamworks.NET loader calls into Steam's native library here.
		// Android builds do not include Steam, so keep the feature unavailable and
		// let the game continue with its local-save/offline paths.
		if (Application.platform == RuntimePlatform.Android)
		{
			initialized = false;
			steamId = 0UL;
			return;
		}
	}

	protected virtual void OnDestroy()
	{
	}

	private void InitComponents()
	{
	}

	private void DestroyComponents()
	{
	}

	private void OnPersonaStateChange(PersonaStateChange_t param)
	{
	}

	protected virtual void Update()
	{
	}

	public static bool IsInitialized()
	{
		return initialized;
	}
}
