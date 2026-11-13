using System;
using System.Collections.Generic;
using Assets.Scripts.Actors;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Utility;
using UnityEngine;

public class PlayerRenderer : MonoBehaviour
{
	public PlayerMovement playerMovement;

	public Animator animator;

	public Material damageFlash;

	private Material defaultMaterial;

	public SkinnedMeshRenderer renderer;

	private Transform hatTransform;

	private MeshRenderer hatRenderer;

	private MeshFilter hatFilter;

	public Transform hips;

	public Transform torso;

	private Quaternion desiredLookRotation;

	private float rotationSpeed = 15f;

	private float stoppedMovingAtTime;

	private float movingToIdleTimeout = 0.1f;

	private bool moving;

	private float resetMaterialTime;

	private GameObject rendererObject;
	private OpenBonkExternalAnimator externalAnimator;
	private Transform externalHatAnchor;
	private Renderer[] externalRenderers;
	private readonly List<Outline> externalOutlines = new List<Outline>();
	private Material[][] externalBeforeFlash;
	private readonly List<Material> externalOwnedMaterials = new List<Material>();
	private readonly Dictionary<Material, ExternalTint> externalTints = new Dictionary<Material, ExternalTint>();
	private sealed class ExternalTint { public string property; public Color original; }

	public Action<CharacterData> A_CharacterSet;

	private CharacterData characterData;

	private SkinData skinData;

	public List<Renderer> subRenderers = new List<Renderer>();

	protected Material[] activeMaterials;

	private List<Material> allMaterials;

	private Color defaultGoochDarkColor = new Color32(128, 128, 128, 255);

	private HatData currentHat;

	private bool isDamageFlash;

	private Material[] beforeDamageFlashMaterials;

	private bool shieldActive;

	private Outline outline;

	private Outline hatOutline;

	public Color shieldColor;

	public Color colorFreeze;

	public Color colorMud;

	public Color poisonColor;

	public Color colorNothing;

	private Vector3 smoothedNormal;

	private Vector3 lastValidMoveDirection;

	private float smoothingMultiplier = 1f;

	private void Awake()
	{
		GameObject hat = new GameObject("PlayerHat");
		hatTransform = hat.transform;
		hat.layer = LayerMask.NameToLayer("Player");
		hatTransform.SetParent(transform);
		hatRenderer = hat.AddComponent<MeshRenderer>();
		hatFilter = hat.AddComponent<MeshFilter>();
		hat.SetActive(false);
		MyTime.A_Pause += OnPause;
		PlayerHealth.A_TakeDamage += OnDamage;
		PlayerHealth.A_Heal += OnHeal;
		PlayerHealth.A_Died += OnDeath;
		PlayerStatusEffects.A_StatusEffectAdded += OnStatusEffectAdded;
		PlayerStatusEffects.A_StatusEffectRemoved += OnStatusEffectRemoved;
		PlayerMovement.A_StartedWallClimb += ForceWalkAnimation;
		MyPlayer.A_PlayerInventoryInitialized += OnInventoryInit;
	}

	private void OnDestroy()
	{
		ReleaseExternalMaterials();
		MyTime.A_Pause -= OnPause;
		PlayerHealth.A_TakeDamage -= OnDamage;
		PlayerHealth.A_Heal -= OnHeal;
		PlayerHealth.A_Died -= OnDeath;
		PlayerStatusEffects.A_StatusEffectAdded -= OnStatusEffectAdded;
		PlayerStatusEffects.A_StatusEffectRemoved -= OnStatusEffectRemoved;
		PlayerMovement.A_StartedWallClimb -= ForceWalkAnimation;
		MyPlayer.A_PlayerInventoryInitialized -= OnInventoryInit;
	}

	private void OnInventoryInit(PlayerInventory inventory)
	{
		bool hasShield = inventory.playerHealth.shield > 0f;
		ChangeShield(hasShield);
	}

