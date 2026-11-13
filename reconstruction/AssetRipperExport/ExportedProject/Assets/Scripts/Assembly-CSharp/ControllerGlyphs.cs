using System.Collections.Generic;
using Assets.Scripts.Utility.Controllers;
using UnityEngine;

public class ControllerGlyphs : MonoBehaviour
{
	public List<InputGlyph> xboxGlyphs;

	public List<InputGlyph> playstationGlyphs;

	public List<InputGlyph> pcGlyphs;

	public Dictionary<KeyCode, Texture> xboxGlyphsDict;

	public Dictionary<KeyCode, Texture> playstationGlyphsDict;

	public Dictionary<KeyCode, Texture> pcGlyphsDict;

	public void Init()
	{
	}

	public Texture GetGlyph(EControllerType controller, KeyCode keycode)
	{
		return null;
	}
}
