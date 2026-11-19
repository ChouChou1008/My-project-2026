using UnityEngine;

/// <summary>
/// 연결된 조건이 모두 풀리면 출구를 연다. 한 번 열리면 다시 닫히지 않는다.
/// 조건이 하나도 없으면 처음부터 열려 있다(위치 자체가 퍼즐인 맵).
/// </summary>
public class PuzzleGate : MonoBehaviour
{
    [SerializeField] private PuzzleCondition[] _conditions;
    [SerializeField] private ExitPortal _exit;

    private bool _isOpen;

    public void Setup(PuzzleCondition[] conditions, ExitPortal exit)
    {
        _conditions = conditions;
        _exit = exit;
    }

    private void OnEnable()
    {
        foreach (PuzzleCondition condition in _conditions)
        {
            condition.Changed += CheckConditions;
        }
    }

    private void OnDisable()
    {
        foreach (PuzzleCondition condition in _conditions)
        {
            condition.Changed -= CheckConditions;
        }
    }

    private void Start()
    {
        CheckConditions();
    }

    private void CheckConditions()
    {
        if (_isOpen)
        {
            return;
        }

        foreach (PuzzleCondition condition in _conditions)
        {
            if (!condition.IsSolved)
            {
                return;
            }
        }

        _isOpen = true;
        _exit.Open();
    }
}
