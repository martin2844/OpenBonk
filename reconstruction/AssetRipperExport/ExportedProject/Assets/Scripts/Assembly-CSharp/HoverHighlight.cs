using UnityEngine;
using UnityEngine.UI;

public class HoverHighlight : MonoBehaviour
{
    public Color defaultColor;
    public Color highlightColor;
    private MaskableGraphic graphic;
    private RectTransform rectTransform;
    private bool mouseOver;

    private void Awake()
    {
        graphic = GetComponent<MaskableGraphic>();
        if (graphic != null) rectTransform = graphic.GetComponent<RectTransform>();
    }

    private void Update() { CustomPointerHandler(); }

    private void CustomPointerHandler()
    {
        if (rectTransform == null) return;
        Vector3 local = rectTransform.InverseTransformPoint(Input.mousePosition);
        bool inside = rectTransform.rect.Contains(local);
        if (inside && !mouseOver) OnPointerEnter();
        else if (!inside && mouseOver) OnPointerExit();
    }

    private void OnPointerEnter()
    {
        mouseOver = true;
        if (graphic != null) graphic.color = highlightColor;
    }

    private void OnPointerExit()
    {
        mouseOver = false;
        if (graphic != null) graphic.color = defaultColor;
    }
}
