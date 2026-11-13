using UnityEngine;
using UnityEngine.EventSystems;

public class CharacterSpin : MonoBehaviour, IPointerDownHandler, IEventSystemHandler, IPointerUpHandler
{
	private float totalSpin;

	private Vector2 lastPosition;

	private bool mouseSpinning;

	public Transform playerRenderer;

	private float maxSpin = 600f;

	private float controllerSpin = 4f;

	private void OnEnable()
	{
		mouseSpinning = false;
	}

	private void Update()
	{
		if (mouseSpinning)
		{
			Vector2 position = Input.mousePosition;
			AddSpin(-(position.x - lastPosition.x));
			lastPosition = position;
		}
		if (MyInputManager.GetButton(MyInputManager.UIShoulderLeft)) AddSpin(controllerSpin);
		else if (MyInputManager.GetButton(MyInputManager.UIShoulderRight)) AddSpin(-controllerSpin);
		if (totalSpin == 0f || playerRenderer == null) return;
		float spin = Mathf.Sign(totalSpin) * Mathf.Clamp(Mathf.Abs(totalSpin * 4f), 40f, 1500f) * Time.deltaTime;
		totalSpin -= spin;
		playerRenderer.Rotate(Vector3.up * spin);
		if (Mathf.Abs(totalSpin) <= .1f) totalSpin = 0f;
	}

	public void OnPointerDown(PointerEventData eventData)
	{
		if (eventData == null) return;
		mouseSpinning = true;
		lastPosition = eventData.position;
	}

	public void OnPointerUp(PointerEventData eventData)
	{
		mouseSpinning = false;
	}

	private void AddSpin(float spin)
	{
		totalSpin = Mathf.Clamp(totalSpin + spin, -maxSpin, maxSpin);
	}
}
