using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Linq;
using System.Collections.Generic;
using Game.Core;

namespace Game.Race
{
    public class RewardUI : MonoBehaviour
    {
        public static RewardUI Instance;

        [Header("Основные окна")]
        [SerializeField] private GameObject mainPanel; // Панель с контентом
        [SerializeField] private GameObject overlay;   // Затемняющий фон (Overlay)

        [Header("Текстовые поля")]
        [SerializeField] private Text titleText;
        [SerializeField] private Text leaderboardText;
        [SerializeField] private Text formulaText;
        [SerializeField] private Text rewardText;

        [Header("Кнопки")]
        [SerializeField] private Button backButton;

        [Header("Настройки цветов")]
        public Color winColor = Color.green;
        public Color loseColor = Color.red;

        [Header("Тестирование")]
        [Tooltip("Нажми кнопку в меню компонента (три точки) или используй кнопку ниже в режиме Play")]
        public bool runTestInPlayMode;

        private void Awake()
        {
            Instance = this;
            // Скрываем всё при старте
            if (mainPanel != null) mainPanel.SetActive(false);
            if (overlay != null) overlay.SetActive(false);
        }

        private void Start()
        {
            if (backButton != null)
            {
                backButton.onClick.AddListener(() => SceneManager.LoadScene("Base"));
            }
        }

        // Метод для обновления из инспектора (удобно для теста)
        private void OnValidate()
        {
            if (runTestInPlayMode && Application.isPlaying)
            {
                runTestInPlayMode = false;
                TestUI();
            }
        }

        public void GenerateRewardUI(List<Cockroach> racers)
        {
            // Включаем фон и панель
            if (overlay != null) overlay.SetActive(true);
            if (mainPanel != null) mainPanel.SetActive(true);

            // 1. Если список пуст (для теста), создаем фейковые данные
            if (racers == null || racers.Count == 0)
            {
                SetFakeData();
                return;
            }

            // 2. Сортируем участников по Хайпу
            var sorted = racers.OrderByDescending(r => r.GetHype()).ToList();
            if (sorted.Count < 1) return;

            bool playerWon = sorted[0] is PlayerCockroach;

            // 3. Устанавливаем заголовок
            titleText.text = playerWon ? "ПОБЕДА!" : "ПОРАЖЕНИЕ";
            titleText.color = playerWon ? winColor : loseColor;

            // 4. Заполняем таблицу лидеров
            string leaderboardStr = "";
            for (int i = 0; i < Mathf.Min(3, sorted.Count); i++)
            {
                bool isPlayer = sorted[i] is PlayerCockroach;
                string colorHex = isPlayer ? "#00FF00" : "#FFFFFF";
                leaderboardStr += $"<color={colorHex}>{i + 1}. {sorted[i].racerName} — {sorted[i].GetHype()}</color>\n\n";
            }
            leaderboardText.text = leaderboardStr;

            // 5. Логика награды
            if (playerWon && sorted.Count >= 2)
            {
                int hype1st = sorted[0].GetHype();
                int hype2nd = sorted[1].GetHype();
                int reward = Mathf.Max(0, hype1st - hype2nd);

                if (formulaText != null)
                {
                    formulaText.gameObject.SetActive(true);
                    formulaText.text = $"Разница: {hype1st} - {hype2nd} = {reward}";
                }

                if (rewardText != null)
                    rewardText.text = $"ИТОГО: +{reward} ₽";

                if (ProfileManager.Instance != null)
                    ProfileManager.Instance.profile.currentResources += reward;
            }
            else
            {
                if (formulaText != null) formulaText.gameObject.SetActive(false);
                if (rewardText != null)
                    rewardText.text = playerWon ? "Победа! Но соперников нет." : "Награда только за 1-е место";
            }
        }

        private void SetFakeData()
        {
            titleText.text = "ТЕСТОВЫЙ РЕЖИМ";
            titleText.color = winColor;
            leaderboardText.text = "<color=#00FF00>1. Игрок — 500</color>\n\n2. Бот Вася — 300\n\n3. Бот Петя — 100";
            formulaText.text = "Разница: 500 - 300 = 200";
            rewardText.text = "ИТОГО: +200 ₽";
        }

        [ContextMenu("ТЕСТ: Показать награду")]
        public void TestUI()
        {
            GenerateRewardUI(new List<Cockroach>());
        }
    }
}
