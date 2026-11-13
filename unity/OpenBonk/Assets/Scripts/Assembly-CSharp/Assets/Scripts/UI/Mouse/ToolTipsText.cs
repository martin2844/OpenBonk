using System;
using System.Runtime.CompilerServices;
using TMPro;
using UnityEngine;

namespace Assets.Scripts.UI.Mouse
{
	public class ToolTipsText : MonoBehaviour
	{
		public delegate void HoverOnLinkEvent(string keyword, Vector3 mousePos);

		public delegate void CloseTooltipEvent();

		private TMP_Text tmpTextBox;

		private Canvas canvas;

		private UnityEngine.Camera camera;

		private RectTransform textBoxRectTransform;

		private int currentlyActiveLinkedElement = -1;

		public static Action<string, Vector2> A_OpenTooltip;

		public static Action<string> A_CloseTooltip;

		private float readyTime;

		private Vector3 lastPos;

		private bool hasVisibleMouse;
		private bool controllerLinkOpen;

		public static event HoverOnLinkEvent OnHoverOnLinkEvent;

		public static event CloseTooltipEvent OnCloseTooltipEvent;

		private void Awake()
		{
			tmpTextBox = GetComponent<TMP_Text>();
			canvas = GetComponentInParent<Canvas>();
			textBoxRectTransform = GetComponent<RectTransform>();
			camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
		}

		private void OnEnable()
		{
			readyTime = Time.time + 1f;
		}

		private void Update()
		{
			if (controllerLinkOpen) return;
			if (Time.time < readyTime) { Vector3 mouse = Input.mousePosition; lastPos = new Vector3(mouse.x, mouse.y, 0f); return; }
			CheckForLinkAtMousePosition();
		}

		private void CheckForLinkAtMousePosition()
		{
			CheckForLink(Input.mousePosition);
		}

		private void CheckForLink(Vector3 mouse)
		{
			mouse.z = 0f;
			if ((lastPos - mouse).sqrMagnitude < .1f || tmpTextBox == null || textBoxRectTransform == null) return;
			// Remember movement even outside a link. Otherwise returning to the
			// exact point that last opened a tooltip is mistaken for no movement.
			lastPos = mouse;
			int linkIndex = TMP_TextUtilities.IsIntersectingRectTransform(textBoxRectTransform, mouse, camera)
				? TMP_TextUtilities.FindIntersectingLink(tmpTextBox, mouse, camera) : -1;
			if (linkIndex == currentlyActiveLinkedElement) return;
			CloseCurrentLink();
			if (linkIndex < 0 || linkIndex >= tmpTextBox.textInfo.linkCount) return;
			TMP_LinkInfo link = tmpTextBox.textInfo.linkInfo[linkIndex];
			currentlyActiveLinkedElement = linkIndex;
			A_OpenTooltip?.Invoke(link.GetLinkID(), GetLinkPosition(link));
		}

		private Vector3 GetLinkPosition(TMP_LinkInfo linkInfo, float verticalOffset = 10f)
		{
			var info = tmpTextBox.textInfo;
			int firstIndex = linkInfo.linkTextfirstCharacterIndex;
			int lastIndex = firstIndex + linkInfo.linkTextLength - 1;
			if (linkInfo.linkTextLength <= 0 || firstIndex < 0 || lastIndex >= info.characterCount) return Vector3.zero;
			var first = info.characterInfo[firstIndex]; var last = info.characterInfo[lastIndex];
			Vector3 a = info.meshInfo[first.materialReferenceIndex].vertices[first.vertexIndex + 1];
			Vector3 b = info.meshInfo[last.materialReferenceIndex].vertices[last.vertexIndex + 2];
			Vector3 local = (a + b) * .5f; local.y += verticalOffset;
			return RectTransformUtility.WorldToScreenPoint(camera, textBoxRectTransform.TransformPoint(local));
		}

		private void OnDisable()
		{
			controllerLinkOpen = false;
			CloseCurrentLink();
		}

		// Android controller users cannot hover a word. The selected offer can
		// explicitly open a link while the mouse detector remains idle.
		public bool OpenControllerLink(int index)
		{
			if (tmpTextBox == null) tmpTextBox = GetComponent<TMP_Text>();
			if (tmpTextBox == null || !isActiveAndEnabled) return false;
			tmpTextBox.ForceMeshUpdate();
			if (index < 0 || index >= tmpTextBox.textInfo.linkCount) return false;
			CloseCurrentLink();
			controllerLinkOpen = true;
			currentlyActiveLinkedElement = index;
			var link = tmpTextBox.textInfo.linkInfo[index];
			A_OpenTooltip?.Invoke(link.GetLinkID(), GetLinkPosition(link));
			return true;
		}

		public void CloseControllerLink()
		{
			if (!controllerLinkOpen) return;
			controllerLinkOpen = false;
			CloseCurrentLink();
			lastPos = Input.mousePosition;
		}

		private void CloseCurrentLink()
		{
			int index = currentlyActiveLinkedElement;
			currentlyActiveLinkedElement = -1;
			if (index >= 0 && tmpTextBox != null && index < tmpTextBox.textInfo.linkCount)
				A_CloseTooltip?.Invoke(tmpTextBox.textInfo.linkInfo[index].GetLinkID());
		}
	}
}
