using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ReplicaProjects.MagicSort
{
    /// <summary>
    /// Collider2D'si olan Tube objesine ekle. Tiklaninca PointerDown event'i atar.
    /// Kamerada Physics2DRaycaster + sahnede EventSystem gerektirir.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class TubeInteraction : MonoBehaviour, IPointerDownHandler
    {
        public event Action PointerDown;

        public void OnPointerDown(PointerEventData eventData)
        {
            PointerDown?.Invoke();
        }
    }
}