	public void SetCharacter(CharacterData characterData, PlayerInventory inventory, Vector3 spawnDir)
	{
		if (isDamageFlash) ResetMaterial();
		ReleaseExternalMaterials();
		hatTransform.SetParent(null);
		this.characterData = characterData;
		if (rendererObject != null) { Destroy(rendererObject); animator = null; }
		outline = null;
		externalOutlines.Clear();
		externalAnimator = null;
		externalHatAnchor = null;
		if (characterData == null) return;
		skinData = DataManager.Instance.GetSkin(characterData.eCharacter, SaveManager.Instance.config.preferences.GetCharacterSkin(characterData.eCharacter));
		if (spawnDir != Vector3.zero)
		{
			transform.rotation = Quaternion.LookRotation(spawnDir);
			desiredLookRotation = Quaternion.LookRotation(spawnDir);
		}
		else desiredLookRotation = Quaternion.identity;
		lastValidMoveDirection = spawnDir;
		if (characterData.prefab == null) return;
		rendererObject = Instantiate(characterData.prefab, transform);
		rendererObject.transform.localPosition = Vector3.zero;
		rendererObject.transform.localRotation = Quaternion.identity;
		animator = rendererObject.GetComponent<Animator>();
		renderer = rendererObject.GetComponentInChildren<SkinnedMeshRenderer>();
		if (characterData is OpenBonkExternalCharacterData external)
		{
			rendererObject.transform.localScale = Vector3.one * external.modelScale;
			int playerLayer = LayerMask.NameToLayer("Player");
			if (playerLayer >= 0) foreach (Transform part in rendererObject.GetComponentsInChildren<Transform>(true)) part.gameObject.layer = playerLayer;
			if (animator == null) animator = rendererObject.AddComponent<Animator>();
			if (renderer == null) { Debug.LogError("External character has no skinned mesh"); return; }
			var renderers = new List<Renderer>();
			foreach (Renderer part in rendererObject.GetComponentsInChildren<Renderer>(true))
			{
				if (!(part is SkinnedMeshRenderer) && !(part is MeshRenderer)) continue;
				renderers.Add(part);
				foreach (Material material in part.materials)
				{
					if (material == null) continue;
					if (!externalOwnedMaterials.Contains(material)) externalOwnedMaterials.Add(material);
					string property = material.HasProperty("_BaseColor") ? "_BaseColor" : material.HasProperty("_Color") ? "_Color" : null;
					if (property != null && !externalTints.ContainsKey(material)) externalTints.Add(material, new ExternalTint { property = property, original = material.GetColor(property) });
				}
			}
			externalRenderers = renderers.ToArray();
			activeMaterials = externalOwnedMaterials.ToArray();
			externalAnimator = rendererObject.AddComponent<OpenBonkExternalAnimator>();
			externalAnimator.Initialize(animator, external);
			externalHatAnchor = CreateExternalHatAnchor(external);
		}
		else SetSkin(skinData);
		if (rendererObject.layer != LayerMask.NameToLayer("Player")) Debug.LogError("Character prefab is not on the Player layer.");
		foreach (Transform part in rendererObject.GetComponentsInChildren<Transform>())
		{
			string name = part.gameObject.name.ToLower();
			if (name == "torso") torso = part;
			if (name == "hips" || name == "root") hips = part;
		}
		SetHat(DataManager.Instance.GetHat(SaveManager.Instance.config.preferences.GetCharacterHat(characterData.eCharacter)));
		if (shieldActive) RefreshPlayerOutlines();
		A_CharacterSet?.Invoke(characterData);
	}

	private Transform CreateExternalHatAnchor(OpenBonkExternalCharacterData external)
	{
		Transform head = null;
		foreach (Transform part in rendererObject.GetComponentsInChildren<Transform>(true))
		{
			if (external.hasHatOverride ? part.name == external.hatBone :
				string.Equals(part.name, "Head", StringComparison.OrdinalIgnoreCase) || string.Equals(part.name, "Head_Armature", StringComparison.OrdinalIgnoreCase))
			{ head = part; break; }
		}
		if (head == null) return null;
		Vector3 headScale = head.lossyScale, parentScale = transform.lossyScale;
		if (Mathf.Abs(headScale.x) < .00001f || Mathf.Abs(headScale.y) < .00001f || Mathf.Abs(headScale.z) < .00001f) return null;
		var anchor = new GameObject("OpenBonkHatAnchor").transform;
		anchor.SetParent(head, false);
		Vector3 offset = external.hasHatOverride ? external.hatOffset : new Vector3(0f,
			renderer.bounds.size.y / Mathf.Max(Mathf.Abs(parentScale.y), .00001f) * .1f,
			renderer.bounds.size.z / Mathf.Max(Mathf.Abs(parentScale.z), .00001f) * .1f);
		anchor.position = head.position + transform.TransformVector(offset);
		anchor.rotation = transform.rotation;
		float scale = external.hasHatOverride ? external.hatScale : 1f;
		anchor.localScale = new Vector3(parentScale.x / headScale.x, parentScale.y / headScale.y, parentScale.z / headScale.z) * scale;
		return anchor;
	}

