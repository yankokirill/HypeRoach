using Game.Core;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.EndlessRunner
{
    public class RunnerManager : MonoBehaviour
    {
        public static RunnerManager Instance { get; private set; }

        [Header("World Speed")]
        [SerializeField] private float startWorldSpeed = 5f;
        [SerializeField] private float worldAcceleration = 0.1f;
        [SerializeField] private float maxWorldSpeed = 20f;

        [Header("References")]
        [SerializeField] private RunnerCockroach playerCockroach;
        [SerializeField] private RunnerObstacleSpawner spawner;

        [Header("Rewards")]
        [SerializeField] private int hypeReward = 20;

        [Header("Distance Score")]
        [SerializeField] private int hypePerDistance = 1;

        [Header("Victory Condition")]
        [Tooltip("Базовое количество хайпа для победы (без учёта дней)")]
        [SerializeField] private int hypeGoalStart = 100;
        [Tooltip("Прибавка к цели за каждый прожитый день")]
        [SerializeField] private float hypeGoalCoefficient = 50f;
        [Tooltip("Название сцены базы для возврата после победы/поражения")]
        [SerializeField] private string baseSceneName = "Base";

        public float WorldSpeed { get; private set; }
        public bool IsRunning { get; private set; }
        public int Money { get; private set; }
        public int HypeGoal { get; private set; }

        private float _distanceAccumulator;
        private bool _roundFinished;

        // ── lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);

            WorldSpeed = startWorldSpeed;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Start()
        {
            if (MusicManager.Instance != null)
            {
                MusicManager.Instance.StartRaceMusic();
            }

            // Применяем статы таракана с центрального слота базы
            if (ProfileManager.Instance != null)
                playerCockroach?.ApplyCardStats(ProfileManager.Instance.profile.centerCockroach);

            // Накопленный бонус DedInside прибавляется к базовой награде за стикер
            if (ProfileManager.Instance != null)
                hypeReward += ProfileManager.Instance.profile.dedInsideHypeBonus;

            // Рассчитываем цель победы с учётом текущего дня
            int dayCount = ProfileManager.Instance != null
                ? ProfileManager.Instance.profile.dayCount
                : 0;
            HypeGoal = hypeGoalStart + Mathf.RoundToInt(dayCount * hypeGoalCoefficient);

            RunnerUI.Instance?.UpdateHypeGoal(HypeGoal);
            StartRunner();
        }

        private void Update()
        {
            if (!IsRunning) return;

            WorldSpeed = Mathf.Min(
                WorldSpeed + worldAcceleration * Time.deltaTime,
                maxWorldSpeed);

            if (spawner != null)
                spawner.obstacleSpeed = WorldSpeed;

            _distanceAccumulator += WorldSpeed * Time.deltaTime;
            while (_distanceAccumulator >= 10f)
            {
                _distanceAccumulator -= 10f;
                playerCockroach.AddHype(hypePerDistance);
            }

            // Проверка условия победы
            if (!_roundFinished && playerCockroach != null && playerCockroach.Hype >= HypeGoal)
                OnPlayerVictory();

            RunnerUI.Instance?.UpdateSpeed(WorldSpeed);
        }

        // ── запуск / рестарт ──────────────────────────────────────────────────

        public void StartRunner()
        {
            IsRunning = true;
            _roundFinished = false;
            WorldSpeed = startWorldSpeed;
            _distanceAccumulator = 0f;

            spawner?.StartSpawning();
            RunnerUI.Instance?.ShowHUD(true);
        }

        public void RestartRunner()
        {
            if (MusicManager.Instance != null)
                MusicManager.Instance.StopMusicCompletely();

            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public void ReturnToBase()
        {
            if (MusicManager.Instance != null)
                MusicManager.Instance.StopMusicCompletely();

            SceneManager.LoadScene(baseSceneName);
        }

        // ── коллизии ─────────────────────────────────────────────────────────

        public void OnStickerHit(RunnerStickerView sticker, RunnerCockroach roach)
        {
            if (!IsRunning) return;

            switch (sticker.CurrentType)
            {
                case RunnerStickerType.Hype:
                    int gained = roach.AddHype(hypeReward);
                    RunnerUI.Instance?.SpawnFloatingText(
                        roach.transform.position, $"+{gained}", Color.yellow);
                    break;

                case RunnerStickerType.Death:
                    bool hit = roach.TakeDeathHit();
                    if (hit)
                        RunnerUI.Instance?.SpawnFloatingText(
                            roach.transform.position, "-1", Color.red);
                    else
                        RunnerUI.Instance?.SpawnFloatingText(
                            roach.transform.position, "DODGE!", Color.cyan);
                    break;
            }

            sticker.Consume();
        }

        // ── деньги ────────────────────────────────────────────────────────────

        public void AddMoney(int amount)
        {
            Money += amount;
            RunnerUI.Instance?.UpdateMoney(Money);
        }

        // ── победа ───────────────────────────────────────────────────────────

        public void OnPlayerVictory()
        {
            if (_roundFinished) return;
            _roundFinished = true;
            IsRunning = false;
            spawner?.StopSpawning();

            if (MusicManager.Instance != null)
                MusicManager.Instance.LowerMusicAtEnd();

            SaveRunResult(RunResult.Victory);

            int finalHype = playerCockroach != null ? playerCockroach.Hype : 0;
            RunnerUI.Instance?.ShowVictory(finalHype);
        }

        // ── смерть ───────────────────────────────────────────────────────────

        public void OnPlayerDead()
        {
            if (_roundFinished) return;
            _roundFinished = true;
            IsRunning = false;
            spawner?.StopSpawning();

            if (MusicManager.Instance != null)
                MusicManager.Instance.LowerMusicAtEnd();

            SaveRunResult(RunResult.Defeat);

            // DedInside: если на таракане был этот апгрейд — +2 к базовому хайпу навсегда
            ApplyDedInsideOnDefeat();

            int finalHype = playerCockroach != null ? playerCockroach.Hype : 0;
            RunnerUI.Instance?.ShowGameOver(finalHype);
        }

        // ── сохранение результата ─────────────────────────────────────────────

        private void SaveRunResult(RunResult result)
        {
            if (ProfileManager.Instance != null)
                ProfileManager.Instance.profile.lastRunResult = result;
        }

        private void ApplyDedInsideOnDefeat()
        {
            if (ProfileManager.Instance == null) return;
            var saved = ProfileManager.Instance.profile.centerCockroach;
            if (saved == null || saved.upgrades == null) return;

            int dedInsideCount = saved.upgrades.Count(u => u == Game.Base.UpgradeType.DedInside);

            if (dedInsideCount > 0)
            {
                ProfileManager.Instance.profile.dedInsideHypeBonus += dedInsideCount * 2;
            }
        }
    }
}
