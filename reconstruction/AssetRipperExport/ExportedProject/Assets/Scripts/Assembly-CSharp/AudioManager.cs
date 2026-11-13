using UnityEngine;
using Assets.Scripts.Utility;

public class AudioManager : MonoBehaviour
{
	public AudioSource xp;

	public AudioSource gold;

	public AudioSource silver;

	public AudioSource dungeonDoorEnter;

	private float xpPitch;

	private float xpPitchDefault = .8f;

	private float xpPitchMax = 1.8f;

	private float baseXpVolume;

	private float baseGoldVolume;

	public RandomSfx uiClick;

	public RandomSfx uiSelect;

	public RandomSfx uiInputSet;

	public RandomSfx uiAbort;

	public RandomSfx customSfx;

	public RandomSfx purchaseSfx;

	public RandomSfx bullseye;

	public RandomSfx newMenuButton;

	public static AudioManager Instance;

	private float xpAndGoldVolume = 1f;

	private int xpPerInterval;

	private int goldPerInterval;

	private float interval = 2f;

	private float nextIntervalCheck;

	private int maxPerInterval = 60;

	private int minPerInterval = 20;

	private float xpVolumeMultiplier = 1f;

	private float goldVolumeMultiplier = 1f;

	private float nextMenuSelectTime;

	private float nextMenuEnterTime;

	private float minSelectInterval = .06f;

	private bool queueSelect;

	private bool queueEnter;

	private void Awake()
	{
		if (Instance != null) { Destroy(gameObject); return; }
		Instance = this;
		baseXpVolume = xp.volume;
		baseGoldVolume = gold.volume;
	}

	private void Start()
	{
		SaveManager.A_SavesLoaded += UpdateVolumes;
		CurrentSettings.A_SettingUpdated += OnSettingUpdate;
	}

	private void OnDestroy()
	{
		SaveManager.A_SavesLoaded -= UpdateVolumes;
		CurrentSettings.A_SettingUpdated -= OnSettingUpdate;
	}

	private void OnSettingUpdate(string settingName, object oldValue, object newValue)
	{
		UpdateVolumes();
	}

	private void UpdateVolumes()
	{
		if (SaveManager.Instance != null && SaveManager.Instance.config != null)
			xpAndGoldVolume = SaveManager.Instance.config.cfAudioSettings.xp_and_gold;
	}

	public void PlayXp()
	{
		xp.Play();
		xpPerInterval++;
	}

	public void PlayGold()
	{
		gold.Play();
		goldPerInterval++;
	}

	public void PlaySilver()
	{
		silver.Play();
	}

	public void PlayNewMenuButton()
	{
		newMenuButton.Play();
	}

	private void Update()
	{
		xp.volume = Mathf.Lerp(xp.volume, baseXpVolume * xpVolumeMultiplier * xpAndGoldVolume, Time.deltaTime * .4f);
		gold.volume = Mathf.Lerp(gold.volume, baseGoldVolume * goldVolumeMultiplier * xpAndGoldVolume, Time.deltaTime * .4f);
		if (MyTime.time < nextIntervalCheck) return;
		nextIntervalCheck = MyTime.time + interval;
		xpVolumeMultiplier = xpPerInterval <= minPerInterval ? 1f : 1f - .15f * Mathf.Clamp01((float)(xpPerInterval - minPerInterval) / (maxPerInterval - minPerInterval));
		goldVolumeMultiplier = goldPerInterval <= minPerInterval ? 1f : 1f - .15f * Mathf.Clamp01((float)(goldPerInterval - minPerInterval) / (maxPerInterval - minPerInterval));
		xpPerInterval = goldPerInterval = 0;
	}

	private void LateUpdate()
	{
		if (queueEnter) uiClick.Play();
		else if (queueSelect) uiSelect.Play();
		queueSelect = queueEnter = false;
	}

	public void PlayButtonSelect()
	{
		if (nextMenuSelectTime > Time.time || nextMenuEnterTime > Time.time) return;
		nextMenuSelectTime = Time.time + minSelectInterval;
		queueSelect = true;
	}

	public void PlayButtonEnter()
	{
		if (nextMenuEnterTime > Time.time) return;
		nextMenuEnterTime = Time.time + minSelectInterval;
		queueEnter = true;
	}

	public void PlaySfx(AudioClip clip)
	{
		customSfx.sounds = new[] { clip };
		customSfx.Play();
	}

	public void Bullseye()
	{
		bullseye.Play();
	}

	public void PlayDungeonDoorEnter()
	{
		dungeonDoorEnter.Play();
	}
}
