using TMPro;
using UnityEngine;
using Assets.Scripts.UI.Localization;

public class Funfact : MonoBehaviour
{
	public TextMeshProUGUI t_text;

	private string[] facts = {
		"i hate this game", "bonking at the speed of light is highly discouraged",
		"shh they're not bugs, they're features", "to bonk or not to bonk",
		"scientists agree: bonking is 74% effective.", "the local skeletons are on a union break.",
		"im in your walls", "no, you cant speedrun this game", "bonking is a lifestyle, not a choice.",
		"just one more run", "bonk or be bonked", "now with 10% more bonk!",
		"100% organically sourced bonks!", "bonk responsibly", "bonk with friends", "hello :)",
		"i think im gonna bonk", "bonk bonk bonk", "a severe case of skill issue", "no refunds",
		"beep boop, I am human", "do not eat yellow snow", "do not eat the enemies", "(real)",
		"megaing my bonk", "moldy cheese my beloved", "there might be bonk"
	};

	private void Start()
	{
		t_text.text = GetRandomFact();
	}

	private string GetRandomFact()
	{
		return facts[Random.Range(0, facts.Length)];
	}

	private void Update()
	{
		t_text.enabled = LocalizationUtility.IsEnglish();
		transform.localScale = Vector3.one * (1f + Mathf.Sin(Time.time * 2.5f) * .04f);
		transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * 2f) * 10f);
	}
}
