using Game.Core;
using UnityEngine;

namespace Game.Race
{
    public enum StickerType
    {
        Hype,      // Дает очки
        Sabotage   // Ловушка: отнимает очки и отбрасывает
    }

    public class StickerView : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private int hypeAmount = 20;
        [SerializeField] private int sabotagePenalty = -10;
        [SerializeField] private float sabotageBumpForce = 3f;

        [Header("Stats Influence")]
        [Tooltip("Сколько % бонуса к хайпу дает 1 единица харизмы")]
        [SerializeField] private float charismaMultiplier = 0.02f; // 2% за 1 ед.
        [Tooltip("Какой шанс уклонения (0-1) дает 1 единица IQ")]
        [SerializeField] private float iqDodgeMultiplier = 0.01f; // 1% за 1 ед.

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
            if (iconRenderer == null) return;
            iconRenderer.sprite = (type == StickerType.Hype) ? hypeIcon : sabotageIcon;
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (isCollected) return;

            if (collision.TryGetComponent(out Cockroach racer))
            {
                // Проверяем эффекты IQ и Харизмы ПЕРЕД тем как применить эффект
                ApplyEffect(racer);
                CollectSticker();
            }
        }

        private void ApplyEffect(Cockroach racer)
        {
            bool isPlayer = racer is PlayerCockroach;

            // Получаем статы только если это игрок (у ботов пока статов нет)
            int iq = isPlayer ? ProfileManager.Instance.profile.baseIQ : 0;
            int charisma = isPlayer ? ProfileManager.Instance.profile.baseCharisma : 0;

            if (currentType == StickerType.Hype)
            {
                // --- ЛОГИКА ХАРИЗМЫ ---
                // Формула: БазовыйХайп + (БазовыйХайп * Харизма * Множитель)
                float bonus = hypeAmount * (charisma * charismaMultiplier);
                int finalHype = hypeAmount + Mathf.RoundToInt(bonus);

                racer.AddHype(finalHype);

                if (isPlayer && charisma > 0)
                    Debug.Log($"<color=yellow>Бонус Харизмы!</color> Получено {finalHype} вместо {hypeAmount}");
            }
            else if (currentType == StickerType.Sabotage)
            {
                if (racer.IsInvulnerable()) return;

                // --- ЛОГИКА IQ (Уворот) ---
                // Формула: Шанс = IQ * Множитель (например 30 IQ = 30% шанс)
                float dodgeChance = iq * iqDodgeMultiplier;

                if (isPlayer && Random.value < dodgeChance)
                {
                    Debug.Log("<color=cyan>IQ СРАБОТАЛ!</color> Таракан просчитал траекторию ловушки и увернулся!");
                    return; // Выходим, не применяя штраф
                }

                // Если не увернулся — штрафуем
                racer.AddHype(sabotagePenalty);
                racer.BumpBack(sabotageBumpForce);
            }
        }

        private void CollectSticker()
        {
            isCollected = true;
            if (collectEffect != null)
                Instantiate(collectEffect, transform.position, Quaternion.identity);
            Destroy(gameObject);
        }
    }
}
