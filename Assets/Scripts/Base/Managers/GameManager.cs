using System;
using System.Collections.Generic;
using UnityEngine;
using Game.Core;

namespace Game.Base
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }
        
        [Header("Prefabs")]
        [SerializeField] private CardView cardPrefab;

        [Header("Card System")]
        [SerializeField] private CardDatabase mainDatabase;

        [Header("Hierarchy References")]
        public Transform dragLayer;
        public Hand playerHand;

        [Header("Game Settings")]
        [SerializeField] private int startingResources = 500;

        // ── Events (For UI to listen to) ─────────────────────────────
        public event Action<int> OnResourcesChanged;
        public event Action<GridSlot> OnSlotSelected;
        public event Action OnStatsChanged;

        // ── Core State ───────────────────────────────────────────────
        private GridSlot selectedSlot;
        private List<CardData> deck = new List<CardData>();

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void Start()
        {
            Initialize();
        }

        private void Initialize()
        {
            if (ProfileManager.Instance != null && ProfileManager.Instance.profile.totalPopulation == 0)
            {
                ProfileManager.Instance.profile.currentResources = startingResources;
            }

            BuildDeck();

            for (int i = 0; i < 5; i++) DrawCard();

            OnResourcesChanged?.Invoke(GetCurrentResources());
        }

        private void BuildDeck()
        {
            if (mainDatabase == null)
            {
                Debug.LogError("Main Database не назначена в GameManager!");
                return;
            }

            // Наполняем игровую колоду клонами данных из базы
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
            if (playerHand.Count >= playerHand.maxHandSize) return;
            CardData newCardData = GetRandomCardData();
            if (newCardData == null) return;

            CardView newCard = Instantiate(cardPrefab, playerHand.handParent);
            newCard.Initialize(newCardData);
            playerHand.AddCard(newCard);
        }

        public void OnCardPlaced(CardView card, GridSlot slot)
        {
            playerHand.RemoveCard(card);
        }

        public void OnSlotClicked(GridSlot slot)
        {
            selectedSlot = slot;
            OnSlotSelected?.Invoke(slot);
        }

        public int GetCurrentResources()
        {
            return ProfileManager.Instance != null ? ProfileManager.Instance.profile.currentResources : startingResources;
        }

        public bool CanAfford(int amount) => GetCurrentResources() >= amount;

        public void AddResources(int amount)
        {
            if (ProfileManager.Instance != null)
            {
                ProfileManager.Instance.profile.currentResources += amount;
                OnResourcesChanged?.Invoke(ProfileManager.Instance.profile.currentResources);
            }
        }

        public bool SpendResources(int amount)
        {
            if (!CanAfford(amount)) return false;

            if (ProfileManager.Instance != null)
            {
                ProfileManager.Instance.profile.currentResources -= amount;
                OnResourcesChanged?.Invoke(ProfileManager.Instance.profile.currentResources);
                return true;
            }
            return false;
        }

        public void NotifyStatsChanged()
        {
            OnStatsChanged?.Invoke();
        }
    }
}
