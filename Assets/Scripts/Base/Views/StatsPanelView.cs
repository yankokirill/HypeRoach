using TMPro;
using UnityEngine;

namespace Game.Base
{
    public class StatsPanelView : MonoBehaviour
    {
        [Header("Text Fields")]
        public TextMeshProUGUI populationText;

        public void SetStats(int currentPop)
        {
            if (populationText != null)
                populationText.text = $"{currentPop} / 1054";
        }
    }
}
