using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Assets.Scripts.Game.Combat.EnemyDebuffs;
using Assets.Scripts.Game.Combat.EnemyDebuffs.Implementations;
using Assets.Scripts.Utility;

public class TargetOfInterestPrefab : MonoBehaviour
{
	public TextMeshProUGUI t_name;

	public TextMeshProUGUI t_hp;

	public TextMeshProUGUI t_armor;

	public RawImage hpBar;

	public RawImage bloodmark;

	public RawImage poison;

	public CanvasGroup canvasGroup;

	public Color defaultColor;

	public Color invulnerableColor;

	private Enemy enemy;

	private float fadeTimer;

	private float fadeTime = .5f;

	private float debuffLerpSpeed = 5f;

	private float bloodmarkRatio;

	private float poisonRatio;

	private float poisonTargetRatio;

	private float bloodmarkTargetRatio;

	private void Awake()
	{
		if (t_name != null)
		{
			// TextSizer otherwise expands the name beyond its boss-bar column.
			var textSizer = t_name.GetComponent<TextSizer>();
			if (textSizer != null) textSizer.enabled = false;
			// The exported name rectangle has a fixed negative width inherited
			// from the single-bar layout; let it follow its own row instead.
			RectTransform textRect = t_name.rectTransform;
			if (textRect.anchorMin.x == 0f && textRect.anchorMax.x == 1f)
			{
				Vector2 size = textRect.sizeDelta;
				size.x = -8f;
				textRect.sizeDelta = size;
			}
			t_name.enableAutoSizing = true;
			t_name.fontSizeMin = Mathf.Min(t_name.fontSizeMin, 14f);
		}
		Enemy.A_Damage += OnDamage;
		Enemy.A_HealthChange += OnHealthChange;
		Enemy.A_InvulnerableChanged += OnInvulnerableChanged;
		Enemy.A_ArmorChanged += OnArmorChange;
	}

	private void OnDestroy()
	{
		Enemy.A_Damage -= OnDamage;
		Enemy.A_HealthChange -= OnHealthChange;
		Enemy.A_InvulnerableChanged -= OnInvulnerableChanged;
		Enemy.A_ArmorChanged -= OnArmorChange;
	}

	public void SetEnemy(Enemy enemy)
	{
		if (this.enemy == enemy) return;
		this.enemy = enemy;
		if (hpBar != null) hpBar.color = defaultColor;
		gameObject.SetActive(enemy != null);
		if (enemy == null) return;
		if (t_name != null) t_name.text = enemy.enemyData != null ? enemy.enemyData.GetName() : string.Empty;
		UpdateHp();
		if (canvasGroup != null) canvasGroup.alpha = 0f;
		fadeTimer = 0f;
		if (t_armor != null) t_armor.gameObject.SetActive(false);
		OnArmorChange(enemy, enemy.armorCurrent, enemy.armorMax);
		bloodmarkRatio = poisonRatio = poisonTargetRatio = bloodmarkTargetRatio = 0f;
		if (bloodmark != null) bloodmark.transform.localScale = Vector3.zero;
		if (poison != null) poison.transform.localScale = Vector3.zero;
	}

	private void Update()
	{
		if (enemy == null) return;
		UpdateDebuffs();
		if (fadeTimer < 1f)
		{
			fadeTimer += fadeTime > 0f ? MyTime.deltaTime / fadeTime : 1f;
			if (canvasGroup != null) canvasGroup.alpha = Mathf.Clamp01(fadeTimer);
		}
	}

	private void UpdateHp()
	{
		if (enemy == null) return;
		float current = Mathf.Round(enemy.hp), max = Mathf.Round(enemy.maxHp);
		if (t_hp != null) t_hp.text = DamageNumbers.FormatDamageNumber(current) + " / " + DamageNumbers.FormatDamageNumber(max);
		float ratio = max > 0f ? Mathf.Clamp01(current / max) : 0f;
		if (hpBar != null) hpBar.transform.localScale = new Vector3(ratio, 1f, 1f);
		if (bloodmarkTargetRatio + poisonTargetRatio > 1f)
			poisonTargetRatio = 1f - bloodmarkTargetRatio;
	}

	private void UpdateDebuffs()
	{
		if (enemy == null || enemy.maxHp <= 0f) return;
		bloodmarkTargetRatio = enemy.debuffs.TryGetValue(EDebuff.Bloodmark, out var blood) && blood is DebuffBloodmark bloodDebuff ? Mathf.Clamp01(bloodDebuff.GetDamage() / enemy.maxHp) : 0f;
		poisonTargetRatio = enemy.debuffs.TryGetValue(EDebuff.Poison, out var poisonDebuff) && poisonDebuff is DebuffPoison poisonValue ? Mathf.Clamp01(poisonValue.GetDamageForHpBar() / enemy.maxHp) : 0f;
		bloodmarkRatio = Mathf.Lerp(bloodmarkRatio, bloodmarkTargetRatio, MyTime.deltaTime * debuffLerpSpeed);
		poisonRatio = Mathf.Lerp(poisonRatio, poisonTargetRatio, MyTime.deltaTime * debuffLerpSpeed);
		float hpRatio = hpBar != null ? hpBar.transform.localScale.x : 0f;
		bloodmarkRatio = Mathf.Min(bloodmarkRatio, hpRatio);
		poisonRatio = Mathf.Min(poisonRatio, hpRatio - bloodmarkRatio);
		if (bloodmark != null) bloodmark.transform.localScale = new Vector3(bloodmarkRatio, 1f, 1f);
		if (poison != null)
		{
			poison.transform.localScale = new Vector3(poisonRatio, 1f, 1f);
			float width = hpBar != null ? hpBar.rectTransform.rect.width : 0f;
			poison.rectTransform.anchoredPosition = new Vector2(2f + width * (hpRatio - poisonRatio), 0f);
		}
	}

	private void OnDamage(Enemy enemy, DamageContainer dc)
	{
		if (enemy == this.enemy) UpdateHp();
	}

	private void OnInvulnerableChanged(Enemy enemy, bool invulnerable)
	{
		if (enemy == this.enemy && hpBar != null) hpBar.color = invulnerable ? invulnerableColor : defaultColor;
	}

	private void OnHealthChange(Enemy enemy)
	{
		if (enemy == this.enemy) UpdateHp();
	}

	private void OnArmorChange(Enemy enemy, int current, int max)
	{
		if (enemy != this.enemy || t_armor == null) return;
		t_armor.gameObject.SetActive(true);
		t_armor.text = string.Empty;
		for (int i = 0; i < max; i++) t_armor.text += i < current ? "<sprite name=Shield>" : "<sprite name=ShieldBroken>";
	}
}
