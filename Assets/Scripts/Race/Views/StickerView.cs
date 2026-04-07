using UnityEngine;

namespace Game.Race
{
    public enum StickerType { Hype, Sabotage, Arrow }
    public enum ArrowDirection { Forward = 0, Right = 1, Back = 2, Left = 3 }

    public class StickerView : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private SpriteRenderer iconRenderer;

        [Header("Sprites")]
        [SerializeField] private Sprite hypeIcon;
        [SerializeField] private Sprite sabotageIcon;
        [SerializeField] private Sprite arrowIcon;

        [Header("VFX")]
        [SerializeField] private GameObject collectEffect;

        public StickerType CurrentType { get; private set; }
        public ArrowDirection CurrentDirection { get; private set; }
        public float CurrentProgress { get; private set; } // Текущий прогресс на сплайне

        private StickerMovementData moveData;
        private float initialProgress;
        private int initialLane;
        private int currentLogicalLane;
        private bool isCollected = false;

        public void Setup(
            StickerType type,
            int laneIndex,
            float progress,
            StickerMovementData movement)
        {
            CurrentType = type;
            initialLane = laneIndex;
            currentLogicalLane = laneIndex;

            CurrentDirection = (ArrowDirection)Random.Range(0, 4);
            initialProgress = progress;
            CurrentProgress = progress;
            moveData = movement;

            if (iconRenderer == null) return;

            if (type == StickerType.Arrow)
            {
                iconRenderer.sprite = arrowIcon;
            }
            else
            {
                iconRenderer.sprite = (type == StickerType.Hype) ? hypeIcon : sabotageIcon;
            }

            // Инициализируем позицию и угол
            UpdatePositionAndVisuals(initialLane, initialProgress);
        }

        private void Update()
        {
            if (isCollected || !moveData.isMoving) return;

            // 1. Считаем смещение по линиям
            float laneOffset = Mathf.Sin(Time.time * moveData.laneSpeed + moveData.lanePhase) * moveData.laneAmplitude;
            float targetLaneFloat = Mathf.Clamp(initialLane + laneOffset, 0f, 2f);

            // 2. Считаем смещение по прогрессу трассы
            float progOffset = Mathf.Sin(Time.time * moveData.progSpeed + moveData.progPhase) * moveData.progAmplitude;
            CurrentProgress = (initialProgress + progOffset) % 1f;
            if (CurrentProgress < 0f) CurrentProgress += 1f; // Защита от отрицательного прогресса

            // 3. Обновляем логическую полосу для коллизий и правил стрелок
            currentLogicalLane = Mathf.RoundToInt(targetLaneFloat);

            // 4. Применяем расчеты
            UpdatePositionAndVisuals(targetLaneFloat, CurrentProgress);
        }

        private void UpdatePositionAndVisuals(float targetLaneFloat, float currentProg)
        {
            // --- ПОЗИЦИЯ ---
            Vector3 pos0 = Race.GetLane(0).EvaluatePosition(currentProg);
            Vector3 pos1 = Race.GetLane(1).EvaluatePosition(currentProg);
            Vector3 pos2 = Race.GetLane(2).EvaluatePosition(currentProg);

            Vector3 finalPos;
            if (targetLaneFloat <= 1f) finalPos = Vector3.Lerp(pos0, pos1, targetLaneFloat);
            else finalPos = Vector3.Lerp(pos1, pos2, targetLaneFloat - 1f);

            transform.position = finalPos;

            // --- УГОЛ НАКЛОНА ---
            if (CurrentType == StickerType.Arrow)
            {
                // Берем касательную центральной линии как базу для поворота
                Vector3 tangent = Race.GetLane(1).EvaluateTangent(currentProg);
                float splineAngle = Mathf.Atan2(tangent.y, tangent.x) * Mathf.Rad2Deg;

                float localRotation = 0f;
                switch (CurrentDirection)
                {
                    case ArrowDirection.Forward: localRotation = 180f; break;
                    case ArrowDirection.Right: localRotation = 90f; break;
                    case ArrowDirection.Back: localRotation = 0f; break;
                    case ArrowDirection.Left: localRotation = -90f; break;
                }

                iconRenderer.transform.rotation = Quaternion.Euler(0, 0, splineAngle + localRotation);
                iconRenderer.color = Color.cyan;
            }
            else
            {
                iconRenderer.transform.rotation = Quaternion.identity;
            }
        }

        private void OnMouseDown()
        {
            if (CurrentType != StickerType.Arrow || isCollected) return;
            CycleDirection();
        }

        private void CycleDirection()
        {
            int nextDir = ((int)CurrentDirection + 1) % 4;

            CurrentDirection = (ArrowDirection)nextDir;

            // Сразу обновляем визуал при клике
            UpdatePositionAndVisuals(currentLogicalLane, CurrentProgress);
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (isCollected) return;
            if (collision.TryGetComponent(out Cockroach racer))
            {
                if (RaceManager.Instance != null) RaceManager.Instance.OnStickerCollected(this, racer);
            }
        }

        public void Consume()
        {
            isCollected = true;
            if (collectEffect != null) Instantiate(collectEffect, transform.position, Quaternion.identity);
            Destroy(gameObject);
        }
    }
}
