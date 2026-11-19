using UnityEngine;

/// <summary>
/// 지정한 채집물(열쇠)을 캐면 풀리는 조건.
/// 채집하면 채집물이 없어지거나 꺼지므로, 그 상태를 보고 판단한다.
/// </summary>
public class GatherKeyCondition : PuzzleCondition
{
    [SerializeField] private GameObject _keyItem;

    private bool _isSolved;

    public override bool IsSolved => _isSolved;

    public void Setup(GameObject keyItem)
    {
        _keyItem = keyItem;
    }

    private void Update()
    {
        if (_isSolved)
        {
            return;
        }

        if (_keyItem != null && _keyItem.activeInHierarchy)
        {
            return;
        }

        _isSolved = true;
        NotifyChanged();
    }
}