	private void ReleaseExternalMaterials()
	{
		if (externalRenderers != null) activeMaterials = null;
		foreach (Material material in externalOwnedMaterials) if (material != null) Destroy(material);
		externalOwnedMaterials.Clear(); externalTints.Clear(); externalRenderers = null; externalBeforeFlash = null;
	}

	private void CreateMaterials(int amount)
	{
		allMaterials ??= new List<Material>();
		activeMaterials = new Material[amount];
		while (allMaterials.Count < amount) allMaterials.Add(new Material(AlwaysManager.Instance.playerMaterialPreset));
		for (int i = 0; i < amount; i++) activeMaterials[i] = allMaterials[i];
		renderer.materials = (Material[])activeMaterials.Clone();
		foreach (Material material in activeMaterials)
		{
			material.SetColor("_IridescenceColor", colorNothing);
			material.SetColor("_AlbedoColor", Color.white);
			material.SetColor("_EmissionColor", Color.black);
			material.SetColor("_GoochDarkColor", defaultGoochDarkColor);
			material.SetFloat("_IndirectFade", 1f);
		}
	}

	public void SetSkin(SkinData skinData)
	{
		if (characterData is OpenBonkExternalCharacterData) return;
		CreateMaterials(skinData.materials.Length);
		bool fullShader = SystemInfo.graphicsShaderLevel >= 45;
		for (int i = 0; i < activeMaterials.Length; i++)
		{
			Material source = skinData.materials[i];
			Material material = activeMaterials[i];
			// The reconstructed toon shader samples these properties on every
			// graphics tier, including Android devices below shader level 45.
			material.SetTexture("_AlbedoMap", source.mainTexture);
			material.SetColor("_AlbedoColor", source.color);
			material.SetTexture("_MainTex", source.mainTexture);
			material.SetColor("_Color", source.color);
			// Preserve the source material's constant or texture-masked emission
			// on every graphics tier supported by the reconstructed shader.
			material.SetColor("_EmissionColor", source.GetColor("_EmissionColor"));
			material.SetTexture("_EmissionMap", source.GetTexture("_EmissionMap"));
			if (source.IsKeywordEnabled("_MK_EMISSION")) material.EnableKeyword("_MK_EMISSION"); else material.DisableKeyword("_MK_EMISSION");
			if (source.IsKeywordEnabled("_MK_EMISSION_MAP")) material.EnableKeyword("_MK_EMISSION_MAP"); else material.DisableKeyword("_MK_EMISSION_MAP");
			// The runtime reuses preset materials between skins. Copy both the
			// animation parameters and keywords, and clear them for other skins.
			material.SetFloat("_VertexAnimationIntensity", source.GetFloat("_VertexAnimationIntensity"));
			material.SetVector("_VertexAnimationFrequency", source.GetVector("_VertexAnimationFrequency"));
			if (source.IsKeywordEnabled("_MK_VERTEX_ANIMATION_SINE")) material.EnableKeyword("_MK_VERTEX_ANIMATION_SINE"); else material.DisableKeyword("_MK_VERTEX_ANIMATION_SINE");
			if (source.IsKeywordEnabled("_MK_VERTEX_ANIMATION_STUTTER")) material.EnableKeyword("_MK_VERTEX_ANIMATION_STUTTER"); else material.DisableKeyword("_MK_VERTEX_ANIMATION_STUTTER");
			if (fullShader)
			{
				material.SetColor("_GoochDarkColor", source.GetColor("_GoochDarkColor"));
				material.SetFloat("_IndirectFade", source.GetFloat("_IndirectFade"));
			}
			// The Android replacement shader supports the surface state on GLES
			// even when Unity reports a graphics shader level below 45.
			if (!fullShader && Application.platform != RuntimePlatform.Android) continue;
			int face = source.GetInt("_RenderFace");
			if (face == 0)
			{
				material.SetInt("_RenderFace", 0);
				material.SetInt("_AlphaClipping", 1);
				material.SetFloat("_AlphaCutoff", 0.8f);
				material.SetInt("_Outline", 3);
				material.SetInt("_VertexAnimation", 1);
				material.SetFloat("_VertexAnimationIntensity", 0.05f);
				material.SetInt("_VertexAnimationStutter", 1);
				material.SetVector("_VertexAnimationFrequency", Vector3.one * 6f);
			}
			else if (face == 1)
			{
				material.SetInt("_RenderFace", 2);
				material.SetInt("_AlphaClipping", 0);
				material.SetInt("_Outline", 1);
				material.SetInt("_VertexAnimation", 0);
			}
			else if (face == 2 && Application.platform == RuntimePlatform.Android)
			{
				// Restore the prior Android defaults when a reused leaf material
				// is assigned to an ordinary, back-face-culled skin.
				Material preset = AlwaysManager.Instance.playerMaterialPreset;
				material.SetInt("_RenderFace", preset.GetInt("_RenderFace"));
				material.SetInt("_AlphaClipping", preset.GetInt("_AlphaClipping"));
				material.SetFloat("_AlphaCutoff", preset.GetFloat("_AlphaCutoff"));
				material.SetInt("_Outline", preset.GetInt("_Outline"));
				material.SetInt("_VertexAnimation", 0);
			}
		}
	}

