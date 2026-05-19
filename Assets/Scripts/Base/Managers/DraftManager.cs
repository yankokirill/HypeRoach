using System.Collections.Generic;
using UnityEngine;

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
            if (draftPanel != null) draftPanel.SetActive(false);
        }

        public void StartDraft()
        {
            if (database == null || database.buildings.Count == 0)
            {
                Debug.LogError("DraftManager: база пустая!");
                return;
            }

            draftPanel.SetActive(true);

            foreach (Transform child in draftCardsParent)
                Destroy(child.gameObject);

            HandCardData data = database.GetRandomBuilding();

            CardView card = Instantiate(cardPrefab, draftCardsParent);
            card.InitAsHandCard(data);

            HandCardData captured = data;
            card.OnCardClicked = _ => OnCardSelected(captured);
        }

        private void OnCardSelected(HandCardData data)
        {
            if (gameManager.playerHand.Count < gameManager.playerHand.maxHandSize)
                gameManager.SpawnHandCard(data);
            else
                Debug.LogWarning("Рука полная, карта сброшена.");

            draftPanel.SetActive(false);
        }
    }
}
