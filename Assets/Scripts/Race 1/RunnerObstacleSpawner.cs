using System.Collections.Generic;
using UnityEngine;

namespace Game.EndlessRunner
{
    public class RunnerObstacleSpawner : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private RunnerPatternDatabase patternDatabase;
        [SerializeField] private GameObject obstaclePrefab;   // префаб с RunnerStickerView + Collider2D

        [Header("Spawn Settings")]
        [SerializeField] private float spawnX = 12f;
        [SerializeField] private float gapBetweenPatterns = 5f;
        [SerializeField] private float initialDelay = 3f;

        [Header("Speed")]
        [Tooltip("Скорость стикеров влево — должна совпадать со скоростью фона")]
        public float obstacleSpeed = 5f;

        // ── приватные ─────────────────────────────────────────────────────────

        private bool _active;
        private float _distanceToNext;
        private readonly List<GameObject> _spawned = new();

        // ── запуск / остановка ────────────────────────────────────────────────

        public void StartSpawning()
        {
            _active = true;
            _distanceToNext = obstacleSpeed * initialDelay;
        }

        public void StopSpawning() => _active = false;

        public void ClearAll()
        {
            foreach (var go in _spawned)
                if (go != null) Destroy(go);
            _spawned.Clear();
        }

        // ── апдейт ────────────────────────────────────────────────────────────

        private void Update()
        {
            if (!_active || patternDatabase == null) return;

            _distanceToNext -= obstacleSpeed * Time.deltaTime;

            if (_distanceToNext <= 0f)
                SpawnNextPattern();
        }

        // ── спавн паттерна ────────────────────────────────────────────────────

        private void SpawnNextPattern()
        {
            RunnerPattern pattern = patternDatabase.GetRandom();
            if (pattern == null) return;

            var layout = RunnerLaneLayout.Instance;

            foreach (var data in pattern.stickers)
            {
                float x = spawnX + data.xOffset;
                float y = layout.GetLaneY(Mathf.Clamp(data.startLane, 0, layout.LaneCount - 1));

                GameObject go = Instantiate(
                    obstaclePrefab,
                    new Vector3(x, y, 0f),
                    Quaternion.identity,
                    transform);

                if (go.TryGetComponent<RunnerStickerView>(out var view))
                    view.Setup(data.type, obstacleSpeed, data.startLane, data.movement);

                _spawned.Add(go);
            }

            _distanceToNext = pattern.patternWidth + gapBetweenPatterns;
            _spawned.RemoveAll(o => o == null);
        }
    }
}