	public HatData GetCurrentHat()
	{
		return currentHat;
	}

	public void SetHat(HatData hatData)
	{
		currentHat = hatData;
		if (characterData is OpenBonkExternalCharacterData external)
		{
			var reference = hatData?.orientations?.Find(x => x.character == ECharacter.Fox);
			if (hatData == null || externalHatAnchor == null || hatData.mesh == null || hatData.material == null || reference == null)
			{ hatTransform.SetParent(transform, false); hatTransform.gameObject.SetActive(false); if (shieldActive && !isDamageFlash) RefreshPlayerOutlines(); return; }
			hatTransform.SetParent(externalHatAnchor, false);
			Vector3 position = reference.pos;
			// The original Fox reference puts glasses above the eyes on models
			// with a proportionally taller head. Apply a bounds-based fallback;
			// authored anchors can supply their own alignment instead.
			if (!external.hasHatOverride && hatData.eHat == Assets.Scripts._Data.Hats.EHat.Glasses && renderer != null)
			{
				Vector3 parentScale = transform.lossyScale;
				position.y -= renderer.bounds.size.y / Mathf.Max(Mathf.Abs(parentScale.y), .00001f) * .05f;
				position.z += renderer.bounds.size.z / Mathf.Max(Mathf.Abs(parentScale.z), .00001f) * .03f;
			}
			hatTransform.localPosition = position;
			hatTransform.localRotation = Quaternion.Euler(reference.rot);
			hatTransform.localScale = reference.scale;
			hatFilter.sharedMesh = hatData.mesh; hatRenderer.sharedMaterial = hatData.material; hatTransform.gameObject.SetActive(true);
			if (shieldActive && !isDamageFlash) RefreshPlayerOutlines();
			return;
		}
		if (characterData.meshDefault != null) renderer.sharedMesh = characterData.meshDefault;
		foreach (Transform part in rendererObject.GetComponentsInChildren<Transform>())
		{
			string name = part.gameObject.name.ToLower();
			if (name != "head" && name != "neck") continue;
			hatTransform.SetParent(part);
			hatTransform.localPosition = Vector3.zero;
			hatTransform.localRotation = Quaternion.identity;
		}
		if (hatData == null) { hatTransform.gameObject.SetActive(false); return; }
		hatTransform.gameObject.SetActive(true);
		hatFilter.sharedMesh = hatData.mesh;
		hatRenderer.sharedMaterial = hatData.material;
		var orientation = hatData.orientations.Find(x => x.character == characterData.eCharacter);
		if (orientation == null) return;
		hatTransform.localPosition = orientation.pos;
		hatTransform.localRotation = Quaternion.Euler(orientation.rot);
		hatTransform.localScale = orientation.scale;
		if (orientation.meshForHat == Assets.Scripts._Data.Hats.EMeshForHat.Low) renderer.sharedMesh = characterData.meshLow;
		else if (orientation.meshForHat == Assets.Scripts._Data.Hats.EMeshForHat.Lowest) renderer.sharedMesh = characterData.meshLowest;
	}

