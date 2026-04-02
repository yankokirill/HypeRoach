using UnityEngine;
using UnityEngine.UI;

namespace Game.Base
{
    public class StatsPanelView : MonoBehaviour
    {
        [Header("Text Fields")]
        public Text populationText;
        public Text championIQText;
        public Text championCharismaText;

        public void SetStats(int currentPop, int currentIQ, int currentCharisma)
        {
            if (populationText != null)
                populationText.text = $"{currentPop} / 1000";

            if (championIQText != null)
                championIQText.text = currentIQ.ToString();

            if (championCharismaText != null)
                championCharismaText.text = currentCharisma.ToString();
        }
    }
}
