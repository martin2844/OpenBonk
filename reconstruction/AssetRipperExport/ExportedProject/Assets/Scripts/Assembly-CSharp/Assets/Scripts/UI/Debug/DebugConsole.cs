using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.UI.Debug
{
	public class DebugConsole : MonoBehaviour
	{
		public TextMeshProUGUI textField;

		public TextMeshProUGUI suggestionText;

		public TextMeshProUGUI inputFieldText;

		public TMP_InputField inputField;

		public ScrollRect scrollRect;

		public Transform suggestionParent;

		public GameObject suggestionPrefab;

		private List<CommandSuggestion> suggestionPrefabs;

		private List<DebugCommandBase> suggestions;

		private int suggestionIndex;

		public List<object> commandList;

		public static DebugConsole Instance;

		private bool isTyping;

		public GameObject consoleTransform;

		private bool oldPauseState;

		private bool movedIndexThisFrame;

		private int nSuggestions;

		private void OnEnable()
		{
		}

		private void Start()
		{
		}

		private void Awake()
		{
			if (Instance != null) { Destroy(gameObject); return; }
			Instance = this;
		}

		private void Update()
		{
		}

		private void CheckToggle()
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

		public bool IsActive()
		{
			return consoleTransform != null && consoleTransform.activeInHierarchy;
		}

		public void Toggle()
		{
		}

		public void HandleInput()
		{
		}

		public void AppendMessage(string msg)
		{
		}

		public void TryAutoComplete()
		{
		}

		private void MoveSelectionIndex(int dir)
		{
		}

		private void DoAutoComplete()
		{
		}
	}
}
