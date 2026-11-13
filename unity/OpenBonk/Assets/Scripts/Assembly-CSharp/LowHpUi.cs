using UnityEngine;
using UnityEngine.UI;
using Assets.Scripts.Actors.Player;

public class LowHpUi : MonoBehaviour
{
	public RawImage vignette;

	private Color vignetteColor;

	private float maxOpacity = 1f;

	private void Awake()
	{
	}

	private void Update()
	{
		if (vignette == null || MyPlayer.Instance == null || MyPlayer.Instance.playerEffects == null) return;
		Color color = vignette.color;
		color.a = MyPlayer.Instance.playerEffects.GetDangerRatio() * maxOpacity;
		vignette.color = color;
	}
}
