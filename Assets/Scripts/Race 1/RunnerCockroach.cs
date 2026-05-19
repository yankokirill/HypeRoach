using UnityEngine;
using Game.Base;

namespace Game.EndlessRunner
{
    [System.Serializable]
    public class CockroachStats
    {
        [Header("Базовые")]
        [Tooltip("Жизни таракана (равно уровню)")]
        public int maxLives = 1;

        [Tooltip("Шанс уворота от Death-стикера (0..1). Ловкач даёт +0.07 за штуку")]
        [Range(0f, 1f)] public float dodgeChance = 0f;

        [Tooltip("Процентный бонус к итоговому хайпу за стикер. Богач даёт +0.15 за штуку")]
        [Range(0f, 10f)] public float hypePercentBonus = 0f;

        [Header("Легендарные улучшения")]
        [Tooltip("Плоский бонус к хайпу за каждый следующий стикер (Тусовщик)")]
        public int hypeFlatBonusPerSticker = 0;

        [Tooltip("Бонус хайпа за стикер когда осталась одна жизнь (Берсерк)")]
        public int berserkBonus = 0;

        [Header("Редкие улучшения")]
        [Tooltip("Деньги за подобранный Hype-стикер (Барыга)")]
        public int moneyPerHype = 0;

        [Tooltip("Деньги за подобранный Death-стикер (Dead Inside)")]
        public int moneyPerDeath = 0;
    }

    public class RunnerCockroach : MonoBehaviour
    {
        [Header("Lane Settings")]
        [Range(0, 3)] public int startLane = 1;
        public float laneChangeSpeed = 10f;

        [Header("Invulnerability after hit")]
        public float invulnerabilityDuration = 1.5f;

        [Header("Stats")]
        public CockroachStats stats = new CockroachStats();

        // ── публичные свойства ────────────────────────────────────────────────

        public int CurrentLane { get; private set; }
        public int Hype { get; private set; }
        public int Lives { get; private set; }
        public int LivesSpent { get; private set; }
        public bool IsAlive => Lives > 0;
        public bool IsInvulnerable => _invulTimer > 0f;

        // ── приватные ─────────────────────────────────────────────────────────

        private RunnerLaneLayout _layout;
        private float _targetY;
        private float _invulTimer;
        private bool _inputBlocked;
        private int _stickersCollected;

        // ── lifecycle ─────────────────────────────────────────────────────────

        void Awake()
        {
            Lives = stats.maxLives;
        }

        void Start()
        {
            _layout = RunnerLaneLayout.Instance;
            CurrentLane = Mathf.Clamp(startLane, 0, _layout.LaneCount - 1);
            _targetY = _layout.GetLaneY(CurrentLane);
            SnapToTargetY();
        }

        void Update()
        {
            if (!IsAlive) return;
            HandleInput();
            UpdateInvulnerability();
        }

        void FixedUpdate()
        {
            if (!IsAlive) return;
            MoveTowardsLane();
        }

        // ── ввод ──────────────────────────────────────────────────────────────

        private void HandleInput()
        {
            if (_inputBlocked) return;

            bool up = Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W);
            bool down = Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S);

