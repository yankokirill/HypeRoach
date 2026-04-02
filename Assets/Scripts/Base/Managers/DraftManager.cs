using UnityEngine;
using System.Collections.Generic;

namespace Game.Base
{
    public class DraftManager : MonoBehaviour
    {
        [Header("UI References")]
        public GameObject draftPanel;
        public Transform draftCardsParent;
        public CardView cardPrefab;

        [Header("References")]
        public GameManager gameManager;
        public CardDatabase database;

        public bool IsDrafting => draftPanel != null && draftPanel.activeInHierarchy;

        private void Start()
        {
            if (draftPanel != null)
                draftPanel.SetActive(false);
        }

        public void StartDraft()
        {
            if (database == null || database.allCards.Count == 0)
            {
                Debug.LogError("DraftManager: Database is missing or empty!");
                return;
            }

            draftPanel.SetActive(true);

            // Очистка старых карт
            foreach (Transform child in draftCardsParent)
                Destroy(child.gameObject);

            // Создаем 3 карты для выбора
            for (int i = 0; i < 3; i++)
            {
                CardData randomData = database.GetRandomCard();
                if (randomData == null) continue;

                // Клонируем данные, чтобы не менять оригинал в ассетах
                CardData runtimeData = randomData.Clone();

                CardView newCard = Instantiate(cardPrefab, draftCardsParent);

                // 1. Инициализируем данные
                newCard.Initialize(runtimeData);

                // 2. Устанавливаем увеличенный масштаб для карты из окна выбора
                newCard.SetScale(1.5f);

                // 3. Подписываемся на клик
                newCard.OnCardClicked = (clickedCard) => OnCardSelected(runtimeData);
            }
        }

        private void OnCardSelected(CardData selectedData)
        {
            if (gameManager.playerHand.Count < gameManager.playerHand.maxHandSize)
            {
                Debug.Log($"Drafted: {selectedData.cardName}");

                // Создаем карту для руки
                CardView newHandCard = Instantiate(cardPrefab, gameManager.playerHand.handParent);
                newHandCard.Initialize(selectedData);
                newHandCard.SetScale(1.0f);

                gameManager.playerHand.AddCard(newHandCard);
            }
            else
            {
                Debug.LogWarning("Hand is full! Card discarded.");
            }

            draftPanel.SetActive(false);
        }
    }
}
