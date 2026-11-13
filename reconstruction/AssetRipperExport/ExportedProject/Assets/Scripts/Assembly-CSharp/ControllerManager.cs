using UnityEngine;

public class ControllerManager : MonoBehaviour
{
	public ControllerGlyphs controllerGlyphs;

	public static ControllerManager Instance;

	private void Awake()
	{
	}

	public bool GetGlyph(KeyCode keycode, out Texture glyph)
	{
		glyph = null;
		return false;
	}
}
