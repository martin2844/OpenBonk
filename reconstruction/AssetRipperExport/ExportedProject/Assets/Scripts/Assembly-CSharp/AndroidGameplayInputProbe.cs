#if UNITY_ANDROID && !UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory.Stats;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Managers;
using Assets.Scripts.MapGeneration;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Utility;
using UnityEngine;

// Opt-in device diagnostic; normal gameplay does not create this component.
public sealed class AndroidGameplayInputProbe : MonoBehaviour
{
    public static bool Active { get; private set; }
    private MyPlayer player;
    private bool traceFixedMovement;
    private int fixedSamples;
    private int neutralSamples;
    private int releaseSamples;
    private static readonly FieldInfo InputVerticalField = typeof(PlayerInput).GetField("moveVertical", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo MovementVerticalField = typeof(PlayerMovement).GetField("y", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo LastVelocityField = typeof(PlayerMovement).GetField("lastVelocity", BindingFlags.Instance | BindingFlags.NonPublic);
    public static void Begin(MyPlayer player)
    {
        var go = new GameObject("Android gameplay input probe");
        go.AddComponent<AndroidGameplayInputProbe>().player = player;
    }
    private IEnumerator Start()
    {
        Active = true;
        bool triggerProbe;
        bool clearExisting;
        bool axisResponse;
        using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
        using (var intent = activity.Call<AndroidJavaObject>("getIntent"))
        {
            triggerProbe = intent.Call<bool>("getBooleanExtra", "megabonk_gameplay_triggers_probe", false);
            traceFixedMovement = intent.Call<bool>("getBooleanExtra", "megabonk_fixed_movement_probe", false);
            clearExisting = intent.Call<bool>("getBooleanExtra", "megabonk_clear_enemies_probe", false);
            axisResponse = intent.Call<bool>("getBooleanExtra", "megabonk_axis_response_probe", false);
        }
        int oldHoldAim = SaveManager.Instance.config.cfControlSettings.hold_aim;
        if (triggerProbe)
        {
            SaveManager.Instance.config.cfControlSettings.hold_aim = 1;
            player.inventory.weaponInventory.AddWeapon(DataManager.Instance.GetWeapon(EWeapon.Sniper), null);
        }
        var manager = EnemyManager.Instance; bool oldWaves = manager.enabledWaves; manager.enabledWaves = false;
        if (clearExisting)
        {
            var existing = new List<Assets.Scripts.Actors.Enemies.Enemy>(manager.enemies.Values);
            Debug.Log($"Gameplay input probe: clearing {existing.Count} existing enemies for movement isolation");
            foreach (var enemy in existing) if (enemy != null && enemy.gameObject.activeInHierarchy) enemy.ReleaseToPool();
        }
        var disabled = new List<WeaponBase>();
        foreach (var weapon in player.inventory.weaponInventory.weapons.Values) if (weapon.enabled) { weapon.Disable(); disabled.Add(weapon); }
        var body = player.playerMovement.GetRb();
        var groundField = typeof(PlayerMovement).GetField("groundedObject", BindingFlags.NonPublic | BindingFlags.Instance);
        var skating = player.GetComponentInChildren<SkatingAnimations>(true);
        if (player.character == ECharacter.Calcium)
            Debug.Log($"Calcium animation probe: found={(skating != null)}, animator={(skating != null && skating.animator != null)}, audio={(skating != null && skating.skateSfx != null)}");
        var invincible = typeof(MyPlayer).GetField("isInvincible", BindingFlags.NonPublic | BindingFlags.Instance);
        var invincibleUntil = typeof(MyPlayer).GetField("damageCooldownOverAtTime", BindingFlags.NonPublic | BindingFlags.Instance);
        var origin = body.position;
		float previousSampleTime = Time.realtimeSinceStartup;
		Vector3 previousSamplePosition = origin;
		Debug.Log($"Gameplay input probe: ready pos={origin}, grounded={player.playerMovement.grounded}, yaw={player.playerInput.cameraRotation.y}, paused={MyTime.paused}, moveStat={PlayerStats.GetStat(EStat.MoveSpeedMultiplier):F3}, maxSpeed={player.playerMovement.movementValues.GetMaxSpeed():F3}, bodyMass={body.mass:F3}, fixedDelta={Time.fixedDeltaTime:F5}");
        bool lastPaused = MyTime.paused;
        bool previousMapButton = false, previousMapVisible = false, previousAimButton = false, previousAiming = false;
        var map = FindObjectOfType<FullMap>(true);
        float end = Time.realtimeSinceStartup + 120f;
        while (Time.realtimeSinceStartup < end && player != null)
        {
			float sampleTime = Time.realtimeSinceStartup;
			float sampleDuration = sampleTime - previousSampleTime;
			Vector3 sampleDelta = body.position - previousSamplePosition;
            invincible.SetValue(player, true); invincibleUntil.SetValue(player, float.MaxValue);
            float x = MyInputManager.GetAxis(MyInputManager.MoveHorizontal), y = MyInputManager.GetAxis(MyInputManager.MoveVertical);
            if (axisResponse) Debug.Log($"Axis response probe: x={x:F6}, y={y:F6}, real={sampleTime:F6}");
            float lookX = MyInputManager.GetAxis(MyInputManager.LookHorizontal), lookY = MyInputManager.GetAxis(MyInputManager.LookVertical);
            bool jump = MyInputManager.GetButton(MyInputManager.Jump), slide = MyInputManager.GetButton(MyInputManager.Slide);
            if (skating != null && skating.animator != null && skating.skateSfx != null)
                Debug.Log($"Calcium animation probe: input={x:F2},{y:F2}, speed={body.velocity.magnitude:F2}, ground={player.playerMovement.IsTouchingGround()}, idle={skating.animator.GetBool("idle")}, kicking={skating.animator.GetBool("kicking")}, audioPlaying={skating.skateSfx.isPlaying}, audioVolume={skating.skateSfx.volume:F3}");
            if (triggerProbe)
            {
                bool mapButton = MyInputManager.GetButton(MyInputManager.MapOverlay);
                bool mapVisible = map != null && map.mapDisplayTransform != null && map.mapDisplayTransform.gameObject.activeInHierarchy;
                bool aimButton = MyInputManager.GetButton(MyInputManager.Aim);
                bool aiming = player.playerInput.IsAiming();
                if (mapButton != previousMapButton || mapVisible != previousMapVisible || aimButton != previousAimButton || aiming != previousAiming)
                {
                    Debug.Log($"Gameplay trigger probe: mapButton={mapButton}, mapVisible={mapVisible}, aimButton={aimButton}, aiming={aiming}, aimable={player.inventory.weaponInventory.HasAimableWeapon()}, paused={MyTime.paused}");
                    previousMapButton = mapButton; previousMapVisible = mapVisible; previousAimButton = aimButton; previousAiming = aiming;
                }
            }
            if (lastPaused != MyTime.paused)
            {
                lastPaused = MyTime.paused;
                var pause = UiManager.Instance != null ? UiManager.Instance.pause : null;
                Debug.Log($"Gameplay input probe pause: paused={MyTime.paused}, menu={(pause != null && pause.gameObject.activeInHierarchy)}, main={(pause != null && pause.main.activeInHierarchy)}");
            }
            if (Mathf.Abs(x) > .1f || Mathf.Abs(y) > .1f || Mathf.Abs(lookX) > .1f || Mathf.Abs(lookY) > .1f || jump || slide || Mathf.Abs(body.velocity.y) > 2f)
			{
                var ground = groundField.GetValue(player.playerMovement) as GameObject;
                var lastGround = player.playerMovement.lastGroundedPosition;
                bool rayHit = Physics.Raycast(body.position, Vector3.down, out var surface, 5f, player.playerMovement.whatIsGround);
                Debug.Log($"Ground diagnostic: collider={(ground != null ? ground.name : "none")}, lastGround={lastGround}, mapLower={MapInfo.mapBoundsLower}, rayHit={rayHit}, rayCollider={(rayHit ? surface.collider.name : "none")}, rayNormal={(rayHit ? surface.normal : Vector3.zero)}");
				Debug.Log($"Gameplay input probe: x={x:F2}, y={y:F2}, lookX={lookX:F2}, lookY={lookY:F2}, jump={jump}, slide={slide}, pos={body.position}, velocity={body.velocity}, horizontalSpeed={new Vector2(body.velocity.x, body.velocity.z).magnitude:F3}, elapsedReal={sampleTime:F3}, sampleSeconds={sampleDuration:F3}, sampleHorizontalDistance={new Vector2(sampleDelta.x, sampleDelta.z).magnitude:F3}, moveStat={PlayerStats.GetStat(EStat.MoveSpeedMultiplier):F3}, maxSpeed={player.playerMovement.movementValues.GetMaxSpeed():F3}, grounded={player.playerMovement.grounded}, crouch={player.playerMovement.crouchState}, yaw={player.playerInput.cameraRotation.y:F1}, paused={MyTime.paused}");
            }
			previousSampleTime = sampleTime;
			previousSamplePosition = body.position;
            yield return new WaitForSecondsRealtime(.2f);
        }
        if (player != null) { invincible.SetValue(player, false); invincibleUntil.SetValue(player, 0f); }
        if (triggerProbe && SaveManager.Instance != null) SaveManager.Instance.config.cfControlSettings.hold_aim = oldHoldAim;
        manager.enabledWaves = oldWaves; foreach (var weapon in disabled) weapon.Enable();
        Debug.Log($"Gameplay input probe: finished moved={Vector3.Distance(origin, body.position):F2}, pos={body.position}");
        Active = false;
        Destroy(gameObject);
    }
    private void Update()
    {
        if (!Input.GetKeyDown(KeyCode.JoystickButton10)) return;
        var pause = UiManager.Instance != null ? UiManager.Instance.pause : null;
        Debug.Log($"Gameplay input probe start key: abort={MyInputManager.GetButtonDown(MyInputManager.UIAbort)}, paused={MyTime.paused}, menu={(pause != null && pause.gameObject.activeInHierarchy)}, managerEnabled={(UiManager.Instance != null && UiManager.Instance.isActiveAndEnabled)}");
    }

    private void FixedUpdate()
    {
        if (!traceFixedMovement || fixedSamples >= 600 || player == null || player.playerMovement == null || player.playerInput == null || MyTime.paused) return;
        float x = MyInputManager.GetAxis(MyInputManager.MoveHorizontal);
        float y = MyInputManager.GetAxis(MyInputManager.MoveVertical);
        var body = player.playerMovement.GetRb();
        if (body == null) return;
        var p = body.position;
        var v = body.velocity;
        if (Mathf.Abs(x) < .1f && Mathf.Abs(y) < .1f)
        {
            if (fixedSamples == 0 && neutralSamples++ % 10 == 0)
                Debug.Log($"Neutral movement probe: fixed={Time.fixedTime:F6}, input={x:F6},{y:F6}, grounded={player.playerMovement.grounded}, pos={p.x:F6},{p.y:F6},{p.z:F6}, vel={v.x:F6},{v.y:F6},{v.z:F6}");
            if (fixedSamples > 0 && releaseSamples < 40)
            {
                var cached = LastVelocityField != null ? (Vector3)LastVelocityField.GetValue(player.playerMovement) : new Vector3(float.NaN, float.NaN, float.NaN);
                float sampledInput = InputVerticalField != null ? (float)InputVerticalField.GetValue(player.playerInput) : float.NaN;
                float movementInput = MovementVerticalField != null ? (float)MovementVerticalField.GetValue(player.playerMovement) : float.NaN;
                Debug.Log($"Release movement probe: n={releaseSamples++}, frame={Time.frameCount}, real={Time.realtimeSinceStartup:F6}, fixed={Time.fixedTime:F6}, dt={Time.fixedDeltaTime:F6}, input={x:F6},{y:F6}, sampledY={sampledInput:F6}, movementY={movementInput:F6}, grounded={player.playerMovement.grounded}, crouch={player.playerMovement.crouchState}, pos={p.x:F6},{p.y:F6},{p.z:F6}, vel={v.x:F6},{v.y:F6},{v.z:F6}, cached={cached.x:F6},{cached.y:F6},{cached.z:F6}");
            }
            return;
        }
        Debug.Log($"Fixed movement probe: n={fixedSamples++}, real={Time.realtimeSinceStartup:F6}, fixed={Time.fixedTime:F6}, dt={Time.fixedDeltaTime:F6}, input={x:F6},{y:F6}, grounded={player.playerMovement.grounded}, crouch={player.playerMovement.crouchState}, yaw={player.playerInput.cameraRotation.y:F2}, pos={p.x:F6},{p.y:F6},{p.z:F6}, vel={v.x:F6},{v.y:F6},{v.z:F6}, max={player.playerMovement.movementValues.GetMaxSpeed():F6}");
    }
}
#endif
