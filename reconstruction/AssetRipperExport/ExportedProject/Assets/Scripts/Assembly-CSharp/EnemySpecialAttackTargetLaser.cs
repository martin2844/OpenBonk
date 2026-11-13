using Assets.Scripts.Actors.Player;
using Assets.Scripts.Game.Combat.EnemySpecialAttacks.Implementations;
using Assets.Scripts.Managers;
using Assets.Scripts.Utility;
using UnityEngine;

public class EnemySpecialAttackTargetLaser : EnemySpecialAttackPrefab
{
    public Transform laser;
    public Transform laserEnd;
    private float speed;
    private float defaultMaxSpeed = 35f;
    private float maxSpeed = 35f;
    public Transform blackhole;
    private float maxLaserLength = 999f;
    private float laserLength;
    private float overAtTime;
    private float timeToMaxSpeed = 5f;
    private float speedTimer;
    private float damageCooldown = .1f;
    private float nextDamageReadyTime;

    protected override void Init()
    {
        maxSpeed = defaultMaxSpeed;
        int stage = MapController.GetStageIndex();
        if (stage == 1) maxSpeed *= 1.5f;
        else if (stage >= 2) maxSpeed *= 2f;
        speedTimer = 0f;
        speed = 0f;
        laser.position = GetLaserPosition();
        Vector3 center = enemy.GetCenterPosition();
        Vector3 playerPosition = MyPlayer.Instance.transform.position;
        Vector3 target = center + (playerPosition - center).normalized * Vector3.Distance(playerPosition, center) * .5f + Vector3.up * 500f;
        laser.LookAt(global::Utility.RaycastUtility.RayToGround(target, 9999f));
        overAtTime = MyTime.time + specialAttack.duration;
        blackhole.localScale = Vector3.zero;
    }

    private Vector3 GetLaserPosition()
    {
        return enemy.GetHeadPosition() + Vector3.up * (enemy.GetHeight() * .1f);
    }

    private void Update()
    {
        blackhole.localScale = Vector3.Lerp(blackhole.localScale, Vector3.one * 10f, MyTime.deltaTime * 2f);
    }

    private void FixedUpdate()
    {
        if (MyTime.time >= overAtTime)
        {
            ReturnToPool();
            gameObject.SetActive(false);
            return;
        }
        transform.position = GetLaserPosition();
        laser.position = GetLaserPosition();
        if (speedTimer < 1f)
        {
            speedTimer = Mathf.Clamp01(speedTimer + MyTime.fixedDeltaTime / timeToMaxSpeed);
            float t = Mathf.Clamp01(speedTimer * speedTimer * speedTimer);
            speed = maxSpeed * t;
            laserLength = maxLaserLength * t;
        }
        Quaternion target = Quaternion.LookRotation(MyPlayer.Instance.transform.position - laser.position);
        laser.rotation = Quaternion.RotateTowards(laser.rotation, target, speed * MyTime.fixedDeltaTime);
        float distance = Physics.Raycast(laser.position, laser.forward, out RaycastHit ground, laserLength, GameManager.Instance.whatIsGroundAndObjects) ? ground.distance : laserLength;
        laser.localScale = new Vector3(1f, 1f, distance);
        if (Physics.Raycast(laser.position, laser.forward, out RaycastHit playerHit, laser.localScale.z, GameManager.Instance.whatIsPlayer))
            laser.localScale = new Vector3(1f, 1f, playerHit.distance + .5f);
        laserEnd.position = laser.position + laser.forward * laser.localScale.z;
    }

    private void OnTriggerStay(Collider other)
    {
        if (nextDamageReadyTime > MyTime.time) return;
        nextDamageReadyTime = MyTime.time + damageCooldown;
        MyPlayer.Instance.inventory.playerHealth.DamagePlayerExternal(specialAttack.GetDamage(enemy), 0f, Vector3.up, false, "Enemy", GetDamageFlags());
    }
}
