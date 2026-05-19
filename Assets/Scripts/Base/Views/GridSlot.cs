using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.Base
{
    [RequireComponent(typeof(Button))]
    public class GridSlot : MonoBehaviour, IDropHandler
    {
        [Header("Slot Coordinates")]
        public int gridX;
        public int gridY;

        [Header("UI References")]
        [SerializeField] private Transform cardHolder;

        private CardView placedCard;
        private Button slotButton;

        public bool IsEmpty => placedCard == null;

        public Transform CardHolder => cardHolder;

        private void Awake()
        {
            slotButton = GetComponent<Button>();
            slotButton.onClick.AddListener(OnSlotClicked);
        }

        public void OnDrop(PointerEventData eventData)
        {
            CardView dragged = eventData.pointerDrag?.GetComponentInParent<CardView>();
            if (dragged == null) return;

            // ── Карта из руки ─────────────────────────────────────────────────────
            if (dragged.currentHand != null)
            {
                HandCardData handData = dragged.handData;
                if (handData == null) return;

                if (handData.type == CardType.Building)
                {
                    if (!IsEmpty)
                    {
                        Debug.Log("Building можно класть только на пустой слот.");
                        return;
                    }

                    // ИСПРАВЛЕНИЕ БАГА: Сначала извлекаем карту из руки (удаляем из списка)
                    // Делаем это ДО PlaceCard, иначе currentHand станет null!
                    Hand previousHand = dragged.currentHand;
                    previousHand.RemoveCard(dragged);

                    PlaceCard(dragged, FieldCard.FromHand(handData));

                    // ИСПРАВЛЕНИЕ NRE: Безопасное обращение к GameManager
                    if (GameManager.Instance != null)
                    {
                        // Если OnCardPlaced — это метод, то оставляем так.
                        // Если это Action/delegate, замените строку ниже на: GameManager.Instance.OnCardPlaced?.Invoke(dragged, this);
                        GameManager.Instance.OnCardPlaced(dragged, this);
                    }
                    else
                    {
                        Debug.LogError("GameManager.Instance равен null! Убедитесь, что GameManager инициализирован на сцене.");
                    }
                }
                else // Upgrade
                {
                    if (IsEmpty)
                    {
                        Debug.Log("Upgrade можно применять только к зданиям.");
                        return;
                    }
                    if (GetCard().fieldData.TryApplyUpgrade(handData))
                    {
                        dragged.currentHand.RemoveCard(dragged);
                        Object.Destroy(dragged.gameObject);
                        GetCard().Refresh();

                        if (GameManager.Instance != null)
                            GameManager.Instance.NotifyStatsChanged();
                    }
                }
                return;
            }

            // ── Building с другого слота ──────────────────────────────────────────
            if (dragged.currentSlot != null)
            {
                GridSlot sourceSlot = dragged.currentSlot;
                if (sourceSlot == this) return;

                if (IsEmpty)
                {
                    PlaceCard(dragged, dragged.fieldData);
                    sourceSlot.RemoveCard();

                    if (GameManager.Instance != null)
                        GameManager.Instance.NotifyStatsChanged();
                }
                else
                {
                    if (!TryMerge(GetCard(), dragged, sourceSlot))
                    {
                        Debug.Log("Слияние невозможно: уровни не совпадают или карта прокачана.");
                    }
                }
            }
        }

        private bool TryMerge(CardView baseView, CardView dragView, GridSlot sourceSlot)
        {
            if (!baseView.fieldData.TryMerge(dragView.fieldData)) return false;

            sourceSlot.RemoveCard();
            Object.Destroy(dragView.gameObject);

            baseView.Refresh();

            if (GameManager.Instance != null)
                GameManager.Instance.NotifyStatsChanged();

            return true;
        }

        public void PlaceCard(CardView card, FieldCard fc)
        {
            if (!IsEmpty && placedCard != card)
            {
                Debug.LogWarning($"GridSlot ({gridX},{gridY}) уже занят другой картой — PlaceCard отклонён.");
                return;
            }

            card.InitAsFieldCard(fc);

            placedCard = card;
            card.SetSlot(this); // Здесь card.currentHand становится null
            card.transform.SetParent(cardHolder, false);

            RectTransform rt = card.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            card.transform.localScale = Vector3.one;
        }

        public void RemoveCard()
        {
            placedCard = null;
        }

        public CardView GetCard() => placedCard;

        private void OnSlotClicked()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnSlotClicked(this);
        }
    }
}
