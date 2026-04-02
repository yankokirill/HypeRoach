using UnityEngine;

namespace Game.Run
{
    public enum StickerType
    {
        Hype,      // Дает очки
        Sabotage   // Ловушка: отнимает очки и отбрасывает
    }

    [RequireComponent(typeof(Collider2D))]
    public class StickerView : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private int hypeAmount = 20;
        [SerializeField] private int sabotagePenalty = -10;
        [SerializeField] private float sabotageBumpForce = 3f;

        [Header("Components")]
        [SerializeField] private SpriteRenderer iconRenderer;

        [Header("Emoji Sprites")]
        [SerializeField] private Sprite hypeIcon;
        [SerializeField] private Sprite sabotageIcon;

        [Header("VFX (Optional)")]
        [SerializeField] private GameObject collectEffect;

        private StickerType currentType;
        private bool isCollected = false;

        public void Setup(StickerType type)
        {
            currentType = type;

            switch (type)
            {
                case StickerType.Hype:
                    if (iconRenderer != null) iconRenderer.sprite = hypeIcon;
                    break;
                case StickerType.Sabotage:
                    if (iconRenderer != null) iconRenderer.sprite = sabotageIcon;
                    break;
            }
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            // Если стикер уже подобран, игнорируем
            if (isCollected) return;

            // Ищем компонент Cockroach на объекте, который в нас вошел
            if (collision.TryGetComponent(out Cockroach racer))
            {
                ApplyEffect(racer);
                CollectSticker();
            }
        }

        private void ApplyEffect(Cockroach racer)
        {
            if (currentType == StickerType.Hype)
            {
                racer.AddHype(hypeAmount);
            }
            else if (currentType == StickerType.Sabotage)
            {
                // Если у таракана есть бафф на прохождение сквозь врагов (Рывок/Упряжка)
                if (racer.IsInvulnerable())
                {
                    Debug.Log($"{racer.racerName} раздавил ловушку без урона!");
                    return; // Игнорируем штраф, но стикер всё равно уничтожится
                }

                // Штрафуем и откидываем
                racer.AddHype(sabotagePenalty);
                racer.BumpBack(sabotageBumpForce);
            }
        }

        private void CollectSticker()
        {
            isCollected = true;

            // Спавним эффект на месте стикера (если он назначен в инспекторе)
            if (collectEffect != null)
            {
                Instantiate(collectEffect, transform.position, Quaternion.identity);
            }

            // Уничтожаем объект стикера
            Destroy(gameObject);
        }
    }
}
