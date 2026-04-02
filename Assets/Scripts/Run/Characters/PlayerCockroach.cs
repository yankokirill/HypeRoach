using System;
using UnityEngine;

namespace Game.Run
{
    public class PlayerCockroach : Cockroach
    {
        [Header("Player Stats")]
        public int mana = 10;

        public event Action<int> OnManaChanged;

        private void Start()
        {
            OnManaChanged?.Invoke(mana);
        }

        public override void Initialize()
        {
            base.Initialize();
            OnManaChanged?.Invoke(mana);
        }

        public override void UpdateState()
        {
            if (!isRacing) return;
        }

        private void FixedUpdate()
        {
            Move();
        }

        public void AddMana(int amount)
        {
            mana += amount;
            OnManaChanged?.Invoke(mana);
        }
    }
}
