using Game.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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

        private bool isRaceActive = false;
        private List<Cockroach> allRacers = new List<Cockroach>();
        private Dictionary<Cockroach, int> racerLaps = new Dictionary<Cockroach, int>();
        private Dictionary<Cockroach, float> lastProgress = new Dictionary<Cockroach, float>();
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

            racerLaps.Clear();
            lastProgress.Clear();
            stickerSpawnTimer = stickerSpawnInterval;

            foreach (var racer in allRacers)
            {
                if (racer == null) continue;

                racerLaps[racer] = 0;
                lastProgress[racer] = 0f;
                racer.isRacing = true;
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
            float currentProgress = racer.GetProgress();
            if (lastProgress[racer] > 0.8f && currentProgress < 0.2f)
            {
                racerLaps[racer]++;
                if (racerLaps[racer] >= lapsToWin) EndRace(racer);
            }
            lastProgress[racer] = currentProgress;
        }
    
    private void EndRace(Cockroach winner)
    {
        isRaceActive = false;

        // Останавливаем движение всех участников
        foreach (var r in allRacers)
        {
            if (r != null) r.isRacing = false;
        }

        Debug.Log($"ПОБЕДИТЕЛЬ: {winner.racerName}");

        // 1. Сортируем всех гонщиков по хайпу
        var sortedResults = allRacers
            .OrderByDescending(r => r.GetHype())
            .ToList();

        if (sortedResults.Count >= 2)
        {
            Cockroach firstPlace = sortedResults[0];
            Cockroach secondPlace = sortedResults[1];
            Debug.Log($"Первое место: {firstPlace.GetHype()} хайпа");
            Debug.Log($"Второе место: {secondPlace.GetHype()} хайпа");
            
            // 2. Проверяем, является ли победитель игроком
            if (firstPlace is PlayerCockroach && ProfileManager.Instance != null)
            {
                // 3. Рассчитываем награду: Хайп 1-го места минус Хайп 2-го места
                int hypeReward = firstPlace.GetHype() - secondPlace.GetHype();

                // Если разница положительная, прибавляем к ресурсам
                if (hypeReward > 0)
                {
                    ProfileManager.Instance.profile.currentResources += hypeReward;
                    Debug.Log($"RaceManager: Игрок победил! Начислено ресурсов: {hypeReward}");
                }
            }
        }

        // ВЫЗОВ ОКНА НАГРАДЫ
        if (RewardUI.Instance != null)
        {
            RewardUI.Instance.GenerateRewardUI(allRacers);
        }
        else
        {
            Debug.LogWarning("RaceManager: RewardUI не найден на сцене!");
        }
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

        private void HandleStickerSpawning()
        {
            if (hypeStickerPrefab == null) return;
            stickerSpawnTimer -= Time.fixedDeltaTime;
            if (stickerSpawnTimer <= 0)
            {
                stickerSpawnTimer = stickerSpawnInterval;
                SpawnStickerOnSpline();
            }
        }

        private void SpawnStickerOnSpline()
        {
            if (allRacers.Count == 0) return;
            var leader = allRacers.OrderByDescending(r => racerLaps[r] + r.GetProgress()).First();
            float spawnProg = (leader.GetProgress() + spawnProgressAhead) % 1f;
            Vector3 spawnPos = Race.GetRandomLane().EvaluatePosition(spawnProg);
            GameObject go = Instantiate(hypeStickerPrefab, spawnPos, Quaternion.identity, transform);
            if (go.TryGetComponent(out StickerView view))
            {
                view.Setup(UnityEngine.Random.value < 0.4f ? StickerType.Sabotage : StickerType.Hype);
            }
        }
    }
}