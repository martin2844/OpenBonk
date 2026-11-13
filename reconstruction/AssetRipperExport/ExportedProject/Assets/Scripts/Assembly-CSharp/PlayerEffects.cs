using UnityEngine;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Utility;
using Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations;
using Assets.Scripts.Inventory__Items__Pickups.AbilitiesPassive.Implementations;

public class PlayerEffects : MonoBehaviour
{
	public GameObject mirrorFx;

	public GameObject flexFx;

	public PlayerRenderer playerRenderer;

	public ParticleSystem dashPs;

	public ParticleSystem dashCloudPs;

	private ParticleSystem.EmissionModule dashEmission;

	private float dangerValue;

	private float dangerTarget;

	private float lastHp;

	private float timeLowHp;

	private const float dangerIncreaseSpeed = 4f;

	private const float dangerDecaySpeed = 0.02f;

	private const float sustainedLowThreshold = 15f;

	private const float lerpSpeed = 1.5f;

	private void Awake()
	{
		if (dashPs != null) dashEmission = dashPs.emission;
		ItemMirror.A_MirrorReady += OnMirrorReady;
		PassiveAbilityFlex.A_FlexReady += OnFlexReady;
		if (playerRenderer != null) playerRenderer.A_CharacterSet += OnCharacterSet;
	}

	private void OnDestroy()
	{
		ItemMirror.A_MirrorReady -= OnMirrorReady;
		PassiveAbilityFlex.A_FlexReady -= OnFlexReady;
		if (playerRenderer != null) playerRenderer.A_CharacterSet -= OnCharacterSet;
	}

	private void OnCharacterSet(CharacterData characterData)
	{
		if (flexFx == null || playerRenderer == null || characterData == null) return;
		Vector3 position = flexFx.transform.position;
		position.y = playerRenderer.transform.position.y + characterData.colliderHeight * .75f;
		flexFx.transform.position = position;
		if (mirrorFx != null) mirrorFx.transform.position = position;
	}

	private void Update()
	{
		var player = MyPlayer.Instance;
		var health = player != null && player.inventory != null ? player.inventory.playerHealth : null;
		if (health == null) return;
		DashFx();
		var config = SaveManager.Instance != null ? SaveManager.Instance.config : null;
		if (config == null || config.cfVisualsSettings == null || config.cfVisualsSettings.low_hp_effects == 0)
		{
			dangerValue = 0f;
			dangerTarget = 0f;
			return;
		}
		if (health.IsDead())
		{
			dangerTarget = 0f;
			dangerValue = Mathf.Lerp(dangerValue, 0f, MyTime.deltaTime * lerpSpeed);
			return;
		}
		float hp = health.GetCombinedHp();
		float maxHp = health.GetCombinedMaxHp();
		if (maxHp <= 0f) return;
		if (hp / maxHp < .25f)
		{
			timeLowHp += MyTime.deltaTime;
			if (lastHp > 0f && lastHp > hp) dangerTarget += (lastHp - hp) / maxHp * dangerIncreaseSpeed;
			if (timeLowHp > sustainedLowThreshold) dangerTarget -= MyTime.deltaTime * dangerDecaySpeed;
			dangerTarget = Mathf.Clamp01(dangerTarget);
		}
		else { dangerTarget = 0f; timeLowHp = 0f; }
		lastHp = hp;
		dangerValue = Mathf.Lerp(dangerValue, dangerTarget, MyTime.deltaTime * lerpSpeed);
	}

	private void DashFx()
	{
		if (dashPs == null || MyPlayer.Instance == null || MyPlayer.Instance.playerMovement == null) return;
		bool dashing = MyPlayer.Instance.playerMovement.isDashing;
		if (!dashPs.isPlaying && dashing)
		{
			if (dashCloudPs != null) dashCloudPs.Play();
			dashPs.Play();
		}
		else if (dashPs.isPlaying && !dashing) dashPs.Stop();
	}

	private float RemapHpRatio(float hpRatio, float min, float max)
	{
		if (hpRatio >= max) return 0f;
		if (hpRatio <= min) return 1f;
		return 1f - (hpRatio - min) / (max - min);
	}

	public float GetDangerRatio()
	{
		return dangerValue;
	}

	public float GetDangerRatioMusic()
	{
		return Mathf.Clamp01((dangerValue - .6f) / .4f);
	}

	private void OnMirrorReady(bool isReady)
	{
		if (mirrorFx != null) mirrorFx.SetActive(isReady);
	}

	private void OnFlexReady(bool isReady)
	{
		if (flexFx != null) flexFx.SetActive(isReady);
	}
}
