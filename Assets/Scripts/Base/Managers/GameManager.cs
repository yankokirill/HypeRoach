using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Game.Core;

namespace Game.Base
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Prefabs")]
        public CardView cardPrefab;

        [Header("Card System")]
        public CardDatabase mainDatabase;

        [Header("Hierarchy References")]
        public Hand playerHand;

        [Header("Game Settings")]
        [SerializeField] private int startingResources = 500;

        public event Action<int> OnResourcesChanged;
        public event Action<GridSlot> OnSlotSelected;
        public event Action OnStatsChanged;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void Start()
        {
            Initialize();
            MusicManager.Instance?.StartBaseMusic();
        }

        private void Initialize()
        {
            if (ProfileManager.Instance == null) return;
            var profile = ProfileManager.Instance.profile;

            if (profile.isFirstRun)
            {
                profile.isFirstRun = false;
                profile.currentResources = startingResources;
                for (int i = 0; i < 3; i++) SpawnHandCard(mainDatabase.buildings[0]);
            }
            else
            {
                LoadGridState(profile.placedCardsOnGrid);
                LoadHandState(profile.hand);

                bool wasDefeat = profile.lastRunResult == RunResult.Defeat;

                // После поражения в раннере удаляем карту с центрального слота
                if (wasDefeat)
                {
                    ClearCenterSlot();
                    profile.centerCockroach = null;
                    profile.lastRunResult = RunResult.None;
                }

                // Сразу после очистки слота проверяем условия поражения в игре
                if (wasDefeat)
                {
                    CheckForDefeat();
                }
            }

            OnResourcesChanged?.Invoke(GetCurrentResources());
        }

        // ─── Проверка на поражение ────────────────────────────────────────────

        private void CheckForDefeat()
        {
            bool hasCardsOnField = false;
            GridSlot[] allSlots = FindObjectsByType<GridSlot>(FindObjectsSortMode.None);
            foreach (var slot in allSlots)
            {
                if (!slot.IsEmpty)
                {
                    hasCardsOnField = true;
                    break;
                }
            }

            bool hasBuildingInHand = false;
            if (playerHand != null)
            {
                foreach (var cardView in playerHand.GetCards())
                {
                    if (cardView != null && cardView.handData != null && cardView.handData.type == CardType.Building)
                    {
                        hasBuildingInHand = true;
                        break;
                    }
                }
            }

            // Условие поражения: на поле нет ни одной карты И в руке нет карт типа Building (тараканов)
            if (!hasCardsOnField && !hasBuildingInHand)
            {
                EndGameScreen.Instance?.ShowDefeat();
            }
        }

        // ─── Колода ───────────────────────────────────────────────────────────

        public void SpawnHandCard(HandCardData data)
        {
            if (playerHand.Count >= playerHand.maxHandSize) return;
            CardView card = Instantiate(cardPrefab, playerHand.handParent);
            card.InitAsHandCard(data);
            playerHand.AddCard(card);
        }

        // ─── Сохранение / загрузка ────────────────────────────────────────────

        public void SaveState()
        {
            if (ProfileManager.Instance == null) return;
            var profile = ProfileManager.Instance.profile;

            GridSlot[] slots = GetSortedSlots();
            profile.placedCardsOnGrid.Clear();
            foreach (var slot in slots)
            {
                if (slot.IsEmpty)
                {
                    profile.placedCardsOnGrid.Add(null);
                }
                else
                {
                    FieldCard fc = slot.GetCard().fieldData;
                    profile.placedCardsOnGrid.Add(new SavedFieldCard
                    {
                        cardID = fc.Source.cardID,
                        level = fc.Level,
                        upgrades = new List<UpgradeType>(fc.Upgrades),
                        totalGreen = fc.TotalGreen,
                        totalWhite = fc.TotalWhite,
                        totalYellow = fc.TotalYellow,
                    });
                }
            }

            profile.hand.Clear();
            foreach (var cardView in playerHand.GetCards())
                profile.hand.Add(cardView.handData.cardID);

            // Сохраняем таракана с центрального слота (1,1) для раннера
            profile.centerCockroach = FindCenterCockroach();

            Debug.Log("Состояние базы сохранено!");
        }

        private void LoadGridState(List<SavedFieldCard> savedCards)
        {
            GridSlot[] slots = GetSortedSlots();

            for (int i = 0; i < Mathf.Min(savedCards.Count, slots.Length); i++)
            {
                SavedFieldCard saved = savedCards[i];
                if (saved == null) continue;

                HandCardData source = mainDatabase.FindBuilding(saved.cardID);
                if (source == null) continue;

                FieldCard fc = FieldCard.FromSave(source, saved.level, saved.upgrades,
                    saved.totalGreen, saved.totalWhite, saved.totalYellow);
                CardView card = Instantiate(cardPrefab, slots[i].transform);
                slots[i].PlaceCard(card, fc);
            }
        }

        private void LoadHandState(List<string> savedCardIDs)
        {
            foreach (string id in savedCardIDs)
            {
                HandCardData data = mainDatabase.FindCard(id);
                if (data != null) SpawnHandCard(data);
            }
        }

        private GridSlot[] GetSortedSlots() =>
            FindObjectsByType<GridSlot>(FindObjectsSortMode.None)
                .OrderBy(s => s.transform.position.y)
                .ThenBy(s => s.transform.position.x)
                .ToArray();

        private SavedFieldCard FindCenterCockroach()
        {
            GridSlot[] allSlots = FindObjectsByType<GridSlot>(FindObjectsSortMode.None);
            foreach (var slot in allSlots)
            {
                if (slot.gridX == 1 && slot.gridY == 1 && !slot.IsEmpty)
                {
                    FieldCard fc = slot.GetCard().fieldData;
                    return new SavedFieldCard
                    {
                        cardID = fc.Source.cardID,
                        level = fc.Level,
                        upgrades = new List<UpgradeType>(fc.Upgrades),
                        totalGreen = fc.TotalGreen,
                        totalWhite = fc.TotalWhite,
                        totalYellow = fc.TotalYellow,
                    };
                }
            }
            return null; // слот пустой
        }

        private void ClearCenterSlot()
        {
            GridSlot[] allSlots = FindObjectsByType<GridSlot>(FindObjectsSortMode.None);
            foreach (var slot in allSlots)
            {
                if (slot.gridX == 1 && slot.gridY == 1 && !slot.IsEmpty)
                {
                    Destroy(slot.GetCard().gameObject);
                    slot.RemoveCard();
                    NotifyStatsChanged();
                    break;
                }
            }
        }

        // ─── Экономика ────────────────────────────────────────────────────────

        public int GetCurrentResources() =>
            ProfileManager.Instance != null
                ? ProfileManager.Instance.profile.currentResources
                : startingResources;

        public bool CanAfford(int amount) => GetCurrentResources() >= amount;

        public void AddResources(int amount)
        {
            if (ProfileManager.Instance == null) return;
            ProfileManager.Instance.profile.currentResources += amount;
            OnResourcesChanged?.Invoke(ProfileManager.Instance.profile.currentResources);
        }

        public bool SpendResources(int amount)
        {
            if (!CanAfford(amount) || ProfileManager.Instance == null) return false;
            ProfileManager.Instance.profile.currentResources -= amount;
            OnResourcesChanged?.Invoke(ProfileManager.Instance.profile.currentResources);
            return true;
        }

        // ─── События ──────────────────────────────────────────────────────────

        public void OnCardPlaced(CardView card, GridSlot slot)
        {
            playerHand.RemoveCard(card);
            NotifyStatsChanged();
        }

        public void OnSlotClicked(GridSlot slot) =>
            OnSlotSelected?.Invoke(slot);

        public void NotifyStatsChanged() => OnStatsChanged?.Invoke();
    }
}
