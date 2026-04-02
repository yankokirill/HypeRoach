using System.Collections.Generic;
using UnityEngine;

namespace Game.Run
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance;

        public HandManager handManager;
        public RaceManager raceManager;
        public HypeManager hypeManager;

        public List<Cockroach> allRacers = new List<Cockroach>();

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                Debug.Log("✅ [GameManager] Singleton создан.");
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        }

        void Start()
        {
            Debug.Log("▶️ [GameManager] Метод Start вызван. Запускаем игру...");

            // Проверка ссылок перед запуском
            if (handManager == null) Debug.LogError("❌ [GameManager] Ссылка на HandManager пуста!");
            if (raceManager == null) Debug.LogError("❌ [GameManager] Ссылка на RaceManager пуста!");

            if (handManager != null && raceManager != null)
            {
                StartNewGame();
            }
            else
            {
                Debug.LogError("⛔ [GameManager] Не все ссылки заполнены в Инспекторе! Игра не запустится.");
            }
        }

        public void StartNewGame()
        {
            Debug.Log("🚀 [GameManager] Вызов StartNewGame...");

            Debug.Log("   -> Запуск колоды...");
            handManager.Initialize();

            Debug.Log("   -> Запуск Hype Manager...");
            hypeManager.Initialize(allRacers);

            Debug.Log("   -> Старт гонки...");
            raceManager.StartRace(allRacers);

            Debug.Log("🎉 [GameManager] Игра запущена успешно!");
        }
    }
}
