using UnityEngine;
using UnityEngine.UIElements;

public class BoxTurret : MonoBehaviour
{

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Box"))
        {
            Debug.Log("감지");
        }
    }
}
