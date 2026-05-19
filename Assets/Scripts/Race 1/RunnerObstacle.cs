using UnityEngine;

namespace Game.EndlessRunner
{
    [RequireComponent(typeof(Collider2D))]
    public class RunnerObstacle : MonoBehaviour
    {
        public RunnerStickerType CurrentType { get; private set; }

        private RunnerStickerMovement _movement;
        private int _baseLane;
        private float _aliveTime;
        private float _destroyX = -14f;
        private float _speed;           // скорость передаётся при спавне

        private RunnerLaneLayout _layout;

        public void Setup(
            RunnerStickerType type,
            int lane,
            RunnerStickerMovement movement,
            float speed,
            float destroyX = -14f)
        {
            CurrentType = type;
            _baseLane = lane;
            _movement = movement;
            _speed = speed;
            _destroyX = destroyX;
            _aliveTime = 0f;
            _layout = RunnerLaneLayout.Instance;

            SnapYToLane(_baseLane, 0f);
            ApplyVisuals();

            GetComponent<Collider2D>().isTrigger = true;
        }

        void Update()
        {
            _aliveTime += Time.deltaTime;

            // Движение влево с постоянной скоростью
            transform.position += new Vector3(-_speed * Time.deltaTime, 0f, 0f);

            // Вертикальное колебание
            if (_movement.isMoving && _layout != null)
            {
                float laneOffset = Mathf.Sin(_aliveTime * _movement.laneSpeed + _movement.lanePhase)
                                   * _movement.laneAmplitude;
                float targetLane = Mathf.Clamp(_baseLane + laneOffset, 0f, _layout.LaneCount - 1f);
                SnapYToLane(targetLane, 1f);

                float xOscil = Mathf.Sin(_aliveTime * _movement.xSpeed + _movement.xPhase)
                               * _movement.xAmplitude;
                transform.position += new Vector3(xOscil * Time.deltaTime, 0f, 0f);
            }

            if (transform.position.x < _destroyX)
                Destroy(gameObject);
        }

        private void SnapYToLane(float laneFloat, float lerpSpeed)
        {
            if (_layout == null) return;

            int a = Mathf.Clamp(Mathf.FloorToInt(laneFloat), 0, _layout.LaneCount - 1);
            int b = Mathf.Clamp(Mathf.CeilToInt(laneFloat), 0, _layout.LaneCount - 1);
            float targetY = Mathf.Lerp(_layout.GetLaneY(a), _layout.GetLaneY(b), laneFloat - a);

            transform.position = lerpSpeed <= 0f
                ? new Vector3(transform.position.x, targetY, transform.position.z)
                : new Vector3(transform.position.x,
                    Mathf.Lerp(transform.position.y, targetY, lerpSpeed * Time.deltaTime),
                    transform.position.z);
        }

        private void ApplyVisuals()
        {
            if (!TryGetComponent<SpriteRenderer>(out var sr)) return;
            sr.color = CurrentType switch
            {
                RunnerStickerType.Hype => new Color(1f, 0.9f, 0.1f),
                RunnerStickerType.Death => new Color(1f, 0.2f, 0.2f),
                _ => Color.white
            };
        }

        public void Consume() => Destroy(gameObject);
    }
}
