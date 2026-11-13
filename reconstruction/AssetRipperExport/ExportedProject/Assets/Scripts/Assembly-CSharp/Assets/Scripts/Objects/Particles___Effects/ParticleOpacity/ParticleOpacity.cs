using Assets.Scripts.Game.Combat.ConstantAttacks;
using Assets.Scripts.Inventory__Items__Pickups.Weapons.Projectiles;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Utility;
using UnityEngine;
using UnityEngine.UIElements.Experimental;

namespace Assets.Scripts.Objects.Particles___Effects.ParticleOpacity
{
	public class ParticleOpacity : MonoBehaviour
	{
		public enum EOpacityCurve
		{
			Linear = 0,
			OutCirc = 1,
			Custom = 2
		}

		public ParticleSystem[] particleSystems;

		public float[] defaultOpacitiesParticles;

		public TrailRenderer[] trails;

		public float[] defaultOpacitiesTrailsStart;

		public float[] defaultOpacitiesTrailsEnd;

		public LineRenderer[] lines;

		public float[] defaultOpacitiesLinesStart;

		public float[] defaultOpacitiesLinesEnd;

		public MeshRenderer[] meshRenderer;

		public EffectPlayer effectPlayer;

		public bool subscribeToOpacityChange;

		private bool queueRefreshForce;

		private bool queueRefresh;

		private float readyAtTime;

		private float cooldown = 1f;

		private float lastSetOpacity = .5f;

		private bool isHidden;

		public EOpacityCurve opacityCurve;

		public AnimationCurve customCurve;

		[Header("Projectile Auto Opacity")]
		public bool useProjectileAutoOpacity;

		public ProjectileBase projectileBase;

		public ConstantAttack constantAttack;

		public float autoMinSize = 4f;

		public float autoMaxSize = 8f;

		public float minOpacity = .1f;

		public bool useScaleWithoutProjectileData;

		private string particleOpacitySettingName = "particle_opacity";

		private void Awake()
		{
			if (effectPlayer != null) effectPlayer.A_Played += Refresh;
			if (subscribeToOpacityChange) CurrentSettings.A_SettingUpdated += OnSettingUpdated;
		}

		private void OnDestroy()
		{
			if (effectPlayer != null) effectPlayer.A_Played -= Refresh;
			if (subscribeToOpacityChange) CurrentSettings.A_SettingUpdated -= OnSettingUpdated;
		}

		private void OnEnable()
		{
			queueRefresh = true;
		}

		private void Refresh()
		{
			Refresh(false);
		}

		private void Refresh(bool force = false)
		{
			if (!force && readyAtTime > Time.time) return;
			readyAtTime = Time.time + cooldown;
			var settings = SaveManager.Instance?.config?.cfVisualsSettings;
			if (settings == null) return;
			float opacity = settings.particle_opacity * GetAutoOpacity();
			if (opacityCurve == EOpacityCurve.OutCirc) opacity = Easing.OutCubic(opacity);
			else if (opacityCurve == EOpacityCurve.Custom && customCurve != null) opacity = customCurve.Evaluate(opacity);
			if (opacity > 0f && lastSetOpacity > 0f && Mathf.Abs(opacity - lastSetOpacity) < .01f) return;
			lastSetOpacity = opacity;
			if (opacity <= 0f)
			{
				isHidden = true;
				if (particleSystems != null) foreach (var ps in particleSystems) if (ps != null) { var emission = ps.emission; emission.enabled = false; ps.Stop(true, ParticleSystemStopBehavior.StopEmitting); }
				if (trails != null) foreach (var r in trails) if (r != null) r.enabled = false;
				if (lines != null) foreach (var r in lines) if (r != null) r.enabled = false;
				if (meshRenderer != null) foreach (var r in meshRenderer) if (r != null) r.enabled = false;
				return;
			}
			if (isHidden)
			{
				isHidden = false;
				if (particleSystems != null) foreach (var ps in particleSystems) if (ps != null) { var emission = ps.emission; emission.enabled = true; }
				if (trails != null) foreach (var r in trails) if (r != null) r.enabled = true;
				if (lines != null) foreach (var r in lines) if (r != null) r.enabled = true;
				if (meshRenderer != null) foreach (var r in meshRenderer) if (r != null) r.enabled = true;
			}
			if (particleSystems != null) for (int i = 0; i < particleSystems.Length; i++)
			{
				var ps = particleSystems[i]; if (ps == null) continue;
				var main = ps.main; var gradient = main.startColor; var color = gradient.color;
				color.a = Alpha(defaultOpacitiesParticles, i, color.a) * opacity;
				gradient.color = color; main.startColor = gradient;
			}
			if (trails != null) for (int i = 0; i < trails.Length; i++)
			{
				var trail = trails[i]; if (trail == null) continue;
				var start = trail.startColor; var end = trail.endColor;
				start.a = Alpha(defaultOpacitiesTrailsStart, i, start.a) * opacity;
				end.a = Alpha(defaultOpacitiesTrailsEnd, i, end.a) * opacity;
				trail.startColor = start; trail.endColor = end;
			}
			if (lines != null) for (int i = 0; i < lines.Length; i++)
			{
				var line = lines[i]; if (line == null) continue;
				var start = line.startColor; var end = line.endColor;
				start.a = Alpha(defaultOpacitiesLinesStart, i, start.a) * opacity;
				end.a = Alpha(defaultOpacitiesLinesEnd, i, end.a) * opacity;
				line.startColor = start; line.endColor = end;
			}
			if (meshRenderer != null) foreach (var r in meshRenderer) if (r != null && r.sharedMaterial != null)
			{
				var color = r.sharedMaterial.color; color.a = opacity; r.sharedMaterial.color = color;
			}
		}

