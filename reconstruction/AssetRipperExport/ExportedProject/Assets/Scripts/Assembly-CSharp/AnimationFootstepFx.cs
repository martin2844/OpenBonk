using Assets.Scripts.Objects.Pooling;
using UnityEngine;

public class AnimationFootstepFx : MonoBehaviour
{
    public Transform leftFoot;
    public Transform rightFoot;

    public void RightFoot() { if (rightFoot != null) Spawn(rightFoot.position); }
    public void LeftFoot() { if (leftFoot != null) Spawn(leftFoot.position); }

    private void Spawn(Vector3 position)
    {
        if (PoolManager.Instance == null || PoolManager.Instance.footstepPool == null) return;
        GameObject effect = PoolManager.Instance.footstepPool.Get();
        if (effect == null) return;
        effect.transform.position = position;
        effect.transform.rotation = Quaternion.LookRotation(-transform.forward);
    }
}
