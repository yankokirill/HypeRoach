// SavedFieldCard.cs
using System;
using System.Collections.Generic;

namespace Game.Base
{
    [Serializable]
    public class SavedFieldCard
    {
        public string cardID;
        public int level;
        public List<UpgradeType> upgrades = new List<UpgradeType>();

        public int totalGreen;
        public int totalWhite;
        public int totalYellow;
    }
}