            if (up && CurrentLane < _layout.LaneCount - 1) TryChangeLane(CurrentLane + 1);
            if (down && CurrentLane > 0) TryChangeLane(CurrentLane - 1);
        }

        private void TryChangeLane(int newLane)
        {
            newLane = Mathf.Clamp(newLane, 0, _layout.LaneCount - 1);
            if (newLane == CurrentLane) return;
            CurrentLane = newLane;
            _targetY = _layout.GetLaneY(CurrentLane);
        }

        // ── движение ──────────────────────────────────────────────────────────

        private void MoveTowardsLane()
        {
            float newY = Mathf.MoveTowards(
                transform.position.y, _targetY,
                laneChangeSpeed * Time.fixedDeltaTime);
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);
        }

        private void SnapToTargetY()
        {
            transform.position = new Vector3(transform.position.x, _targetY, transform.position.z);
        }

        // ── неуязвимость ──────────────────────────────────────────────────────

        private void UpdateInvulnerability()
        {
            if (_invulTimer > 0f)
            {
                _invulTimer -= Time.deltaTime;
                if (TryGetComponent<SpriteRenderer>(out var sr))
                {
                    float alpha = Mathf.PingPong(Time.time * 6f, 1f);
                    sr.color = new Color(1f, 1f, 1f, Mathf.Lerp(0.2f, 1f, alpha));
                }
            }
            else
            {
                if (TryGetComponent<SpriteRenderer>(out var sr))
                    sr.color = Color.white;
            }
        }

        // ── публичный API ─────────────────────────────────────────────────────

        /// <summary>
        /// Вызывается при сборе Hype-стикера.
        /// Порядок: база → Берсерк (если 1 жизнь) → Тусовщик (плоский) → Богач (% на итог).
        /// </summary>
        public int AddHype(int baseAmount)
        {
            // Берсерк: +30 если осталась одна жизнь
            int berserk = (stats.berserkBonus > 0 && Lives == 1) ? stats.berserkBonus : 0;
            // Тусовщик: каждый следующий стикер даёт +N плоского
            int flat = stats.hypeFlatBonusPerSticker * _stickersCollected;
            // Богач: процент на итоговую сумму
            int total = Mathf.RoundToInt((baseAmount + berserk + flat) * (1f + stats.hypePercentBonus));

            _stickersCollected++;
            Hype = Mathf.Max(0, Hype + total);
            RunnerUI.Instance?.UpdateHype(Hype);

            // Барыга: деньги за хайп
            if (stats.moneyPerHype > 0)
                RunnerManager.Instance?.AddMoney(stats.moneyPerHype);

            return total; // возвращаем итог чтобы RunnerManager показал правильную цифру
        }

        /// <summary>
        /// Death-стикер: проверяем уворот, потом наносим урон.
        /// </summary>
        public bool TakeDeathHit()
        {
            // Dead Inside: деньги за попытку умереть
            if (stats.moneyPerDeath > 0)
                RunnerManager.Instance?.AddMoney(stats.moneyPerDeath);

            // Ловкач: шанс уворота
            if (stats.dodgeChance > 0f && Random.value < stats.dodgeChance)
                return false; // уворот — сообщаем наружу

            Lives--;
            LivesSpent++;
            _invulTimer = invulnerabilityDuration;
            RunnerUI.Instance?.UpdateLives(Lives);

            if (Lives <= 0) OnDead();
            return true; // урон нанесён
        }

        public void GainLife()
        {
            Lives = Mathf.Min(Lives + 1, stats.maxLives);
            RunnerUI.Instance?.UpdateLives(Lives);
        }

        private void OnDead()
        {
            _inputBlocked = true;
            RunnerManager.Instance?.OnPlayerDead();
        }

        /// <summary>
        /// Применяет статы с карточки таракана (центральный слот базы).
        /// Вызывать до StartRunner, сразу после Awake/Start.
        /// </summary>
        public void ApplyCardStats(SavedFieldCard saved)
        {
            if (saved == null) return;

            // Уровень 4 → 2 жизни, всё остальное → 1
            stats.maxLives = saved.level >= 4 ? 2 : 1;
            Lives = stats.maxLives;
            RunnerUI.Instance?.UpdateLives(Lives);

            // Сбрасываем бонусы — применяем с нуля
            stats.dodgeChance = 0f;
            stats.hypePercentBonus = 0f;
            stats.hypeFlatBonusPerSticker = 0;
            stats.berserkBonus = 0;
            stats.moneyPerHype = 0;
            stats.moneyPerDeath = 0;

            foreach (var upgrade in saved.upgrades)
            {
                switch (upgrade)
                {
                    // DedInside: эффект срабатывает при поражении (см. RunnerManager),
                    // в забеге никаких бонусов не даёт
                    case UpgradeType.DedInside:
                        break;

                    // Богач: +15% к итоговому хайпу за штуку
                    case UpgradeType.Baryga:
                        stats.hypePercentBonus += 0.15f;
                        break;

                    // Берсерк (легендарный): +30 хайпа за стикер когда осталась 1 жизнь
                    case UpgradeType.Berserk:
                        stats.berserkBonus += 30;
                        break;

                    // Тусовщик (легендарный): плоский бонус за каждый следующий стикер
                    case UpgradeType.Tusovshchik:
                        stats.hypeFlatBonusPerSticker += 5;
                        break;
                }
            }
        }
    }
}
