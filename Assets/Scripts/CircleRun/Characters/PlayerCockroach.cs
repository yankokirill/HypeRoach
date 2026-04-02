using UnityEngine;

namespace Game.CircleRun
{
    public class PlayerCockroach : Cockroach
    {
        [Header("Player Specific")]
        public int mana = 10;

        public override void UpdateState()
        {
            // Здесь можно добавить регенерацию маны или обновление UI
        }

        private void FixedUpdate()
        {
            Move();
        }

        public void AddMana(int amount)
        {
            mana += amount;
            UIManager.Instance.SetMana(mana);
        }
    }
}
