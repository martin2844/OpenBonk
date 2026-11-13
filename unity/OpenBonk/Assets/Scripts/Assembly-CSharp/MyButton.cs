using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public abstract class MyButton : MonoBehaviour, ISelectHandler, IEventSystemHandler
{
	public enum EButtonState
	{
		Active = 0,
		Inactive = 1,
		InactiveWithSelection = 2
	}

	protected bool isHovering;

	public Transform scaleOnHover;

	protected Button button;

	public float hoverScale;

	private EButtonState state;

	public GameObject disabledOverlay;

	public AudioClip customSfx;

	protected float selectedAtTime;

	protected void Awake()
	{
		Button uiButton = GetButton();
		if (uiButton == null) return;
		uiButton.onClick.AddListener(OnClick);
		uiButton.onClick.AddListener(PlaySfx);
	}

	public void SetFocus(bool focus)
	{
		Button uiButton = GetButton();
		if (uiButton == null) return;
		uiButton.enabled = focus;
		uiButton.interactable = focus;
		if (focus) RefreshState();
	}

	public void SetInteractable(bool interactable)
	{
		state = interactable ? EButtonState.Active : EButtonState.Inactive;
		RefreshState();
	}

	public void SetInteractableButKeepSelectionOn(bool interactable)
	{
		state = interactable ? EButtonState.Active : EButtonState.InactiveWithSelection;
		RefreshState();
	}

	private void RefreshState()
	{
		Button uiButton = GetButton();
		if (uiButton == null) return;
		uiButton.enabled = state != EButtonState.Inactive;
		uiButton.interactable = state == EButtonState.Active;
		if (disabledOverlay != null) disabledOverlay.SetActive(state != EButtonState.Active);
	}

	public void SetDisabledOverlayButKeepInteractable(bool enabled)
	{
		if (disabledOverlay != null) disabledOverlay.SetActive(enabled);
	}

	public abstract void StartHover();

	public abstract void StopHover();

	protected abstract void OnClick();

	protected void PlaySfx()
	{
		if (AudioManager.Instance == null) return;
		if (customSfx != null) AudioManager.Instance.PlaySfx(customSfx);
		else AudioManager.Instance.PlayButtonEnter();
	}

	public void OnSelect(BaseEventData eventData)
	{
		selectedAtTime = Time.time;
		Assets.Scripts.Managers.ButtonManager.StartedHoveringButton(this);
		if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonSelect();
	}

	protected void Update()
	{
		if (scaleOnHover != null)
			scaleOnHover.localScale = Vector3.Lerp(scaleOnHover.localScale, Vector3.one * (isHovering ? hoverScale : 1f), Time.deltaTime * 18f);
	}

	public Button GetButton()
	{
		if (button == null)
		{
			button = GetComponent<Button>();
		}
		return button;
	}
}
