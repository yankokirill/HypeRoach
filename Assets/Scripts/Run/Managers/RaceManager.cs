using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Game.Run
{
    public class RaceManager : MonoBehaviour
    {
        public static RaceManager Instance { get; private set; }

        [Header("Race Settings")]
        [SerializeField] private float finishLineX = 20f;
        [SerializeField] private float[] laneYPositions = { -1.5f, 0f, 1.5f };
        [SerializeField] private float collisionDistance = 0.8f;

        [Header("Sticker Spawner")]
        [SerializeField] private GameObject hypeStickerPrefab;
        [SerializeField] private float stickerSpawnInterval = 4f; // Как часто спавнить стикеры
        [SerializeField] private float spawnDistanceAhead = 6f; // На каком расстоянии впереди лидера спавнить

        [Header("Card Logic")]
        [SerializeField] private CardData[] currentHand;
        public event Action<int> OnCardPlayed;

        public PlayerCockroach Player => allRacers.OfType<PlayerCockroach>().FirstOrDefault();
        public EnemyCockroach[] Enemies => allRacers.OfType<EnemyCockroach>().ToArray();

        private bool isRaceActive = false;
        private List<Cockroach> allRacers = new List<Cockroach>();
        private List<Cockroach> finishedRacers = new List<Cockroach>();
        private float stickerSpawnTimer;

        public bool IsRaceActive => isRaceActive;

        void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        void FixedUpdate()
        {
            if (!isRaceActive) return;

            foreach (var racer in allRacers)
            {
                racer.UpdateState();
            }

            HandleStickerSpawning();
            HandleManualCollisions();
            CheckFinishLine();
        }

        // --- ЛОГИКА СПАВНА СТИКЕРОВ ---
        private void HandleStickerSpawning()
        {
            if (hypeStickerPrefab == null) return;

            stickerSpawnTimer -= Time.fixedDeltaTime;
            if (stickerSpawnTimer <= 0)
            {
                stickerSpawnTimer = stickerSpawnInterval;
                SpawnStickerAhead();
            }
        }

        private void SpawnStickerAhead()
        {
            // Находим X-координату того, кто бежит первым
            float leaderX = allRacers.Max(r => r.transform.position.x);
            float spawnX = leaderX + spawnDistanceAhead + UnityEngine.Random.Range(0f, 2f);

            // Не спавним стикеры за финишной линией
            if (spawnX >= finishLineX) return;

            // Выбираем случайную линию
            int randomLane = UnityEngine.Random.Range(0, 3);
            float spawnY = GetLaneY(randomLane);

            // Создаем стикер
            GameObject go = Instantiate(hypeStickerPrefab, new Vector3(spawnX, spawnY - 0.2f, 0), Quaternion.identity, transform);

            // Настраиваем ТИП стикера
            StickerView view = go.GetComponent<StickerView>();
            if (view != null)
            {
                StickerType randomType = (UnityEngine.Random.value < 0.5)
                                         ? StickerType.Sabotage
                                         : StickerType.Hype;

                view.Setup(randomType);
            }

        }

        // --- ФИЗИКА И КОЛЛИЗИИ ---
        private void HandleManualCollisions()
        {
            for (int i = 0; i < allRacers.Count; i++)
            {
                for (int j = i + 1; j < allRacers.Count; j++)
                {
                    Cockroach a = allRacers[i];
                    Cockroach b = allRacers[j];

                    if (a.currentLane != b.currentLane) continue;

                    // Если кто-то из них под баффом неуязвимости (Dash, Hitchhike) - игнорируем столкновение
                    if (a.IsInvulnerable() || b.IsInvulnerable()) continue;

                    float dist = a.transform.position.x - b.transform.position.x;

                    if (Mathf.Abs(dist) < collisionDistance)
                    {
                        float bumpForce = 4f;

                        if (dist < 0) a.BumpBack(bumpForce); // 'a' находится позади
                        else b.BumpBack(bumpForce);          // 'b' находится позади
                    }
                }
            }
        }

        // --- БАЗОВАЯ ЛОГИКА ГОНКИ ---
        private void CheckFinishLine()
        {
            foreach (var racer in allRacers)
            {
                if (racer.transform.position.x >= finishLineX)
                {
                    OnRacerFinished(racer);
                    return;
                }
            }
        }

        public float GetLaneY(int laneIndex) => laneYPositions[Mathf.Clamp(laneIndex, 0, 2)];
        public float GetFinishPos() => finishLineX;

        public void StartRace(List<Cockroach> racers)
        {
            allRacers = racers;
            if (allRacers.Count == 0) return;

            finishedRacers.Clear();
            stickerSpawnTimer = stickerSpawnInterval; // Сброс таймера

            foreach (var racer in allRacers)
            {
                if (racer == null) continue;
                racer.Initialize();
                racer.isRacing = true;
            }

            isRaceActive = true;
        }

        // --- ЛОГИКА КАРТ ---

        public bool CanPlayCard(int handIndex)
        {
            CardData card = currentHand[handIndex];
            return card.mana <= Player.mana && card.effect.Check(Player, Enemies);
        }

        public bool TryPlayCard(int handIndex)
        {
            if (!CanPlayCard(handIndex)) return false;
            
            CardData card = currentHand[handIndex];
            Player.AddMana(-card.mana);
            card.effect.Apply(Player, Enemies);

            OnCardPlayed?.Invoke(handIndex);
            return true;
        }

        private void OnRacerFinished(Cockroach racer)
        {
            if (!racer.isRacing) return;
            finishedRacers.Add(racer);
            EndRace();
        }

        private void EndRace()
        {
            isRaceActive = false;
            foreach (var racer in allRacers) racer.Stop();
            Debug.Log($"Гонка завершена! Победитель: {finishedRacers[0].racerName}");
        }
    }
}
