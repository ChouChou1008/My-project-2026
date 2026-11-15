using UnityEngine;
using UnityEngine.EventSystems;

public class Drop : MonoBehaviour, IDropHandler
{
    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null)
        {
            return;
        }

        Drag dragged = eventData.pointerDrag.GetComponent<Drag>();   // 끌려온 칸
        if (dragged == null ||
            dragged.transform == transform)                           // 자기 자신에 놓음
        {
            return;
        }

        dragged.SwapWith(transform);                                 // 나와 교환
    }
}