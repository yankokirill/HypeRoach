using UnityEngine;

namespace Game.EndlessRunner
{
    /// <summary>
    /// Держит Y-позиции четырёх горизонтальных дорожек.
    /// Повесить на любой GameObject в сцене (например RunnerManager).
    /// </summary>
    public class RunnerLaneLayout : MonoBehaviour
    {
        public static RunnerLaneLayout Instance { get; private set; }

        public float lane0Y = -2.25f;
        public float lane1Y = -0.75f;
        public float lane2Y = 0.75f;
        public float lane3Y = 2.25f;

        void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public float GetLaneY(int lane)
        {
            return lane switch
            {
                0 => lane0Y,
                1 => lane1Y,
                2 => lane2Y,
                3 => lane3Y,
                _ => lane1Y
            };
        }

        public int LaneCount => 4;
    }
}
