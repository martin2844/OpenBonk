using Assets.Scripts.Actors.Player;
using Assets.Scripts.Game.Combat.EnemySpecialAttacks.Implementations;
using Assets.Scripts.Managers;
using Assets.Scripts.Utility;
using UnityEngine;

public class EnemySpecialAttackSpinningLasers : EnemySpecialAttackPrefab
{
    public Rigidbody rb;
    public Transform disk;
    public Transform laserParent;
    private Transform[] lasers;
    private float overAtTime;
    private float startedAtTime;
    private float laserLength = 250f;
    private float maxLaserLength = 250f;
    private Vector3 defaultScale;
    private float defaultVolume;
    public AudioSource audio;
    private float audioFadeTime = 1f;
    private float damageCooldown = .1f;
    private float nextDamageReadyTime;
    public float defaultSpinSpeed;
    public float diskRotationSpeed;
    private float spinSpeed;
    private float spinAngle;
    private float spinPhaseOffset;

    protected override void Init()
    {
        if (defaultScale == Vector3.zero) OnceInit();
        startedAtTime = MyTime.time;
        overAtTime = MyTime.time + specialAttack.duration;
        nextDamageReadyTime = MyTime.time + .75f;
        laserLength = 1f;
        audio.volume = 0f;
        spinSpeed = defaultSpinSpeed;
        int stage = MapController.GetStageIndex();
        if (stage > 0) spinSpeed *= 1f + stage * .4f;
    }

    private void OnceInit()
    {
        defaultScale = transform.localScale;
        defaultVolume = audio.volume;
        lasers = new Transform[laserParent.childCount];
        for (int i = 0; i < lasers.Length; i++) lasers[i] = laserParent.GetChild(i);
    }

    private void FixedUpdate()
    {
        if (MyTime.time >= overAtTime)
        {
            ReturnToPool();
            gameObject.SetActive(false);
            return;
        }
        if (MyTime.time - startedAtTime < audioFadeTime)
        {
            float t = (MyTime.time - startedAtTime) / audioFadeTime;
            audio.volume = Mathf.Lerp(0f, defaultVolume, t);
            laserLength = Mathf.Lerp(0f, maxLaserLength, 1f - Mathf.Sqrt(1f - t * t));
        }
        else if (MyTime.time >= overAtTime - audioFadeTime)
        {
            float t = (MyTime.time - (overAtTime - audioFadeTime)) / audioFadeTime;
            audio.volume = Mathf.Lerp(defaultVolume, 0f, t);
            laserLength = Mathf.Lerp(maxLaserLength, 0f, t);
        }
        else { audio.volume = defaultVolume; laserLength = maxLaserLength; }
        rb.MovePosition(enemy.GetCenterPosition() + Vector3.down * (enemy.GetHeight() * .15f));
        RotationStuff();
        foreach (Transform beam in lasers)
        {
            float distance = Physics.Raycast(beam.position, beam.forward, out RaycastHit hit, laserLength, GameManager.Instance.whatIsGroundAndObjects) ? hit.distance : laserLength;
            beam.localScale = new Vector3(1f, 1f, distance);
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (nextDamageReadyTime > MyTime.time) return;
        nextDamageReadyTime = MyTime.time + damageCooldown;
        MyPlayer.Instance.inventory.playerHealth.DamagePlayerExternal(specialAttack.GetDamage(enemy), 0f, Vector3.up, false, "Enemy", GetDamageFlags());
    }

    private void RotationStuff()
    {
        Quaternion target = Quaternion.LookRotation(MyPlayer.Instance.transform.position - disk.position, Vector3.up);
        disk.rotation = Quaternion.Slerp(disk.rotation, target, diskRotationSpeed * MyTime.fixedDeltaTime);
        spinAngle = Mathf.Repeat(spinAngle + spinSpeed * MyTime.fixedDeltaTime, 360f);
        Vector3 up = disk.up;
        Vector3 projected = Vector3.ProjectOnPlane(Vector3.right, up);
        if (projected.sqrMagnitude < .000001f) projected = Vector3.ProjectOnPlane(Vector3.forward, up);
        Vector3 forward = Vector3.Cross(up, projected.normalized);
        Quaternion baseRotation = Quaternion.LookRotation(forward, up);
        Quaternion spin = Quaternion.AngleAxis(spinAngle + spinPhaseOffset, up);
        laserParent.SetPositionAndRotation(disk.position, spin * baseRotation);
    }
}
