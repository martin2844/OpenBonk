using System;
using TMPro;
using UnityEngine;

[ExecuteInEditMode]
public class TextSizer : MonoBehaviour
{
	[Flags]
	public enum Mode
	{
		None = 0,
		Horizontal = 1,
		Vertical = 2,
		Both = 3
	}

	public TMP_Text Text;

	public bool ResizeTextObject = true;

	public Vector2 Padding;

	public Vector2 MaxSize = new Vector2(1000f, float.PositiveInfinity);

	public Vector2 MinSize;

	public Mode ControlAxes = Mode.Both;

	private string _lastText;

	private Mode _lastControlAxes;

	private Vector2 _lastSize;

	private bool _forceRefresh;

	private bool _isTextNull = true;

	private RectTransform _textRectTransform;

	private RectTransform _selfRectTransform;

	protected virtual float MinX => (ControlAxes & Mode.Horizontal) != 0 ? MinSize.x : _selfRectTransform.rect.width - Padding.x;

	protected virtual float MinY => (ControlAxes & Mode.Vertical) != 0 ? MinSize.y : _selfRectTransform.rect.height - Padding.y;

	protected virtual float MaxX => (ControlAxes & Mode.Horizontal) != 0 ? MaxSize.x : _selfRectTransform.rect.width - Padding.x;

	protected virtual float MaxY => (ControlAxes & Mode.Vertical) != 0 ? MaxSize.y : _selfRectTransform.rect.height - Padding.y;

	private void Start()
	{
		Refresh();
	}

	protected virtual void Update()
	{
		Recalculate();
	}

	public void Recalculate()
	{
		if (_isTextNull) return;
		if (Text.text == _lastText && (_lastSize - _selfRectTransform.rect.size).sqrMagnitude < 1e-10f && !_forceRefresh && ControlAxes == _lastControlAxes) return;
		Vector2 preferred = Text.GetPreferredValues(MaxX, MaxY);
		float minX = MinX, maxX = MaxX, minY = MinY, maxY = MaxY;
		if ((ControlAxes & Mode.Horizontal) != 0)
		{
			float width = Mathf.Clamp(preferred.x, minX, maxX) + Padding.x;
			_selfRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
			if (ResizeTextObject) _textRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
		}
		if ((ControlAxes & Mode.Vertical) != 0)
		{
			float height = Mathf.Clamp(preferred.y, minY, maxY) + Padding.y;
			_selfRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
			if (ResizeTextObject) _textRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
		}
		_lastText = Text.text;
		_lastSize = _selfRectTransform.rect.size;
		_lastControlAxes = ControlAxes;
		_forceRefresh = false;
	}

	public virtual void Refresh()
	{
		_forceRefresh = true;
		_isTextNull = Text == null;
		if (Text != null) _textRectTransform = Text.GetComponent<RectTransform>();
		_selfRectTransform = GetComponent<RectTransform>();
	}

	private void OnValidate()
	{
		Refresh();
	}
}
