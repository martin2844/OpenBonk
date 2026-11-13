using System;
using TMPro;
using UnityEngine;
using UnityEngine.UIElements.Experimental;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Utility;

namespace Assets.Scripts.Inventory__Items__Pickups.Interactables
{
	public class DetectInteractables : MonoBehaviour
	{
		private float interactableRange = 5f;

		public LayerMask whatIsInteractable;

		private BaseInteractable currentInteractable;

		public TextMeshProUGUI t_interact;

		public Transform uiParent;

		public MyGlyphDisplay glyphContainer;

		private string tagInteractable = "Interactable";

		public static Action<BaseInteractable, bool> A_Interacted;

		private float animationTime;

		private float animateOverTime = 0.3f;

		private Vector3 fromScale;

		private Vector3 toScale;

		private void Awake()
		{
			if (uiParent != null && uiParent.parent != null) uiParent.parent.SetParent(null, false);
		}

		private void FixedUpdate()
		{
			BaseInteractable nearest = null;
			float distance = float.PositiveInfinity;
			Vector3 position = transform.position;
			foreach (Collider hit in Physics.OverlapSphere(position, interactableRange, whatIsInteractable))
			{
				if (!hit.CompareTag(tagInteractable)) continue;
				float candidateDistance = (hit.ClosestPoint(position) - position).sqrMagnitude;
				if (candidateDistance >= distance) continue;
				BaseInteractable candidate = hit.GetComponent<BaseInteractable>();
				if (candidate == null || !candidate.CanInteract()) continue;
				nearest = candidate;
				distance = candidateDistance;
			}
			if (nearest == currentInteractable) return;
			StopHovering();
			if (nearest != null) StartHovering(nearest.gameObject);
		}

		public void TryInteract()
		{
			if (!CanInteract() || currentInteractable == null) return;
			bool result = currentInteractable.Interact();
			if (currentInteractable == null) StopHovering();
			A_Interacted?.Invoke(currentInteractable, result);
		}

		private bool CanInteract()
		{
			return UiManager.Instance != null && UiManager.Instance.encounterWindows != null &&
				!UiManager.Instance.encounterWindows.encounterInProgress &&
				MyPlayer.Instance != null && !MyPlayer.Instance.isTeleporting;
		}

		private void Update()
		{
			if (uiParent == null) return;
			if (animationTime < animateOverTime)
			{
				animationTime = Mathf.Clamp(animationTime + MyTime.deltaTime, 0f, animateOverTime);
				if (animationTime >= animateOverTime && currentInteractable == null)
					uiParent.gameObject.SetActive(false);
			}
			if (currentInteractable != null)
				uiParent.position = currentInteractable.transform.position + currentInteractable.GetOffset() + Vector3.up * 3f;
			float progress = animateOverTime > 0f ? animationTime / animateOverTime : 1f;
			uiParent.localScale = Vector3.Lerp(fromScale, toScale, Easing.InOutCirc(progress));
		}

		private void StartHovering(GameObject newObject)
		{
			currentInteractable = newObject.GetComponent<BaseInteractable>();
			if (currentInteractable == null) return;
			currentInteractable.StartHover(this);
			RefreshCurrentInteractable();
			if (uiParent != null)
			{
				animationTime = 0f;
				uiParent.gameObject.SetActive(true);
				uiParent.localScale = Vector3.zero;
				fromScale = Vector3.zero;
				toScale = Vector3.one;
			}
		}

		public void RefreshCurrentInteractable()
		{
			if (currentInteractable == null) return;
			if (t_interact != null) { t_interact.text = currentInteractable.GetInteractString(); t_interact.color = currentInteractable.GetColor(); }
			RefreshGlyphContainer();
		}

		private void StopHovering()
		{
			if (uiParent != null && uiParent.gameObject.activeSelf)
			{
				fromScale = uiParent.localScale;
				toScale = Vector3.zero;
				animationTime = 0f;
			}
			if (currentInteractable != null) currentInteractable.StopHover();
			currentInteractable = null;
		}

		public void InteractableDestroyed()
		{
			StopHovering();
		}

		private void RefreshGlyphContainer()
		{
			if (glyphContainer != null) glyphContainer.SetAction("Interact");
		}
	}
}
