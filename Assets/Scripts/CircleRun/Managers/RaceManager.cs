using Game.Run;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Splines;

namespace Game.CircleRun
{
    public class RaceManager : MonoBehaviour
    {
        public static RaceManager Instance { get; private set; }

        [Header("Track Settings")]
        public int lapsToWin = 3;

        [Header("Card Logic")]
        [SerializeField] private CardData[] currentHand;
        public event Action<int> OnCardPlayed;

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

        // ¡˚ÒÚ˚È ‰ÓÒÚÛÔ Í Ë„ÓÍÛ Ë ‚‡„‡Ï ‰Îˇ ÒÔÓÒÓ·ÌÓÒÚÂÈ
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
            Debug.Log("RaceManager: ¬ÒÂ Û˜‡ÒÚÌËÍË „ÓÚÓ‚˚. —Ú‡Ú!");
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
            foreach (var r in allRacers) r.isRacing = false;
            Debug.Log($"œŒ¡≈ƒ»“≈À‹: {winner.racerName}");
        }

        // --- ÀŒ√» ¿  ¿–“ ---
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
            OnCardPlayed?.Invoke(handIndex);
            return true;
        }

        // --- —œ¿¬Õ —“» ≈–Œ¬ ---
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
