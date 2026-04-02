using UnityEngine;
using System.Collections.Generic;

namespace Game.Race
{
    public class GameManager : MonoBehaviour
    {
        [Header("Participants")]
        public PlayerCockroach player;
        public List<EnemyCockroach> enemies;

        void Start()
        {
            player.SnapToStart();
            foreach (var enemy in enemies)
            {
                enemy.SnapToStart();
            }
            UIManager.Instance.Initialize(player, enemies);
            Invoke(nameof(StartRace), 1.0f);
        }

        public void StartRace()
        {
            if (RaceManager.Instance != null)
            {
                // Передаем игрока и список врагов в RaceManager
                RaceManager.Instance.StartRace(player, enemies);
                Debug.Log("GameManager: Сигнал старта отправлен в RaceManager.");
            }
            else
            {
                Debug.LogError("GameManager: RaceManager не найден на сцене!");
            }
        }
    }
}
