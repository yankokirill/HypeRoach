using UnityEngine;
using System.Collections.Generic;

namespace Game.Core
{
    [System.Serializable]
    public class PlayerProfile
    {
        [Header("Economy & Win Condition")]
        public int currentResources = 500;
        public int totalPopulation = 0; // Reach 1000 to win!

        [Header("Cockroach Champion Stats")]
        public int baseIQ = 0;
        public int baseCharisma = 0;

        [Header("Save State")]
        public bool isFirstRun = true;
        public List<Base.CardData> placedCardsOnGrid = new List<Base.CardData>();
        public List<Base.CardData> hand = new List<Base.CardData>();

        public void ValidateStats()
        {
            if (currentResources < 0) currentResources = 0;
            if (totalPopulation < 0) totalPopulation = 0;
            if (baseIQ < 0) baseIQ = 0;
            if (baseCharisma < 0) baseCharisma = 0;
        }
    }

    public class ProfileManager : MonoBehaviour
    {
        public static ProfileManager Instance { get; private set; }

        public PlayerProfile profile;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                if (profile == null) profile = new PlayerProfile();
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}
