using UnityEngine;

namespace Game.EndlessRunner
{
    [RequireComponent(typeof(Collider2D))]
    [RequireComponent(typeof(SpriteRenderer))]
    public class RunnerStickerView : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private SpriteRenderer iconRenderer;

        [Header("Sprites")]
        [SerializeField] private Sprite hypeSprite;
        [SerializeField] private Sprite deathSprite;

        [Header("Destroy Boundary")]
        [SerializeField] private float destroyX = -14f;

        public RunnerStickerType CurrentType { get; private set; }

        private float _speed;
        private bool _consumed;

        private RunnerStickerMovement _movement;
        private int _baseLane;
        private float _aliveTime;
        private RunnerLaneLayout _layout;

        // ── инициализация ─────────────────────────────────────────────────────

        public void Setup(RunnerStickerType type, float speed, int lane, RunnerStickerMovement movement)
        {
            CurrentType = type;
            _speed = speed;
            _baseLane = lane;
            _movement = movement;
            _consumed = false;
            _aliveTime = 0f;
            _layout = RunnerLaneLayout.Instance;

            ApplyVisuals();

            GetComponent<Collider2D>().isTrigger = true;
        }

        // ── движение ──────────────────────────────────────────────────────────

        private void Update()
        {
            if (_consumed) return;

            _aliveTime += Time.deltaTime;

            // Горизонтальное движение влево
            transform.position += new Vector3(-_speed * Time.deltaTime, 0f, 0f);

            // Вертикальное колебание по дорожкам
            if (_movement.isMoving && _layout != null)
            {
                float laneOffset = Mathf.Sin(_aliveTime * _movement.laneSpeed + _movement.lanePhase)
                                   * _movement.laneAmplitude;
                float laneFloat = Mathf.Clamp(_baseLane + laneOffset, 0f, _layout.LaneCount - 1f);

                int a = Mathf.Clamp(Mathf.FloorToInt(laneFloat), 0, _layout.LaneCount - 1);
                int b = Mathf.Clamp(Mathf.CeilToInt(laneFloat), 0, _layout.LaneCount - 1);
                float newY = Mathf.Lerp(_layout.GetLaneY(a), _layout.GetLaneY(b), laneFloat - a);

                transform.position = new Vector3(transform.position.x, newY, transform.position.z);
            }

            if (transform.position.x < destroyX)
                Destroy(gameObject);
        }

        // ── коллизия ──────────────────────────────────────────────────────────

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_consumed) return;
            if (!other.TryGetComponent<RunnerCockroach>(out var roach)) return;
            RunnerManager.Instance?.OnStickerHit(this, roach);
        }

        // ── визуал ────────────────────────────────────────────────────────────

        private void ApplyVisuals()
        {
            iconRenderer.sprite = CurrentType switch
            {
                RunnerStickerType.Hype => hypeSprite,
                RunnerStickerType.Death => deathSprite,
                _ => null
            };
        }

        public void Consume()
        {
            _consumed = true;
            Destroy(gameObject);
        }
    }
}
