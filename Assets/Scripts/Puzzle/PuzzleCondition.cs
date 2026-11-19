using System;
using UnityEngine;

/// <summary>
/// 퍼즐 관문을 여는 조건 하나(발판, 채집 열쇠 등)의 공통 부모.
/// PuzzleGate는 조건의 종류를 몰라도 IsSolved와 Changed만 보고 판단한다.
/// </summary>
public abstract class PuzzleCondition : MonoBehaviour
{
    public event Action Changed;

    public abstract bool IsSolved { get; }

    protected void NotifyChanged()
    {
        Changed?.Invoke();
    }
}
