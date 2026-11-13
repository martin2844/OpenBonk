using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Chatbox : MonoBehaviour
{
	public enum EChatUserType
	{
		Game = 0,
		Server = 1,
		Player = 2,
		OtherPlayerStatus = 3,
		Lobby = 4
	}

	public CanvasGroup overlay;

	public bool isTyping;

	private bool cooldown;

	public TextMeshProUGUI messages;

	public TMP_InputField inputField;

	public ScrollRect ScrollRect;

	private float chatFadeTimer;

	private float chatFadeTime;

	public static Action<ulong, string, string> A_NewMessage;

	private static string storedChat;

	public static Chatbox Instance;

	private const int chatTokenMax = 5;

	private const float chatTokenAddInterval = 1.5f;

	private float chatSentAtTime;

	private float currentTokens;

	private void Awake()
	{
	}

	private void OnDestroy()
	{
	}

	private void Update()
	{
	}

	private void CheckFade()
	{
	}

	private void CheckInput()
	{
	}

	private void StartTyping()
	{
	}

	private void StopTyping()
	{
	}

	private void SendMessage()
	{
	}

	public void AppendMessage(ulong fromUser, string msg)
	{
	}

	public bool TrySendMessage()
	{
		return false;
	}

	public void AppendMessage(EChatUserType fromUser, string msg)
	{
	}

	private void Cooldown()
	{
	}

	public static bool IsTyping()
	{
		return false;
	}
}