	private void LateUpdate()
	{
	}

	private bool HasRenderer()
	{
		return rendererObject != null;
	}

	public void ResetMaterial()
	{
		if (!isDamageFlash) return;
		isDamageFlash = false;
		if (externalBeforeFlash != null && externalRenderers != null)
		{
			for (int i = 0; i < externalRenderers.Length; i++)
				if (externalRenderers[i] != null && externalBeforeFlash[i] != null) externalRenderers[i].sharedMaterials = externalBeforeFlash[i];
			externalBeforeFlash = null;
			RefreshPlayerOutlines();
			return;
		}
		renderer.sharedMaterials = beforeDamageFlashMaterials;
	}

	public void ForceMoving(bool b)
	{
		if (externalAnimator != null) externalAnimator.SetMoving(b); else animator.SetBool("moving", b);
	}

	private void Update()
	{
		if (activeMaterials == null || (PlayerCamera.Instance != null && PlayerCamera.Instance.GetCameraState() == PlayerCamera.ECameraState.Portal) || playerMovement == null || animator == null || animator.speed <= 0f || MyTime.paused || rendererObject == null) return;
		if (isDamageFlash && MyTime.time >= resetMaterialTime) ResetMaterial();
		transform.position = playerMovement.feet.position;
		Vector3 direction = playerMovement.GetWishDir();
		if (Assets.Scripts.Saves___Serialization.Progression.Challenges.ChallengesTracker.HasChallengeModifier("no_movement")) direction = MyPlayer.Instance.playerInput.GetWishDir();
		if (playerMovement.IsHoldingMovement() || playerMovement.IsWallClimbing())
		{
			if (externalAnimator != null) externalAnimator.SetMoving(true); else animator.SetBool("moving", true);
			moving = true;
		}
		else if (moving)
		{
			stoppedMovingAtTime = MyTime.time;
			moving = false;
		}
		else if (MyTime.time >= stoppedMovingAtTime + movingToIdleTimeout) { if (externalAnimator != null) externalAnimator.SetMoving(false); else animator.SetBool("moving", false); }
		if (direction != Vector3.zero) lastValidMoveDirection = direction;
		Vector3 targetNormal = Vector3.up;
		smoothingMultiplier = 1f;
		if (playerMovement.IsGrinding())
		{
			targetNormal = playerMovement.GetGrindNormal();
			lastValidMoveDirection = playerMovement.GetGrindDirection();
		}
		else if (playerMovement.IsWallClimbing()) targetNormal = (playerMovement.wallNormal + Vector3.up).normalized;
		else if (GameManager.Instance != null && Physics.Raycast(transform.position + Vector3.up, Vector3.down, out RaycastHit hit, 51f, GameManager.Instance.whatIsGround) && !hit.collider.gameObject.CompareTag("CameraIgnore"))
		{
			targetNormal = hit.normal;
			smoothingMultiplier = 1f - Mathf.Clamp01((hit.distance - 1f) / 3f) * 0.95f;
		}
		smoothedNormal = Vector3.Lerp(smoothedNormal, targetNormal, Time.deltaTime * 14f * smoothingMultiplier);
		Vector3 projected = Vector3.ProjectOnPlane(lastValidMoveDirection, smoothedNormal);
		if (projected != Vector3.zero)
		{
			desiredLookRotation = Quaternion.LookRotation(projected, smoothedNormal);
			transform.rotation = desiredLookRotation;
		}
		if (externalAnimator != null) externalAnimator.SetMovementState(playerMovement.IsTouchingGround() || playerMovement.IsWallClimbing(), playerMovement.RendererJumping, playerMovement.RendererGrindingPose || playerMovement.RendererSurfing, playerMovement.GetVelocity().magnitude, MyPlayer.Instance != null && MyPlayer.Instance.playerInput != null ? MyPlayer.Instance.playerInput.MovementInputMagnitude : 1f);
		else
		{
			animator.SetBool("grounded", playerMovement.IsTouchingGround() || playerMovement.IsWallClimbing());
			animator.SetBool("jumping", playerMovement.RendererJumping);
			animator.SetBool("grinding", playerMovement.RendererGrindingPose);
		}
	}

