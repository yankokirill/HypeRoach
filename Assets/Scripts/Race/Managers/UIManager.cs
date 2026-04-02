using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using System.Collections.Generic;

namespace Game.Race
{
    [System.Serializable]
    public class RacerUIContainer
    {
        public Slider hypeSlider;
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI manaText; // Только для игрока
    }

    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Header("Global Hype Settings")]
        [SerializeField] private int baseMaxHype = 100;

        [Header("UI Slots (Assign in Inspector)")]
        [SerializeField] private RacerUIContainer playerUI;
        [SerializeField] private RacerUIContainer[] enemyUIs;

        private int currentMaxHype;

        // Быстрый поиск: по таракану находим его UI-контейнер
        private Dictionary<Cockroach, RacerUIContainer> racerToUI = new Dictionary<Cockroach, RacerUIContainer>();

        void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);

            currentMaxHype = baseMaxHype;
        }

        // --- ИНИЦИАЛИЗАЦИЯ (вызывать из GameManager) ---

        public void Initialize(PlayerCockroach player, List<EnemyCockroach> enemies)
        {
            racerToUI.Clear();

            // 1. Привязываем игрока
            if (player != null)
            {
                racerToUI.Add(player, playerUI);
                SetupUI(player, playerUI);
            }

            // 2. Привязываем врагов по порядку
            for (int i = 0; i < enemies.Count; i++)
            {
                if (i < enemyUIs.Length)
                {
                    racerToUI.Add(enemies[i], enemyUIs[i]);
                    SetupUI(enemies[i], enemyUIs[i]);
                }
            }
        }

        private void SetupUI(Cockroach racer, RacerUIContainer ui)
        {
            if (ui.nameText != null) ui.nameText.text = racer.racerName;
            if (ui.hypeSlider != null)
            {
                ui.hypeSlider.maxValue = currentMaxHype;
                ui.hypeSlider.value = 0;
            }
        }

        // --- ПУБЛИЧНЫЕ МЕТОДЫ (API) ---

        public void SetHype(Cockroach racer, int amount)
        {
            if (!racerToUI.TryGetValue(racer, out RacerUIContainer ui)) return;

            if (amount >= currentMaxHype)
            {
                LevelUpGlobalHype(racer);
            }

            // Анимируем полоску
            if (ui.hypeSlider != null)
            {
                ui.hypeSlider.DOValue(amount, 0.3f).SetEase(Ease.OutQuad);
            }

        }

        public void SetMana(int amount)
        {
            // Поскольку мана только у игрока, берем его UI (можно через поиск в словаре или напрямую)
            if (playerUI.manaText != null)
            {
                playerUI.manaText.text = amount.ToString();
                playerUI.manaText.transform.DOPunchScale(Vector3.one * 0.15f, 0.2f);
            }
        }

        // --- ВНУТРЕННЯЯ ЛОГИКА ---

        private void LevelUpGlobalHype(Cockroach racer)
        {
            currentMaxHype *= 2;

            // Обновляем лимиты у всех существующих слайдеров в словаре
            foreach (var item in racerToUI)
            {
                var slider = item.Value.hypeSlider;
                float value = slider.value;
                slider.maxValue = currentMaxHype;
                slider.value = 2 * value;

                if (item.Key != racer)
                {
                    slider.DOValue(value, 0.3f).SetEase(Ease.OutQuad);
                }
            }

            Debug.Log($"[UI] Новый уровень хайпа. Лимит: {currentMaxHype}");
        }
    }
}
