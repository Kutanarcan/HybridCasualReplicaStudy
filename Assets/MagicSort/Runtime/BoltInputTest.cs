using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Test: Collider'i olan Bolt objesine ekle.
/// Tiklaninca console'a log atar ve objeyi kisa sure sarartir.
/// </summary>
[RequireComponent(typeof(Collider))]
public class BoltInputTest : MonoBehaviour, IPointerDownHandler
{
    private Renderer rend;
    private Color originalColor;

    private void Awake()
    {
        rend = GetComponentInChildren<Renderer>();
        if (rend != null) originalColor = rend.material.color;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        Debug.Log($"Bolt tiklandi: {name} | ekran pozisyonu: {eventData.position}");

        if (rend != null)
        {
            CancelInvoke(nameof(ResetColor));
            rend.material.color = Color.yellow;
            Invoke(nameof(ResetColor), 0.2f);
        }
    }

    private void ResetColor()
    {
        if (rend != null) rend.material.color = originalColor;
    }
}