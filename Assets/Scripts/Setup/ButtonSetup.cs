using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Настройка кнопки удаления: задаёт красные цвета, текст, размеры и позицию.
/// Работает с компонентами Image и Button, а также опционально с Text.
/// </summary>
[RequireComponent(typeof(Button))]
[RequireComponent(typeof(Image))]
public class RemoveButtonSetup : MonoBehaviour
{
    [Header("🔴 Цвета кнопки (красная схема)")]
    [Tooltip("Основной цвет кнопки в обычном состоянии")]
    public Color normalColor = new Color(0.85f, 0.25f, 0.25f, 1f);        // #D94040 — насыщенный красный

    [Tooltip("Цвет при наведении (светлее)")]
    public Color highlightedColor = new Color(0.92f, 0.35f, 0.35f, 1f);   // #EB5959 — яркий красный

    [Tooltip("Цвет при нажатии (темнее)")]
    public Color pressedColor = new Color(0.65f, 0.15f, 0.15f, 1f);       // #A62626 — тёмный красный

    [Tooltip("Цвет при выделении (альтернативный ховер)")]
    public Color selectedColor = new Color(0.88f, 0.30f, 0.30f, 1f);      // #E04D4D — средний красный

    [Tooltip("Цвет в неактивном состоянии (приглушённый)")]
    public Color disabledColor = new Color(0.50f, 0.30f, 0.30f, 0.6f);    // #804D4D + прозрачность

    [Header("⚙️ Параметры перехода цветов")]
    [Range(0.1f, 3f)]
    public float colorMultiplier = 1f;           // Множитель яркости (не трогайте, если не уверены)
    [Range(0.05f, 1f)]
    public float fadeDuration = 0.1f;            // Скорость перехода между состояниями

    [Header("🖼️ Параметры изображения")]
    public Sprite sprite;                        // Можно задать спрайт для кнопки
    public Image.Type imageType = Image.Type.Simple;
    [Tooltip("Если включено — спрайт будет масштабироваться с сохранением пропорций")]
    public bool preserveAspect = false;

    [Header("📝 Параметры текста (опционально)")]
    public string buttonText = "✕ Remove";       // Текст кнопки (✕ — символ удаления)
    public Color textColor = Color.white;        // Белый текст на красном фоне
    public int fontSize = 18;
    public Font customFont;                      // Опционально: свой шрифт

    [Header("📐 Позиция и размер (опционально)")]
    public Vector2 anchoredPosition = new Vector2(-30, 360);
    public Vector2 sizeDelta = new Vector2(350, 80);
    public Vector2 pivot = new Vector2(1f, 0f);  // По умолчанию: привязка к правому нижнему углу

    [Header("🎯 Дополнительно")]
    [Tooltip("Если включено — кнопка будет реагировать на клики")]
    public bool interactable = true;
    [Tooltip("Показывать подтверждение при нажатии (требует дополнительной логики)")]
    public bool requireConfirmation = false;

    private Button button;
    private Image image;
    private Text textComponent;

    private void Awake()
    {
        ApplySetup();
    }

    /// <summary>
    /// Применяет настройки к компонентам. Можно вызвать вручную из инспектора.
    /// </summary>
    [ContextMenu("🔧 Apply Setup")]
    public void ApplySetup()
    {
        // Получаем или добавляем необходимые компоненты
        button = GetComponent<Button>();
        image = GetComponent<Image>();
        textComponent = GetComponentInChildren<Text>();

        // Настраиваем RectTransform
        RectTransform rect = GetComponent<RectTransform>();
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;
        rect.pivot = pivot;  // Важно для правильного масштабирования!

        // Настраиваем Image
        if (image != null)
        {
            image.sprite = sprite;
            image.type = imageType;
            image.preserveAspect = preserveAspect;
            image.color = normalColor;          // Начальный цвет изображения
            image.raycastTarget = true;         // Кнопка должна получать клики
        }

        // Настраиваем Button
        if (button != null)
        {
            // Создаём ColorBlock и заполняем красными оттенками
            ColorBlock colors = button.colors;
            colors.normalColor = normalColor;
            colors.highlightedColor = highlightedColor;
            colors.pressedColor = pressedColor;
            colors.selectedColor = selectedColor;
            colors.disabledColor = disabledColor;
            colors.colorMultiplier = colorMultiplier;
            colors.fadeDuration = fadeDuration;
            button.colors = colors;

            // Устанавливаем графический элемент, на который действует кнопка
            if (image != null)
                button.targetGraphic = image;

            // Интерактивность
            button.interactable = interactable;

            // Опционально: добавляем визуальный отклик при клике
            if (requireConfirmation && button.onClick.GetPersistentEventCount() == 0)
            {
                button.onClick.AddListener(OnRemoveClicked);
            }
        }

        // Настраиваем текст (если есть)
        if (textComponent != null)
        {
            textComponent.text = buttonText;
            textComponent.color = textColor;
            textComponent.fontSize = fontSize;
            textComponent.alignment = TextAnchor.MiddleCenter;

        // Применяем кастомный шрифт, если задан
            if (customFont != null)
                textComponent.font = customFont;
        }
    }

    /// <summary>
    /// Обработчик нажатия (если включено requireConfirmation)
    /// </summary>
    private void OnRemoveClicked()
    {
        // Здесь можно добавить логику подтверждения удаления
        Debug.Log($"[RemoveButton] Запрошено удаление: {gameObject.name}");

        // Пример: показать диалог подтверждения
        // ConfirmationDialog.Show("Удалить элемент?", ConfirmRemove, CancelRemove);
    }

    private void ConfirmRemove()
    {
        Debug.Log("[RemoveButton] Подтверждено — удаляем!");
        // Логика удаления
    }

    private void CancelRemove()
    {
        Debug.Log("[RemoveButton] Отменено");
    }

    // В редакторе: кнопка для быстрого применения настроек
    private void OnValidate()
    {
        if (Application.isPlaying) return;

        // Опционально: авто-применение при изменении в инспекторе
        // (раскомментируйте, если хотите видеть изменения в реальном времени)
        // ApplySetup();
    }

    // Визуальная подсказка в инспекторе
    private void Reset()
    {
        Debug.Log("💡 RemoveButtonSetup: Нажмите [🔧 Apply Setup] в контекстном меню компонента, чтобы применить настройки.");
    }
}
