using UnityEngine;
using Assets.Scripts.Utility;

// Presentation only; the movement system remains authoritative.
public sealed class OpenBonkExternalAnimator : MonoBehaviour
{
    private Animator animator;
    private AnimationClip idle, walk, run, jump, surf, current;
    private GameObject surfboard;
    private bool moving, grounded = true, jumping, surfing;
    private float speed, inputMagnitude = 1f, time;
    public void Initialize(Animator target, OpenBonkExternalCharacterData data)
    {
        animator = target; idle = data.idleAnimation; walk = data.walkAnimation; run = data.runAnimation; jump = data.jumpAnimation; surf = data.surfAnimation;
        if (!string.IsNullOrEmpty(data.surfboard)) foreach (Transform part in GetComponentsInChildren<Transform>(true)) if (part.name == data.surfboard) { surfboard = part.gameObject; break; }
        if (surfboard != null) surfboard.SetActive(false);
        time = 0f; moving = false; current = null;
        if (animator != null && walk != null) walk.SampleAnimation(gameObject, 0f);
    }
    public void SetMoving(bool value) { moving = value; }
    public void SetMovementState(bool isGrounded, bool isJumping, bool isSurfing, float velocity, float movementInput = 1f)
    { grounded = isGrounded; jumping = isJumping; surfing = isSurfing; speed = velocity; inputMagnitude = movementInput; }
    private void Update()
    {
        if (animator == null || animator.speed <= 0f || MyTime.paused) return;
        bool showSurf = surfing && surf != null;
        if (surfboard != null && surfboard.activeSelf != showSurf) surfboard.SetActive(showSurf);
        AnimationClip next = showSurf ? surf : ((!grounded || jumping) && jump != null ? jump : (moving ? (run != null && speed > 8f && inputMagnitude >= .5f ? run : walk) : (idle != null ? idle : walk)));
        if (next == null || next.length <= 0f) return;
        if (next != current) { current = next; time = 0f; next.SampleAnimation(gameObject, 0f); }
        if (!moving && next == walk) return;
        time = (time + Time.deltaTime * MyTime.timeScale * animator.speed) % next.length;
        next.SampleAnimation(gameObject, time);
    }
}
