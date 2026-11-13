using Assets.Scripts.Saves___Serialization.Progression.Achievements;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Assets.Scripts.UI.Animation;

public class QuestsCompletedEntry : MonoBehaviour
{
	public RawImage icon;

	public TextMeshProUGUI t_name;

	public TextMeshProUGUI t_description;

	public TextMeshProUGUI t_unlock;

	public UiAnimation uiAnimation;

	public MyButton myButton;

	public void Set(MyAchievement achievement)
	{
		if (achievement == null) return;
		if (icon != null) icon.texture = achievement.GetIcon();
		if (t_name != null) t_name.text = achievement.GetDisplayName();
		if (t_description != null) t_description.text = achievement.GetUnlockRequirement();
		if (t_unlock != null) t_unlock.text = achievement.GetUnlockedString();
		transform.localScale = Vector3.zero;
		if (uiAnimation != null) uiAnimation.Scale(1f, .2f, EEasing.InOutCirc);
		if (myButton != null) myButton.gameObject.SetActive(true);
	}
}
