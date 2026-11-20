using UnityEngine;
using UnityEngine.UIElements;

public class BoxTurret : MonoBehaviour
{
    private bool _isCheck;

    public bool IsCheck
    {
        get { return _isCheck; }
        set { _isCheck = value; }
    }
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Box"))
        {
            Debug.Log("감지");
            _isCheck = true;
        }
    }
}
