using UnityEngine;

namespace ReplicaProjects.Arrows
{
    public class HealthOrchestrator
    {
        public event System.Action Died;

        public int currentHealth;
        private int _maxHealth;

        public void Initialize(int maxhealth)
        {
            _maxHealth = maxhealth;
            currentHealth = maxhealth;
        }

        public void DeInitialize()
        {
            _maxHealth = 0;
            currentHealth = 0;
            Died = null;
        }

        public void DecreaseHealth()
        {
            currentHealth--;
            currentHealth = Mathf.Clamp(currentHealth, 0, _maxHealth);

            if (currentHealth != 0)
                return;

            Died?.Invoke();
        }
    }
}
