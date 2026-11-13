using UnityEngine;
using UnityEngine.UI;

public class LanguageButton : MonoBehaviour
{
	private void Awake()
	{
		var button = GetComponent<Button>();
		if (button != null) button.onClick.AddListener(OpenLanguages);
	}

	public void OpenLanguages()
	{
		if (AlwaysUi.Instance != null) AlwaysUi.Instance.OpenLanguageWindow();
	}
}
