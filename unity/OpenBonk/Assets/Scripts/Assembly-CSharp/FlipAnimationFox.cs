using UnityEngine;

public class FlipAnimationFox : MonoBehaviour
{
	public Animator animator;

	private bool isJumping;

	public Transform flipTransform;

	private float lastAnimationTime;

	private int jumpCount;

	public GameObject[] defaultHandhelds;

	public GameObject[] flippedHandhelds;

	private void Update()
	{
		if (animator == null) return;
		var state = animator.GetCurrentAnimatorStateInfo(0);
		if (state.IsName("Jump"))
		{
			float animationTime = state.normalizedTime;
			if (!isJumping)
			{
				isJumping = true;
				OnJumpAnimationStart();
			}
			else if (lastAnimationTime > animationTime)
			{
				OnJumpAnimationFinishOrInterrupted();
				OnJumpAnimationStart();
			}
			lastAnimationTime = animationTime;
		}
		else if (isJumping)
		{
			isJumping = false;
			OnJumpAnimationFinishOrInterrupted();
		}
	}

	private void OnJumpAnimationStart()
	{
		jumpCount++;
		bool flip = jumpCount % 2 == 0;
		if (flipTransform != null) flipTransform.localScale = flip ? new Vector3(-1f, 1f, 1f) : Vector3.one;
		FlipHandhelds(flip);
	}

	private void OnJumpAnimationFinishOrInterrupted()
	{
		if (flipTransform != null) flipTransform.localScale = Vector3.one;
		FlipHandhelds(false);
	}

	private void FlipHandhelds(bool flip)
	{
		if (defaultHandhelds != null) foreach (var item in defaultHandhelds) if (item != null) item.SetActive(!flip);
		if (flippedHandhelds != null) foreach (var item in flippedHandhelds) if (item != null) item.SetActive(flip);
	}
}
