using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[ExecuteInEditMode]
public class KeyDisplay : MonoBehaviour
{
	public RawImage background;

	public TextMeshProUGUI text;

	public TextSizer textSizer;

	public RawImage glyph;

	private static Dictionary<KeyCode, string> buttonMappings;

	public void SetKey(KeyCode key)
	{
	}

	private void SetGlyph(Texture glyphTexture)
	{
	}

	private void SetNonGlyph(KeyCode keycode)
	{
	}

	private void Refresh()
	{
	}

	public static string GetKeyName(KeyCode key)
	{
		return null;
	}

	public static string GetControllerButtonName(KeyCode keycode)
	{
		return null;
	}
}
