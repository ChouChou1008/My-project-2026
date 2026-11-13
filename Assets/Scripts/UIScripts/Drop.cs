using UnityEngine;
using UnityEngine.EventSystems;

public class Drop : MonoBehaviour, IDropHandler                 
{
    public void OnDrop(PointerEventData eventData)                // OnDrag → OnDrop
    {
        Drag dragged = eventData.pointerDrag != null ?
            eventData.pointerDrag.GetComponent<Drag>() : null;   // 무엇이 끌려왔는지

        if (dragged == null)
        {
            return;
        }

        if (transform.childCount > 0)                             // 이미 아이템이 있으면 → 자리 바꾸기
        {
            Drag current = transform.GetChild(0).GetComponent<Drag>();
            if (current != null)
            {
                current.MoveTo(dragged.OriginalParent);
            }
        }

        dragged.MoveTo(transform);                                // 끌려온 아이템을 이 슬롯으로
        dragged.IsDropped = true;
    }
}