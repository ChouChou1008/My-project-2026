using UnityEngine;

namespace ForestWhispers.Game
{
    /// <summary>
    /// 맵 끝의 출입구. 플레이어가 닿으면 화면을 어둡게 하고 이웃 맵으로 넘어간다.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class MapGate : MonoBehaviour
    {
        public static bool ArrivedFromRight { get; private set; }

        [Header("Gate Settings")]
        [SerializeField] private string _nextScene = "";
        [SerializeField] private bool _arriveAtRightEnd;

        public void Setup(string nextScene, bool arriveAtRightEnd)
        {
            _nextScene = nextScene;
            _arriveAtRightEnd = arriveAtRightEnd;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponent<PlayerController2D>() == null)
            {
                return;
            }

            ArrivedFromRight = _arriveAtRightEnd;
            ScreenFade.LoadScene(_nextScene);
        }
    }
}
