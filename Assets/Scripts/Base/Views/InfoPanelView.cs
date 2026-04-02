using UnityEngine;
using UnityEngine.UI;

namespace Game.Base
{
    public class InfoPanelView : MonoBehaviour
    {
        [Header("Text Fields")]
        public Text titleText;
        public Text descriptionText;

        [Header("Default Text")]
        public string defaultTitle = "Выберите здание...";
        public string defaultDescription = "";

        public void SetTitle(string title)
        {
            titleText.text = title;
        }

        public void SetDescription(string description)
        {
            descriptionText.text = description;
        }

        public void SetInfo(string title, string description)
        {
            SetTitle(title);
            SetDescription(description);
        }

        public void SetDefault()
        {
            SetTitle(defaultTitle);
            SetDescription(PhraseManager.Instance.GetPhrase());
        }
    }
}
