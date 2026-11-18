using Actors.Enemies;
using Assets.Scripts.Game.Combat.EnemyDebuffs;
using Assets.Scripts.Managers;
using Assets.Scripts.Utility;
using UnityEngine;

internal sealed class OpenBonkEnemyCharge
{
    private enum Phase { Ready, Windup, Rush, Recover }
    private const float WindupSeconds = .58f;
    private const float RushSeconds = .48f;
    private const float RushSpeed = 15f;
    private Phase phase;
    private Vector3 direction;
    private float until = MyTime.time + 1.5f;

    public bool Step(EnemyMovementRb movement)
    {
        var enemy = movement.enemy;
        float now = MyTime.time;
        if (movement.state != EnemyMovementRb.State.Normal || enemy.debuffs.ContainsKey(EDebuff.Stun) ||
            enemy.debuffs.ContainsKey(EDebuff.Freeze))
        {
            if (phase != Phase.Ready)
            {
                phase = Phase.Ready;
                until = now + 1f;
                enemy.animatedMesh.SelectClip("walk");
            }
            return false;
        }

        if (phase == Phase.Ready)
        {
            if (now < until || !movement.grounded || enemy.enemyMovement.distanceToTarget < 4f ||
                enemy.enemyMovement.distanceToTarget > 10f) return false;
            until = now + .3f;
            var heading = enemy.target.position - movement.rb.position;
            heading.y = 0;
            if (heading.sqrMagnitude < 1f) return false;
            direction = heading.normalized;
            if (!HasRoom(movement, RushSpeed * RushSeconds + enemy.collider.radius)) return false;
            phase = Phase.Windup;
            until = now + WindupSeconds;
            enemy.animatedMesh.SelectClip("charge");
        }
        else if (phase == Phase.Windup && now >= until)
        {
            phase = Phase.Rush;
            until = now + RushSeconds;
        }
        else if (phase == Phase.Rush && (now >= until || !HasRoom(movement, enemy.collider.radius + .8f)))
        {
            phase = Phase.Recover;
            until = now + .4f;
            enemy.animatedMesh.SelectClip("idle");
        }
        else if (phase == Phase.Recover && now >= until)
        {
            phase = Phase.Ready;
            until = now + 2f + (enemy.id % 7) * .12f;
            enemy.animatedMesh.SelectClip("walk");
            return false;
        }

        var body = movement.rb;
        Vector3 velocity = phase == Phase.Rush ? direction * RushSpeed * MyTime.timeScale : Vector3.zero;
        velocity.y = body.velocity.y;
        body.velocity = velocity;
        body.MoveRotation(Quaternion.LookRotation(direction));
        return true;
    }

    private bool HasRoom(EnemyMovementRb movement, float distance)
    {
        var bottom = movement.enemy.GetBottomPosition();
        var manager = GameManager.Instance;
        if (Physics.Raycast(bottom + Vector3.up * .65f, direction, distance, manager.whatIsGroundAndObjects,
            QueryTriggerInteraction.Ignore)) return false;
        int samples = Mathf.CeilToInt(distance / 1.25f);
        for (int i = 1; i <= samples; i++)
        {
            var candidate = bottom + direction * (distance * i / samples) + Vector3.up * 2.5f;
            if (!Physics.Raycast(candidate, Vector3.down, out var hit, 4f, manager.whatIsGround,
                QueryTriggerInteraction.Ignore) || Vector3.Angle(hit.normal, Vector3.up) > 35f)
                return false;
        }
        return true;
    }
}
