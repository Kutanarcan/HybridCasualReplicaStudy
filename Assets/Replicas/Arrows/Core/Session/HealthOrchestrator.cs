using System;

namespace ReplicaProjects.Arrows
{
    public class HealthOrchestrator
    {
        public int CurrentHealth { get; private set; }
        public bool IsDead => CurrentHealth == 0;

        public void Initialize(int maxHealth) => CurrentHealth = maxHealth;

        public void DecreaseHealth() => CurrentHealth = Math.Max(0, CurrentHealth - 1);
    }
}