	public void ForceRotation(Vector3 dir)
	{
		smoothedNormal = Vector3.up;
		lastValidMoveDirection = dir;
		Vector3 projected = Vector3.ProjectOnPlane(dir, smoothedNormal);
		if (projected == Vector3.zero) return;
		desiredLookRotation = Quaternion.LookRotation(projected, smoothedNormal);
		transform.rotation = desiredLookRotation;
	}

	private void ForceWalkAnimation()
	{
		if (externalAnimator != null) externalAnimator.SetMoving(true); else animator.Play("Walking");
	}

	private void OnPause(bool paused)
	{
		if (animator != null) animator.speed = paused ? 0f : 1f;
	}

	private void OnDamage(PlayerHealth ph, DamageContainer dc, bool shieldDamage)
	{
		if (isDamageFlash) { if (externalRenderers != null) ChangeShield(ph.shield > 0f); return; }
		ChangeShield(ph.shield > 0f);
		if (externalRenderers != null)
		{
			if (damageFlash == null) return;
			externalBeforeFlash = new Material[externalRenderers.Length][];
			for (int i = 0; i < externalRenderers.Length; i++)
			{
				if (externalRenderers[i] == null) continue;
				var materials = externalRenderers[i].sharedMaterials;
				externalBeforeFlash[i] = materials;
				var flash = new Material[materials.Length];
				for (int j = 0; j < flash.Length; j++) flash[j] = damageFlash;
				externalRenderers[i].sharedMaterials = flash;
			}
			resetMaterialTime = MyTime.time + 0.15f;
			isDamageFlash = true;
			return;
		}
		beforeDamageFlashMaterials = renderer.sharedMaterials;
		renderer.sharedMaterial = damageFlash;
		resetMaterialTime = MyTime.time + 0.15f;
		isDamageFlash = true;
	}

	private void OnHeal(PlayerHealth ph, float amount, bool isShield)
	{
		ChangeShield(ph.shield > 0f);
	}

	private void ChangeShield(bool on)
	{
		if (shieldActive == on) return;
		shieldActive = on;
		// Keep the saved material arrays consistent until the damage flash ends.
		if (externalRenderers != null && isDamageFlash) return;
		RefreshPlayerOutlines();
	}

	private void RefreshPlayerOutlines()
	{
		if (shieldActive)
		{
			if (externalRenderers != null)
			{
				foreach (var part in externalRenderers)
				{
					if (part == null) continue;
					var partOutline = part.GetComponent<Outline>();
					if (partOutline == null) partOutline = part.gameObject.AddComponent<Outline>();
					partOutline.ConfigureExternalSubmeshes();
					partOutline.OutlineColor = shieldColor;
					partOutline.OutlineWidth = 6f;
					partOutline.OutlineMode = Outline.Mode.OutlineVisible;
					partOutline.enabled = true;
					if (!externalOutlines.Contains(partOutline)) externalOutlines.Add(partOutline);
				}
			}
			else if (renderer != null && outline == null)
			{
				outline = renderer.gameObject.AddComponent<Outline>();
				outline.OutlineColor = shieldColor;
				outline.OutlineWidth = 6f;
				outline.OutlineMode = Outline.Mode.OutlineVisible;
			}
			if (externalRenderers == null && outline != null) outline.enabled = true;
			if (hatTransform.gameObject.activeInHierarchy)
			{
				if (hatOutline == null)
				{
					hatOutline = hatRenderer.gameObject.AddComponent<Outline>();
					hatOutline.OutlineColor = shieldColor;
					hatOutline.OutlineWidth = 6f;
					hatOutline.OutlineMode = Outline.Mode.OutlineVisible;
				}
				hatOutline.enabled = true;
			}
			else if (externalRenderers != null && hatOutline != null) hatOutline.enabled = false;
		}
		else
		{
			if (outline != null) outline.enabled = false;
			foreach (var partOutline in externalOutlines) if (partOutline != null) partOutline.enabled = false;
			if (hatOutline != null) hatOutline.enabled = false;
		}
	}

