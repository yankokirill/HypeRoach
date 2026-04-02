namespace Game.Run.Effects
{
    [System.Serializable]
    public class TauntEffect : IEffect
    {
        public float pullBackForce = 5f; // —ила отбрасывани€ назад

        public bool Check(PlayerCockroach player, EnemyCockroach[] enemies) => true;

        public void Apply(PlayerCockroach player, EnemyCockroach[] enemies)
        {
            foreach (var enemy in enemies)
            {
                if (enemy == null) continue;

                // ѕерет€гиваем на линию игрока
                enemy.ChangeLane(player.currentLane);

                // ≈сли враг впереди Ч толкаем его назад физически корректно
                if (enemy.transform.position.x > player.transform.position.x)
                {
                    enemy.BumpBack(pullBackForce);
                }

                // 1,5 секунды нельз€ сменить линию
                enemy.LockLaneChanging(1.5f);
            }
        }
    }
}
