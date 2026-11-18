using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Managers;
using Assets.Scripts.Utility;
using UnityEngine;

internal sealed class OpenBonkBossHazard
{
    private readonly Enemy enemy;
    private readonly bool sweep;
    private readonly Material material;
    private LineRenderer warning;
    private Vector3 center, axis;
    private float nextAttack = MyTime.time + 2f;
    private const float Windup = 1.35f;

    public OpenBonkBossHazard(Enemy enemy, bool sweep, Material material)
    {
        this.enemy = enemy;
        this.sweep = sweep;
        this.material = material;
    }

    public void Step()
    {
        var player = MyPlayer.Instance;
        if (player == null || player.IsDead() || player.isTeleporting) { Cancel(); return; }
        float now = MyTime.time;
        Vector3 feet = player.playerMovement.GetRbFeetPosition();
        if (warning == null)
        {
            if (now < nextAttack) return;
            nextAttack = now + .4f;
            Vector3 heading = feet - enemy.transform.position;
            heading.y = 0f;
            if (heading.sqrMagnitude < .1f || heading.sqrMagnitude > 26f * 26f ||
                !Physics.Raycast(feet + Vector3.up * .6f, Vector3.down, out var floor, 2.5f,
                    GameManager.Instance.whatIsGround, QueryTriggerInteraction.Ignore)) return;
            center = floor.point;
            axis = Vector3.Cross(heading.normalized, Vector3.up);
            ShowWarning();
            nextAttack = now + Windup;
            return;
        }

        var color = sweep ? new Color(.22f, .96f, .92f) : new Color(1f, .56f, .14f);
        color.a = Mathf.Lerp(.42f, .94f, 1f - Mathf.Clamp01((nextAttack - now) / Windup));
        warning.startColor = warning.endColor = color;
        if (now < nextAttack) return;

        Vector3 offset = feet - center;
        bool closeToFloor = Mathf.Abs(offset.y) < 2.4f;
        offset.y = 0f;
        bool inside = sweep
            ? Mathf.Abs(Vector3.Dot(offset, axis)) < 9f && Mathf.Abs(Vector3.Dot(offset, Vector3.Cross(axis, Vector3.up))) < 2.05f
            : offset.sqrMagnitude < 4.7f * 4.7f;
        if (closeToFloor && inside) player.inventory.playerHealth.DamagePlayer(enemy, offset.normalized);
        Cancel();
    }

    public void Cancel()
    {
        if (warning == null) return;
        Object.Destroy(warning.gameObject);
        warning = null;
        nextAttack = MyTime.time + (sweep ? 4.1f : 3.2f);
    }

    private void ShowWarning()
    {
        var root = new GameObject(sweep ? "Sweep warning" : "Pulse warning");
        warning = root.AddComponent<LineRenderer>();
        warning.sharedMaterial = material;
        warning.loop = true;
        warning.widthMultiplier = .24f;
        warning.startColor = warning.endColor = sweep ? Color.cyan : new Color(1f, .56f, .14f);
        warning.positionCount = sweep ? 4 : 32;
        Vector3 perpendicular = Vector3.Cross(axis, Vector3.up);
        for (int i = 0; i < warning.positionCount; i++)
        {
            float angle = (i + .5f) * 2f * Mathf.PI / warning.positionCount;
            Vector3 offset = sweep
                ? axis * (Mathf.Sign(Mathf.Cos(angle)) * 9f) + perpendicular * (Mathf.Sign(Mathf.Sin(angle)) * 2.05f)
                : new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * 4.7f;
            warning.SetPosition(i, center + offset + Vector3.up * .17f);
        }
    }
}
