using System.Collections;
using System.Collections.Generic;
using Assets.Scripts.Saves___Serialization.Progression.Achievements;
using Assets.Scripts.Saves___Serialization.Progression.Unlocks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements.Experimental;

public class AchievementPopup : MonoBehaviour
{
	public RectTransform content;
	public RawImage icon;
	public TextMeshProUGUI t_title;
	public TextMeshProUGUI t_description;
	public RandomSfx sfx;
	private Queue<MyAchievement> queue = new Queue<MyAchievement>();
	private bool isAnimating;
	private float contentHeight;
	private float moveTime = 0.75f;
	private float stayTime = 4f;

	private void Awake() { MyAchievements.A_Unlocked += OnAchievementUnlocked; }
	private void OnDestroy() { MyAchievements.A_Unlocked -= OnAchievementUnlocked; }
	private void Start() { if (content != null) contentHeight = content.sizeDelta.y; }

	private void OnAchievementUnlocked(MyAchievement achievement)
	{
		queue.Enqueue(achievement);
		if (!isAnimating) StartCoroutine(ShowAchievements());
	}

	private IEnumerator ShowAchievements()
	{
		if (content == null) yield break;
		content.gameObject.SetActive(true);
		isAnimating = true;
		while (queue.Count > 0)
		{
			MyAchievement achievement = queue.Dequeue();
			if (achievement == null) continue;
			if (icon != null) icon.texture = achievement.GetIcon();
			if (t_title != null) t_title.text = achievement.GetDisplayName();
			if (t_description != null) t_description.text = achievement.GetUnlockRequirement();
			if (sfx != null) sfx.Play();
			float t = 0f;
			while (t < moveTime)
			{
				t += Time.deltaTime;
				content.anchoredPosition = Vector2.up * Mathf.Lerp(0f, contentHeight, 1f - Easing.InOutQuad(t / moveTime));
				yield return null;
			}
			yield return new WaitForSeconds(stayTime);
			t = 0f;
			while (t < moveTime)
			{
				t += Time.deltaTime;
				content.anchoredPosition = Vector2.up * Mathf.Lerp(contentHeight, 0f, 1f - Easing.InOutQuad(t / moveTime));
				yield return null;
			}
		}
		content.gameObject.SetActive(false);
		isAnimating = false;
	}
}
