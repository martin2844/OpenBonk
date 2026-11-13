#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using System.Reflection;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Game.Combat;
using UnityEngine;

// Test-only: observes the actual spawned player after a diagnostic selection.
internal sealed class AndroidExternalGameplayProbe : MonoBehaviour
{
    internal PlayerInventory inventory;
    private int primary, chained, badElement, sameDamagePairs;
    private float previousPrimary = -1f;
    private float minDamage = float.MaxValue, maxDamage;
    private IEnumerator Start()
    {
        var player = MyPlayer.Instance;
        var expectedCharacter = (ECharacter)OpenBonkContentIds.Character("pikachu-test", "pikachu");
        var expectedWeapon = (EWeapon)OpenBonkContentIds.Weapon("pikachu-test", "thunderbolt");
        if (player == null || player.character != expectedCharacter || inventory == null || !inventory.weaponInventory.weapons.ContainsKey(expectedWeapon) || player.playerRenderer == null || player.playerRenderer.renderer == null)
        { Debug.LogError("External gameplay probe FAIL: missing player, model or test weapon"); Destroy(gameObject); yield break; }
        var actor = player.playerRenderer;
        Enemy.A_Damage += OnDamage;
        OpenBonkExternalAnimator animation = null;
        float readyDeadline = Time.realtimeSinceStartup + 8f;
        while (Time.realtimeSinceStartup < readyDeadline && animation == null)
        {
            animation = actor.GetComponentInChildren<OpenBonkExternalAnimator>(true);
            if (animation == null) yield return null;
        }
        var field = typeof(OpenBonkExternalAnimator).GetField("time", BindingFlags.Instance | BindingFlags.NonPublic);
        if (animation == null || field == null) { Debug.LogError($"External gameplay probe FAIL: animator={animation != null} timeField={field != null} renderer={actor.renderer.name} active={actor.gameObject.activeInHierarchy}"); Destroy(gameObject); yield break; }
        float playDeadline = Time.realtimeSinceStartup + 30f;
        while (Time.realtimeSinceStartup < playDeadline && (GameManager.Instance == null || !GameManager.Instance.IsPlaying())) yield return null;
        if (GameManager.Instance == null || !GameManager.Instance.IsPlaying()) { Debug.LogError("External gameplay probe INCONCLUSIVE: gameplay did not start"); Destroy(gameObject); yield break; }
        Vector3 origin = player.transform.position; float maxDistance = 0f, last = (float)field.GetValue(animation); int animatedFrames = 0;
        Debug.Log("External gameplay probe READY: registered character, imported model and Lua thunderbolt present");
        float deadline = Time.realtimeSinceStartup + 18f;
        while (Time.realtimeSinceStartup < deadline && player != null && animation != null)
        {
            yield return null;
            float current = (float)field.GetValue(animation);
            if (Mathf.Abs(current-last) > .00001f) animatedFrames++;
            last = current; maxDistance = Mathf.Max(maxDistance, Vector3.Distance(origin, player.transform.position));
        }
        Debug.Log($"External gameplay probe {(maxDistance > .1f && animatedFrames > 0 ? "PASS" : "INCONCLUSIVE")}: maxDistance={maxDistance:F3} animatedFrames={animatedFrames}");
        float ratio = minDamage > 0f && minDamage < float.MaxValue ? maxDamage / minDamage : 0f;
        Debug.Log($"Lua thunderbolt probe {(primary >= 2 && ratio > 1.4f && ratio < 1.6f && badElement == 0 ? "PASS" : "INCONCLUSIVE")}: primary={primary} chained={chained} min={minDamage:F2} max={maxDamage:F2} ratio={ratio:F3} badElement={badElement}");
        Debug.Log($"Lua thunderbolt primary sequence: consecutiveEqual={sameDamagePairs} hits={primary}");
        Destroy(gameObject);
    }
    private void OnDamage(Enemy enemy, DamageContainer damage)
    {
        if (damage == null || inventory?.characterData?.weapon == null || damage.damageSource != inventory.characterData.weapon.damageSourceName) return;
        if (damage.element != EElement.Lightning) badElement++;
        if (Mathf.Abs(damage.procCoefficient - .5f) < .001f) chained++;
        else if (Mathf.Abs(damage.procCoefficient - 1f) < .001f && !damage.crit)
        { if (Mathf.Abs(previousPrimary - damage.damage) < .001f) sameDamagePairs++; previousPrimary = damage.damage; primary++; minDamage = Mathf.Min(minDamage, damage.damage); maxDamage = Mathf.Max(maxDamage, damage.damage); }
    }
    private void OnDestroy() { Enemy.A_Damage -= OnDamage; }
}
#endif
