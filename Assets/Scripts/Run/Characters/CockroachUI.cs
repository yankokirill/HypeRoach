using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace Game.Run
{
    public class CockroachUI : MonoBehaviour
    {
        [SerializeField] private Cockroach targetRacer;
        [SerializeField] private Slider hypeSlider;
        [SerializeField] private TextMeshProUGUI hpText;
        [SerializeField] private TextMeshProUGUI manaText;

        private void OnEnable()
        {
            if (targetRacer != null)
            {
                targetRacer.UpdateHypeUI += UpdateHype;
                if (targetRacer is PlayerCockroach player)
                {
                    player.OnManaChanged += UpdateMana;
                }
            }
        }

        private void OnDisable()
        {
            if (targetRacer != null)
            {
                targetRacer.UpdateHypeUI -= UpdateHype;
                if (targetRacer is PlayerCockroach player)
                {
                    player.OnManaChanged -= UpdateMana;
                }
            }
        }

        private void UpdateHype(int hype, int maxHype)
        {
            if (hypeSlider == null) return;

            // Если максимальное значение изменилось
            if (hypeSlider.maxValue != maxHype)
            {
                StartCoroutine(AnimateMaxValueChange(hype, maxHype));
            }
            // Если изменилось только текущее значение
            else if (hypeSlider.value != hype)
            {
                hypeSlider.DOValue(hype, 0.3f).SetEase(Ease.OutQuad);
            }
        }

        private IEnumerator AnimateMaxValueChange(int newHype, int newMaxHype)
        {
            float oldMax = hypeSlider.maxValue;
            float oldValue = hypeSlider.value;
            float duration = 0.3f;

            if (newHype >= oldMax)
            {
                // Анимируем текущее значение к старому максимуму (заполняем до конца)
                if (oldValue < oldMax)
                {
                    yield return hypeSlider.DOValue(oldMax, duration)
                        .SetEase(Ease.OutQuad)
                        .WaitForCompletion();
                }

                // Мгновенно меняем maxValue
                hypeSlider.maxValue = newMaxHype;
                hypeSlider.value = newMaxHype;

                // Анимируем к целевому значению
                yield return hypeSlider.DOValue(newHype, duration)
                    .SetEase(Ease.OutQuad)
                    .WaitForCompletion();
            }
            else
            {
                // Мгновенно меняем maxValue
                hypeSlider.maxValue = newMaxHype;
                hypeSlider.value = newMaxHype * (oldValue / oldMax);

                // Анимируем к целевому значению
                yield return hypeSlider.DOValue(newHype, duration)
                    .SetEase(Ease.OutQuad)
                    .WaitForCompletion();
            }
        }

        private void UpdateHealth(int hp)
        {
            if (hpText != null) hpText.text = hp.ToString();
        }

        private void UpdateMana(int mana)
        {
            if (manaText != null) manaText.text = mana.ToString();
        }
    }
}
