using System;
using System.Collections.Generic;
using UnityEngine;
using Game.Core;
using System.Linq;

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
        public Transform dragLayer;
        public Hand playerHand;

        [Header("Game Settings")]
        [SerializeField] private int startingResources = 500;

        public event Action<int> OnResourcesChanged;
        public event Action<GridSlot> OnSlotSelected;
        public event Action OnStatsChanged;

        private List<CardData> deck = new List<CardData>();
        private GridSlot selectedSlot;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void Start()
        {
            Initialize();
            if (MusicManager.Instance != null)
            {
                MusicManager.Instance.StartBaseMusic();
            }
        }

        private void Initialize()
        {
            if (ProfileManager.Instance == null) return;
            var profile = ProfileManager.Instance.profile;
            Debug.Log($"Рубли: {profile.currentResources}");

            // Сначала строим колоду (нужна и для новой игры, и для DrawCard потом)
            BuildDeck();

            if (profile.isFirstRun)
            {
                Debug.Log("Первый запуск. Подготовка новой фермы...");
                profile.isFirstRun = false;
                profile.currentResources = startingResources;
                // Раздаем 5 стартовых карт
                for (int i = 0; i < 5; i++) DrawCard();
            }
            else
            {
                Debug.Log("Загрузка сохраненной базы...");
                LoadGridState(profile.placedCardsOnGrid);
                LoadHandState(profile.hand);
            }

            OnResourcesChanged?.Invoke(GetCurrentResources());
        }

        // --- ЛОГИКА КОЛОДЫ ---
        private void BuildDeck()
        {
            if (mainDatabase == null) return;
            deck.Clear();
            foreach (var cardTemplate in mainDatabase.allCards)
            {
                deck.Add(cardTemplate.Clone());
            }
        }

        private CardData GetRandomCardData()
        {
            if (deck.Count == 0) return null;
            return deck[UnityEngine.Random.Range(0, deck.Count)].Clone();
        }

        public void DrawCard()
        {
            CardData data = GetRandomCardData();
            if (data != null) DrawSpecificCard(data);
        }

        public void DrawSpecificCard(CardData cardData)
        {
            if (playerHand.Count >= playerHand.maxHandSize) return;
            CardView newCard = Instantiate(cardPrefab, playerHand.handParent);
            newCard.Initialize(cardData);
            playerHand.AddCard(newCard);
        }

        // --- СОХРАНЕНИЕ / ЗАГРУЗКА ---
        public void SaveState()
        {
            if (ProfileManager.Instance == null) return;
            var profile = ProfileManager.Instance.profile;

            // Сохраняем сетку
            profile.placedCardsOnGrid.Clear();
            // Сортируем слоты по позиции, чтобы порядок всегда был одинаковым
            GridSlot[] slots = FindObjectsByType<GridSlot>(FindObjectsSortMode.None)
                                .OrderBy(s => s.transform.position.y)
                                .ThenBy(s => s.transform.position.x).ToArray();

            foreach (var slot in slots)
            {
                profile.placedCardsOnGrid.Add(slot.IsEmpty ? null : slot.GetCard().cardData);
            }

            // Сохраняем руку
            profile.hand.Clear();
            foreach (var cardView in playerHand.GetCards())
            {
                profile.hand.Add(cardView.cardData);
            }

            Debug.Log("Состояние базы сохранено!");
        }

        private void LoadGridState(List<CardData> savedCards)
        {
            GridSlot[] slots = FindObjectsByType<GridSlot>(FindObjectsSortMode.None)
                                .OrderBy(s => s.transform.position.y)
                                .ThenBy(s => s.transform.position.x).ToArray();

            for (int i = 0; i < Mathf.Min(savedCards.Count, slots.Length); i++)
            {
                if (savedCards[i] != null)
                {
                    CardView newCard = Instantiate(cardPrefab, slots[i].transform);
                    newCard.Initialize(savedCards[i]);
                    slots[i].PlaceCard(newCard);
                }
            }
        }

        private void LoadHandState(List<CardData> savedCards)
        {
            foreach (var data in savedCards) DrawSpecificCard(data);
        }

        // --- ЭКОНОМИКА И СОБЫТИЯ ---
        public int GetCurrentResources() => ProfileManager.Instance != null ? ProfileManager.Instance.profile.currentResources : startingResources;

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

        public void OnCardPlaced(CardView card, GridSlot slot) => playerHand.RemoveCard(card);

        public void OnSlotClicked(GridSlot slot)
        {
            selectedSlot = slot;
            OnSlotSelected?.Invoke(slot);
        }

        public void NotifyStatsChanged() => OnStatsChanged?.Invoke();
    }
}
