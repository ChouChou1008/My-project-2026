using UnityEngine;
using UnityEngine.EventSystems;

public class Drop : MonoBehaviour, IDropHandler
{
    public void OnDrop(PointerEventData eventData)
    {
        Drag drag = eventData.pointerDrag.GetComponent<Drag>();
        //eventData.pointerDrag  드래그하고 있는 아이템
        if (drag != null)
        {
            drag.SwapWith(transform);
        }                               
    }
}