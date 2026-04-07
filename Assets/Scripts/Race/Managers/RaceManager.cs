using Game.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Game.Race
{
    public class RaceManager : MonoBehaviour
    {
        public static RaceManager Instance { get; private set; }

        [Header("Track Settings")]
        public int lapsToWin = 3;

        [Header("Card Logic")]
        [SerializeField] private CardData[] currentHand;

        [Header("Sticker Spawner")]
        [SerializeField] private GameObject hypeStickerPrefab;
        [SerializeField] private float stickerSpawnInterval = 3f;
        [SerializeField] private float spawnProgressAhead = 0.15f;

        [Header("Sticker Effects Settings")]
        [SerializeField] private int hypeAmount = 20;
        [SerializeField] private int sabotagePenalty = -10;
        [SerializeField] private float sabotageBumpForce = 3f;
        [SerializeField] private float patternLength = 0.12f;

        [Tooltip("Сколько % бонуса к хайпу дает 1 единица харизмы")]
        [SerializeField] private float charismaMultiplier = 0.02f;
        [Tooltip("Какой шанс уклонения (0-1) дает 1 единица IQ")]
        [SerializeField] private float iqDodgeMultiplier = 0.01f;

        [Header("Pattern Database")]
        [SerializeField] private List<StickerPattern> patternDatabase;

        private bool isRaceActive = false;
        private List<Cockroach> allRacers = new List<Cockroach>();
        private float stickerSpawnTimer;

        public bool IsRaceActive => isRaceActive;

        public PlayerCockroach Player => allRacers.OfType<PlayerCockroach>().FirstOrDefault();
        public EnemyCockroach[] Enemies => allRacers.OfType<EnemyCockroach>().ToArray();

        void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        public void StartRace(PlayerCockroach player, List<EnemyCockroach> enemies)
        {
            allRacers.Clear();
            if (player != null) allRacers.Add(player);
            if (enemies != null) allRacers.AddRange(enemies);

            stickerSpawnTimer = 0.5f;

            foreach (var racer in allRacers)
            {
                if (racer == null) continue;

                racer.isRacing = true;
            }

            if (VoiceManager.Instance != null)
            {
                VoiceManager.Instance.ResetRaceCounters();
            }

            if (MusicManager.Instance != null)
            {
                MusicManager.Instance.StartRaceMusic();
            }
            
            isRaceActive = true;
            Debug.Log("RaceManager: Все участники готовы. Старт!");
        }

        void FixedUpdate()
        {
            if (!isRaceActive) return;

            foreach (var racer in allRacers)
            {
                racer.UpdateState();
                CheckLaps(racer);
            }

            HandleStickerSpawning();
        }

        private void CheckLaps(Cockroach racer)
        {
            if (racer.currentLap >= lapsToWin)
            {
                EndRace(racer);
            }
        }

        private void EndRace(Cockroach winner)
        {
            isRaceActive = false;

            foreach (var r in allRacers)
            {
                if (r != null) r.isRacing = false;
            }

            var sortedResults = allRacers.OrderByDescending(r => r.GetHype()).ToList();

            if (sortedResults.Count >= 2)
            {
                Cockroach firstPlace = sortedResults[0];
                Cockroach secondPlace = sortedResults[1];

                if (firstPlace is PlayerCockroach && ProfileManager.Instance != null)
                {
                    int hypeReward = firstPlace.GetHype() - secondPlace.GetHype();
                    ProfileManager.Instance.profile.currentResources += hypeReward;
                    ProfileManager.Instance.profile.result = hypeReward;
                } else
                {
                    ProfileManager.Instance.profile.result = -1; // Lose
                }
            }

            if (MusicManager.Instance != null)
                MusicManager.Instance.LowerMusicAtEnd();

            if (RewardUI.Instance != null)
                RewardUI.Instance.GenerateRewardUI(allRacers);
        }

        public bool CanPlayCard(int handIndex)
        {
            if (handIndex < 0 || handIndex >= currentHand.Length) return false;
            CardData card = currentHand[handIndex];
            return Player != null && card.mana <= Player.mana && card.effect.Check(Player, Enemies);
        }

        public bool TryPlayCard(int handIndex)
        {
            if (!CanPlayCard(handIndex)) return false;
            CardData card = currentHand[handIndex];
            Player.AddMana(-card.mana);
            card.effect.Apply(Player, Enemies, card.level);
            return true;
        }

        #region Sticker Spawning & Logic
        
        private void ExecutePattern(StickerPattern pattern, float baseProg)
        {
            foreach (var stickerData in pattern.stickers)
            {
                // Вычисляем финальный прогресс для каждого стикера в паттерне
                float finalProg = (baseProg + stickerData.progressOffset) % 1f;

                SpawnSticker(
                    stickerData.startLane,
                    finalProg,
                    stickerData.type,
                    stickerData.movement
                );
            }
        }

        private void HandleStickerSpawning()
        {
            if (hypeStickerPrefab == null || allRacers.Count < 2 || patternDatabase.Count == 0) return;

            stickerSpawnTimer -= Time.fixedDeltaTime;
            if (stickerSpawnTimer <= 0)
            {
                // 1. Ищем таракана на втором месте
                var sortedRacers = allRacers.OrderByDescending(r => r.currentLap + r.GetProgress()).ToList();
                Cockroach secondPlaceRacer = sortedRacers[1];

                // 2. Высчитываем базовую точку спавна
                float baseProg = (secondPlaceRacer.GetProgress() + spawnProgressAhead) % 1f;

                // 3. Выбираем случайный паттерн из базы
                int randomIndex = Random.Range(0, patternDatabase.Count);
                StickerPattern selectedPattern = patternDatabase[randomIndex];

                // 4. Спавним паттерн
                ExecutePattern(selectedPattern, baseProg);

                // Сброс таймера
                stickerSpawnTimer = stickerSpawnInterval;
            }
        }

        private void SpawnSticker(int laneIndex, float progress, StickerType type, StickerMovementData movement)
        {
            // Начальная позиция нужна только для Instantiate
            Vector3 startPos = Race.GetLane(laneIndex).EvaluatePosition(progress);

            GameObject go = Instantiate(hypeStickerPrefab, startPos, Quaternion.identity, transform);
            if (go.TryGetComponent(out StickerView view))
            {
                view.Setup(type, laneIndex, progress, movement);
            }
        }

        private struct CocroachStats
        {
            public bool isPlayer;
            public int iq;
            public int charisma;
            public Vector3 spawnPos;
        }

        private CocroachStats GetCockroachStats(Cockroach racer)
        {
            bool isPlayer = racer is PlayerCockroach;
            int iq = isPlayer && ProfileManager.Instance != null ? ProfileManager.Instance.profile.baseIQ : 0;
            int charisma = isPlayer && ProfileManager.Instance != null ? ProfileManager.Instance.profile.baseCharisma : 0;
            Vector3 spawnPos = racer.transform.position;

            return new CocroachStats
            {
                isPlayer = isPlayer,
                iq = iq,
                charisma = charisma,
                spawnPos = spawnPos
            };
        }

        public void OnStickerCollected(StickerView sticker, Cockroach racer)
        {
            if (!isRaceActive) return;

            bool isPlayer = racer is PlayerCockroach;
            bool effectApplied = true;

            // Ссылки для сокращения кода
            int iq = isPlayer && ProfileManager.Instance != null ? ProfileManager.Instance.profile.baseIQ : 0;
            int charisma = isPlayer && ProfileManager.Instance != null ? ProfileManager.Instance.profile.baseCharisma : 0;
            Vector3 spawnPos = racer.transform.position;

            if (sticker.CurrentType == StickerType.Hype)
            {
                ApplyHypeSticker(racer);
            }
            else if (sticker.CurrentType == StickerType.Sabotage)
            {
                effectApplied = ApplySabotageSticker(racer);
            }
            else if (sticker.CurrentType == StickerType.Arrow)
            {
                ApplyArrowSticker(racer, sticker.CurrentDirection);
            }

            if (VoiceManager.Instance != null)
            {
                VoiceManager.Instance.OnCollectedSticker(sticker.CurrentType, effectApplied);
            }

            sticker.Consume();
        }

        private void ApplyHypeSticker(Cockroach racer)
        {
            var stats = GetCockroachStats(racer);
            float bonus = hypeAmount * (stats.charisma * charismaMultiplier);
            int finalHype = hypeAmount + Mathf.RoundToInt(bonus);

            racer.AddHype(finalHype);

            // --- ВИЗУАЛ ХАЙПА ---
            UIManager.Instance.SpawnFloatingText(stats.spawnPos, $"+{finalHype} HYPE", Color.yellow);
        }

        private bool ApplySabotageSticker(Cockroach racer)
        {
            var stats = GetCockroachStats(racer);
            if (!racer.IsInvulnerable())
            {
                float dodgeChance = stats.iq * iqDodgeMultiplier;

                if (stats.isPlayer && Random.value < dodgeChance)
                {
                    Debug.Log("<color=cyan>IQ СРАБОТАЛ!</color>");

                    // --- ВИЗУАЛ УКЛОНЕНИЯ ---
                    UIManager.Instance.SpawnFloatingText(stats.spawnPos, "DODGE!", Color.cyan);
                    return false;
                }
                else
                {
                    racer.AddHype(sabotagePenalty);
                    racer.BumpBack(sabotageBumpForce);

                    // --- ВИЗУАЛ ШТРАФА ---
                    UIManager.Instance.SpawnFloatingText(stats.spawnPos, $"{sabotagePenalty} HYPE", Color.red);
                    return true;
                }
            }
            else
            {
                UIManager.Instance.SpawnFloatingText(stats.spawnPos, "SHIELD!", Color.white);
                return false;
            }
        }

        private void ApplyArrowSticker(Cockroach racer, ArrowDirection direction)
        {
            if (direction == ArrowDirection.Forward)
            {
                var boost = new Effects.SpeedBoostModifier(1f);
                boost.Initialize(racer, 0.8f);
                racer.AddEffect(boost);

                UIManager.Instance.SpawnFloatingText(racer.transform.position, "BOOST!", Color.yellow);
            }
            else if (direction == ArrowDirection.Back)
            {
                var reverse = new Effects.ReverseModifier();
                reverse.Initialize(racer, 1.5f);
                racer.AddEffect(reverse);
                UIManager.Instance.SpawnFloatingText(racer.transform.position, "REVERSE!", Color.magenta);
            }
            else
            {
                int offset = (direction == ArrowDirection.Right) ? 1 : -1;
                int targetLane = Mathf.Clamp(racer.currentLane + offset, 0, 2);

                racer.ChangeLane(targetLane);
                UIManager.Instance.SpawnFloatingText(racer.transform.position, "SWITCH!", Color.cyan);
            }
        }
        #endregion
    }
}
