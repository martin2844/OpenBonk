using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public abstract class SelectionButton : MonoBehaviour, ISelectHandler, IEventSystemHandler, IDeselectHandler
{
	public GameObject selectionOverlay;

	public RawImage i_icon;

	public TextMeshProUGUI t_name;

	private Button button;

	protected bool clicked;

	protected bool selected;

	private void Awake()
	{
	}

	private void OnDestroy()
	{
	}

	protected abstract void Init();

	protected abstract void Cleanup();

	private void Update()
	{
	}

	public void Enable()
	{
	}

	public void Disable()
	{
	}

	protected void OnClick()
	{
	}

	protected abstract void OnClicked();

	protected void UpdateSelectionOverlay()
	{
	}

	public void OnSelect(BaseEventData eventData)
	{
	}

	public void SelectCharacter()
	{
	}

	protected abstract void OnSelectedCharacter();

	public void OnDeselect(BaseEventData eventData)
	{
	}
}
