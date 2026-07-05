using UnityEngine;
using UnityEngine.UI;

namespace ReplicaProjects.MagicSort
{
    public class InGameUI : MonoBehaviour
    {
        public event System.Action InteractionButtonPressed;

        [SerializeField] private Canvas _Canvas;
        [SerializeField] private Button _InteractionButton;

        private void Awake()
        {
            _InteractionButton.onClick.AddListener(OnInteractionButtonPressed);
        }

        private void OnInteractionButtonPressed()
        {
            InteractionButtonPressed?.Invoke();
        }

        private void OnDestroy()
        {
            _InteractionButton.onClick.RemoveAllListeners();
        }
        public void Show() => _Canvas.enabled = true;
        public void Hide() => _Canvas.enabled = false;

    }
}
