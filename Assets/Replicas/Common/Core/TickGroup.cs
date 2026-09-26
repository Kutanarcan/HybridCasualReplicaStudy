using System.Collections.Generic;

namespace ReplicaProjects.Common
{
    /// <summary>Ticks its members in registration order. The composition root forwards Update here.</summary>
    public sealed class TickGroup : ITickable
    {
        private readonly List<ITickable> _members = new();

        public void Add(ITickable tickable) => _members.Add(tickable);

        public void Tick(float deltaTime)
        {
            for (int i = 0; i < _members.Count; i++)
                _members[i].Tick(deltaTime);
        }
    }
}