		private static float Alpha(float[] values, int index, float fallback) => values != null && index < values.Length ? values[index] : fallback;

		private float GetAutoOpacity()
		{
			if (!useProjectileAutoOpacity) return 1f;
			if (!useScaleWithoutProjectileData && (projectileBase == null || projectileBase.weaponBase == null)) return 1f;
			var settings = SaveManager.Instance?.config?.cfVisualsSettings;
			if (settings == null || settings.particle_auto_opacity == 0) return 1f;
			var weapon = useScaleWithoutProjectileData ? (constantAttack != null ? constantAttack.weaponBase : null) : projectileBase.weaponBase;
			if (weapon == null) return 1f;
			float size = WeaponUtility.GetAttackSizeMultiplier(weapon);
			if (size < autoMinSize) return 1f;
			return 1f + (minOpacity - 1f) * Mathf.Clamp01((size - autoMinSize) / (autoMaxSize - autoMinSize));
		}

		public void TryValidate()
		{
			particleSystems = GetComponentsInChildren<ParticleSystem>();
			defaultOpacitiesParticles = new float[particleSystems.Length];
			for (int i = 0; i < particleSystems.Length; i++) defaultOpacitiesParticles[i] = particleSystems[i].main.startColor.color.a;
			trails = GetComponentsInChildren<TrailRenderer>();
			defaultOpacitiesTrailsStart = new float[trails.Length]; defaultOpacitiesTrailsEnd = new float[trails.Length];
			for (int i = 0; i < trails.Length; i++) { defaultOpacitiesTrailsStart[i] = trails[i].startColor.a; defaultOpacitiesTrailsEnd[i] = trails[i].endColor.a; }
			lines = GetComponentsInChildren<LineRenderer>();
			defaultOpacitiesLinesStart = new float[lines.Length]; defaultOpacitiesLinesEnd = new float[lines.Length];
			for (int i = 0; i < lines.Length; i++) { defaultOpacitiesLinesStart[i] = lines[i].startColor.a; defaultOpacitiesLinesEnd[i] = lines[i].endColor.a; }
			effectPlayer = GetComponentInChildren<EffectPlayer>();
			if (useProjectileAutoOpacity) projectileBase = GetComponent<ProjectileBase>();
		}

		private void OnSettingUpdated(string name, object oldValue, object newValue)
		{
			if (name == particleOpacitySettingName) queueRefreshForce = true;
		}

		private void Update()
		{
			if (MyTime.paused) return;
			if (queueRefreshForce)
			{
				queueRefreshForce = false; Refresh(true);
				if (particleSystems != null) foreach (var ps in particleSystems) if (ps != null) { ps.Stop(true, ParticleSystemStopBehavior.StopEmitting); ps.Play(); }
			}
			if (queueRefresh) { queueRefresh = false; Refresh(false); }
		}
	}
}
