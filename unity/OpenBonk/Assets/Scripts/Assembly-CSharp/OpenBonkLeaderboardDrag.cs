using System;
using UnityEngine;
using UnityEngine.EventSystems;

// Turns drags and mouse-wheel input on the leaderboard rows into a distance in
// canvas units; LeaderboardUiNew maps it to whole rows.
internal sealed class OpenBonkLeaderboardDrag : MonoBehaviour, IInitializePotentialDragHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IScrollHandler
{
    internal Action<float> moved;

    private Canvas canvas;

    public void OnInitializePotentialDrag(PointerEventData eventData) => eventData.useDragThreshold = true;

    public void OnBeginDrag(PointerEventData eventData) { }

    // Dragging the finger up reveals lower ranks.
    public void OnDrag(PointerEventData eventData) => moved?.Invoke(eventData.delta.y / Scale());

    public void OnEndDrag(PointerEventData eventData) { }

    public void OnScroll(PointerEventData eventData) => moved?.Invoke(-eventData.scrollDelta.y * 40f);

    private float Scale()
    {
        if (canvas == null) canvas = GetComponentInParent<Canvas>();
        float scale = canvas != null ? canvas.scaleFactor : 1f;
        return scale > 0f ? scale : 1f;
    }
}
