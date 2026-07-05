using System;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Test: Collider'i olan Bolt objesine ekle.
/// Tiklaninca console'a log atar ve objeyi kisa sure sarartir.
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