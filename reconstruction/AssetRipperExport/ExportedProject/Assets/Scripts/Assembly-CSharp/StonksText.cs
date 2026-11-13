using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using TMPro;
using UnityEngine;

public class StonksText : MonoBehaviour
{
	private int amount;

	public TextMeshProUGUI t_gold;

	private bool active;

	private float rotationSpeed = 1f;

	private float maxRotationAngle = 10f;

	private float timeCounter;

	private float lerp;

	private float lerp2;

	private float scaleOffset;

	private void Awake()
	{
		PlayerStatusEffects.A_StatusEffectAdded += OnStatusEffectAdded;
		PlayerStatusEffects.A_StatusEffectRemoved += OnStatusEffectRemoved;
		Enemy.A_EnemyDied += OnEnemyDied;
	}

	public void Reset()
	{
		amount = 0;
		if (t_gold == null) return;
		t_gold.transform.localScale *= 1.3f;
		t_gold.text = "$0";
	}

	private void OnStatusEffectAdded(EStatusEffect eStatusEffect, bool isNewEffect)
	{
		if (eStatusEffect != EStatusEffect.Stonks || !isNewEffect) return;
		Reset();
		active = true;
	}

	private void OnStatusEffectRemoved(EStatusEffect eStatusEffect)
	{
		if (eStatusEffect == EStatusEffect.Stonks) active = false;
	}

	private void OnEnemyDied(Enemy e, DamageContainer deathSource)
	{
		if (e == null || e.enemyData == null || t_gold == null) return;
		amount += e.enemyData.GetGold();
		if (t_gold.transform.localScale.x < 2.5f) t_gold.transform.localScale *= 1.3f;
		t_gold.text = "$" + amount;
	}

	private void Update()
	{
		if (!active || t_gold == null) return;
		Transform textTransform = t_gold.transform;
		textTransform.localScale = Vector3.Lerp(textTransform.localScale, Vector3.one * scaleOffset, Time.deltaTime * 5f);
		float rotationTarget = Mathf.PingPong(Time.time * 2f, 2f) - 1f;
		float scaleTarget = Mathf.PingPong(Time.time * 4f, 2f) - 1f;
		lerp = Mathf.Lerp(lerp, rotationTarget, Time.deltaTime * 2f);
		lerp2 = Mathf.Lerp(lerp2, scaleTarget, Time.deltaTime * 2f);
		scaleOffset = 1f + lerp2 * .25f;
		textTransform.rotation = Quaternion.Euler(0f, 0f, lerp * 5f);
	}

	private void OnDestroy()
	{
		PlayerStatusEffects.A_StatusEffectAdded -= OnStatusEffectAdded;
		PlayerStatusEffects.A_StatusEffectRemoved -= OnStatusEffectRemoved;
		Enemy.A_EnemyDied -= OnEnemyDied;
	}
}
