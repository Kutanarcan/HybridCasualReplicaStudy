using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ReplicaProjects.MagicSort
{
    /// <summary>
    /// Collider'i olan Bolt objesine ekle. Tiklaninca PointerDown event'i atar.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class BoltInteraction : MonoBehaviour, IPointerDownHandler
    {
        public event Action PointerDown;

        public void OnPointerDown(PointerEventData eventData)
        {
            PointerDown?.Invoke();
        }
    }
}
