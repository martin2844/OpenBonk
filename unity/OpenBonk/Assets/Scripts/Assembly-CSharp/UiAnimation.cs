using Assets.Scripts.UI.Animation;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements.Experimental;

public class UiAnimation : MonoBehaviour
{
	public MaskableGraphic element;

	private MaskableGraphic[] subElements;

	private bool checkedChildren;

	private float scaleTarget;

	private float scaleTimer;

	private float scaleTimespan;

	private float fromScale;

	private EEasing scaleEasing;

	private void Awake()
	{
		subElements = GetComponentsInChildren<MaskableGraphic>();
	}

	private void CheckSubElements()
	{
		if (checkedChildren) return;
		checkedChildren = true;
		subElements = GetComponentsInChildren<MaskableGraphic>();
	}

	public void CrossFadeAndScaleIn(float time, EEasing easing = EEasing.Linear)
	{
		CheckSubElements();
		CrossFade(0f, 0f);
		CrossFade(1f, time);
		Scale(0f, 0f, easing);
		Scale(1f, time, easing);
	}

	public void ScaleIn(float time, EEasing easing = EEasing.Linear)
	{
		CheckSubElements();
		Scale(0f, 0f, easing);
		Scale(1f, time, easing);
	}

	public void CrossFade(float alpha, float time)
	{
		CheckSubElements();
		foreach (MaskableGraphic graphic in subElements) if (graphic != null) graphic.CrossFadeAlpha(alpha, time, true);
	}

	public void Scale(float scale, float time, EEasing easing = EEasing.Linear)
	{
		CheckSubElements();
		if (element == null) return;
		fromScale = element.transform.localScale.x;
		scaleTarget = scale;
		scaleTimer = 0f;
		scaleTimespan = time;
		scaleEasing = easing;
		if (time <= 0f) { scaleTimer = 1f; element.transform.localScale = Vector3.one * scale; }
	}

	private void Update()
	{
		if (scaleTimer >= 1f || element == null || scaleTimespan <= 0f) return;
		scaleTimer = Mathf.Clamp01(scaleTimer + Time.deltaTime / scaleTimespan);
		element.transform.localScale = Vector3.one * Mathf.Lerp(fromScale, scaleTarget, GetEaseValue(scaleTimer, scaleEasing));
	}

	private float GetEaseValue(float value, EEasing easing)
	{
		switch (easing)
		{
			case EEasing.InOutCirc: return Easing.InOutCirc(value);
			case EEasing.OutCirc: return Easing.OutCirc(value);
			default: return value;
		}
	}
}