	public void SetMaterial(Material mat)
	{
		foreach (Renderer child in GetComponentsInChildren<Renderer>())
		{
			if (child.GetComponent<ParticleSystem>() != null) continue;
			Material[] materials = child.sharedMaterials;
			for (int i = 0; i < materials.Length; i++) materials[i] = mat;
			child.sharedMaterials = materials;
		}
	}

	public void SetIdle()
	{
		if (animator == null) return;
		if (externalAnimator != null) { externalAnimator.SetMoving(false); return; }
		animator.SetBool("grounded", true);
		animator.SetBool("moving", false);
		if (characterData.eCharacter == (ECharacter)1) animator.SetBool("idle", true);
	}

	private void OnDeath()
	{
		if (animator != null) animator.speed = 0f;
	}

	private void OnStatusEffectAdded(EStatusEffect effect, bool newEffect)
	{
		RefreshStatusEffectColor();
	}

	private void OnStatusEffectRemoved(EStatusEffect effect)
	{
		RefreshStatusEffectColor();
	}

	private void RefreshStatusEffectColor()
	{
		if (activeMaterials == null) return;
		var player = MyPlayer.Instance;
		var effects = player != null && player.inventory != null ? player.inventory.statusEffects : null;
		Color color = colorNothing;
		bool enabled = false;
		if (effects != null)
		{
			if (effects.HasStatusEffect(EStatusEffect.Freeze)) { color = colorFreeze; enabled = true; }
			else if (effects.HasStatusEffect(EStatusEffect.Slow)) { color = colorMud; enabled = true; }
			else if (effects.HasStatusEffect(EStatusEffect.Bleed)) { color = Color.red; enabled = true; }
			else if (effects.HasStatusEffect(EStatusEffect.Poison) || effects.HasStatusEffect(EStatusEffect.BossPoison)) { color = poisonColor; enabled = true; }
		}
		foreach (Material material in activeMaterials)
		{
			if (material == null) continue;
			if (externalTints.TryGetValue(material, out var tint) && !material.HasProperty("_IridescenceColor"))
			{
				Color tinted = enabled ? Color.Lerp(tint.original, color, .45f) : tint.original;
				tinted.a = tint.original.a;
				material.SetColor(tint.property, tinted);
				continue;
			}
			material.SetColor("_IridescenceColor", color);
			// The original only changes the keyword when a player is present.
			if (player == null) continue;
			if (enabled)
			{
				material.SetFloat("_Iridescence", 1f);
				material.EnableKeyword("_MK_IRIDESCENCE_DEFAULT");
				material.EnableKeyword("_IRIDESCENCE_ON");
				material.SetFloat("_IridescenceSize", 0.18f);
			}
			else material.DisableKeyword("_MK_IRIDESCENCE_DEFAULT");
		}
	}

	public void SetOutlineColor(Color color)
	{
		if (activeMaterials == null) return;
		foreach (Material material in activeMaterials)
			if (material != null) material.SetColor("_OutlineColor", color);
	}

	public void SetRim(Color color, float size)
	{
		if (activeMaterials == null) return;
		foreach (Material material in activeMaterials)
		{
			if (material == null) continue;
			material.SetFloat("_Rim", 1f);
			material.SetColor("_RimColor", color);
			material.SetFloat("_RimSize", Mathf.Clamp01(size));
		}
	}

	public void SetRimOff()
	{
		if (activeMaterials == null) return;
		foreach (Material material in activeMaterials)
			if (material != null) material.SetFloat("_Rim", 0f);
	}
}
