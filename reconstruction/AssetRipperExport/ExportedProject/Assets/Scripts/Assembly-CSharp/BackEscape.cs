using UnityEngine;

public class BackEscape : MonoBehaviour
{
	public new bool enabled = true;

	private void Update()
	{
		if (!enabled || AndroidControllerBindings.IsListening || (KeyListener.Instance != null && KeyListener.Instance.IsListening()) || !MyInputManager.GetButtonDown(MyInputManager.UICancel)) return;
		var button = GetComponent<UnityEngine.UI.Button>();
		if (button != null && button.enabled && button.interactable)
		{
#if UNITY_ANDROID && !UNITY_EDITOR
			if (AndroidGameplayInputProbe.Active) Debug.Log($"Gameplay input probe back action: object={gameObject.name}, frame={Time.frameCount}, start={Input.GetKeyDown(KeyCode.JoystickButton10)}, escape={Input.GetKeyDown(KeyCode.Escape)}");
#endif
			button.onClick.Invoke();
		}
	}
}
