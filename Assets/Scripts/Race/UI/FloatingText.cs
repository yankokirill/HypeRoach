using UnityEngine;
using TMPro;
using DG.Tweening;

namespace Game.Race
{
    public class FloatingText : MonoBehaviour
    {
        [SerializeField] private TextMeshPro textMesh;

        public void Setup(string text, Color color)
        {
            textMesh.text = text;
            textMesh.color = color;
            transform.localScale = Vector3.zero;

            Camera mainCam = Camera.main;
            if (mainCam == null) return;

            // 1. Рассчитываем лимит по высоте
            // Получаем мировые координаты верхней границы экрана (0.95 — это 5% отступ сверху)
            // z берем как дистанцию от камеры до объекта, чтобы расчет был точным
            float distanceToCam = Mathf.Abs(mainCam.transform.position.z - transform.position.z);
            Vector3 viewportTop = new Vector3(0.5f, 0.95f, distanceToCam);
            Vector3 worldTop = mainCam.ViewportToWorldPoint(viewportTop);

            float maxY = worldTop.y;

            // 2. Рассчитываем целевую позицию
            float desiredY = transform.position.y + 0.5f;

            // Ограничиваем, чтобы текст не улетал выше maxY
            float finalY = Mathf.Min(desiredY, maxY);

            // Если таракан УЖЕ под самым потолком, и двигаться некуда, 
            // немного опустим начальную точку спавна текста, чтобы он был виден
            if (finalY - transform.position.y < 0.5f)
            {
                transform.position = new Vector3(transform.position.x, maxY - 0.6f, transform.position.z);
                finalY = maxY;
            }

            // 3. Анимация
            Sequence s = DOTween.Sequence();

            // Появление (Scale)
            s.Append(transform.DOScale(1.2f, 0.2f).SetEase(Ease.OutBack));

            // Движение вверх до finalY
            s.Join(transform.DOMoveY(finalY, 1.2f).SetEase(Ease.OutCubic));

            // Небольшое случайное смещение по X для естественности
            s.Join(transform.DOBlendableMoveBy(new Vector3(Random.Range(-0.3f, 0.3f), 0, 0), 1.2f).SetEase(Ease.Linear));

            // Исчезновение
            s.Insert(0.8f, textMesh.DOFade(0, 0.4f));

            // Настройки безопасности
            s.SetUpdate(UpdateType.Late);
            s.SetLink(gameObject);

            s.OnComplete(() => {
                if (this != null && gameObject != null) Destroy(gameObject);
            });
        }
    }
}
