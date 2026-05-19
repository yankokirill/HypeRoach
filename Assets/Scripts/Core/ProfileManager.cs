using UnityEngine;
using System.Collections.Generic;
using Game.Base;

namespace Game.Core
{
    public enum RunResult { None, Victory, Defeat }

    [System.Serializable]
    public class PlayerProfile
    {
        public int currentResources = 500;
        public int currentIQ = 0;
        public int currentCharisma = 0;
        public int totalPopulation = 0;
        public int populationGrowth = 10;
        public bool isFirstRun = true;
        public int result = 0;
        public int dayCount = 0;

        public RunResult lastRunResult = RunResult.None;
        public bool startingDialoguePlayed = false;

        /// <summary>
        /// Накопленный постоянный бонус к базовому хайпу за стикер.
        /// Растёт на +2 за каждое поражение с апгрейдом DedInside.
        /// </summary>
        public int dedInsideHypeBonus = 0;

        /// <summary>
        /// Карта таракана с центрального слота (1,1) — передаётся в раннер.
        /// null = слот пустой, используются дефолтные статы.
        /// </summary>
        public SavedFieldCard centerCockroach = null;

        public List<SavedFieldCard> placedCardsOnGrid = new List<SavedFieldCard>();
        public List<string> hand = new List<string>();

        public void ValidateStats()
        {
            if (currentResources < 0) currentResources = 0;
            if (totalPopulation < 0) totalPopulation = 0;
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

        public void ResetProfile()
        {
            profile = new PlayerProfile();
            profile.dayCount = 1;
        }
    }
}
