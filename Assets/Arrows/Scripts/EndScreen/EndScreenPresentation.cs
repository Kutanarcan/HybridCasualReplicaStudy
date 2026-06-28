using UnityEngine;
using UnityEngine.UI;

namespace ReplicaProjects.Arrows
{
    public class EndScreenPresentation : MonoBehaviour
    {
        public event System.Action InteractionButtonPressed;

        [SerializeField] private Canvas _Canvas;
        [SerializeField] private EndScreenObjectHolder _VictoryScreenObjectHolder;
        [SerializeField] private EndScreenObjectHolder _DefeatScreenObjectHolder;
        [SerializeField] private Button _InteractionButton;

        private void Awake()
        {
            _Canvas.enabled = false;
            _InteractionButton.onClick.AddListener(OnInteractionButtonPressed);
        }

        private void OnInteractionButtonPressed()
        {
            InteractionButtonPressed?.Invoke();
            _Canvas.enabled = false;
        }

        private void OnDestroy()
        {
            _InteractionButton.onClick.RemoveAllListeners();
        }

        public void SetState(bool isVictory)
        {
            _Canvas.enabled = true;

            _VictoryScreenObjectHolder.Set(isVictory);
            _DefeatScreenObjectHolder.Set(!isVictory);

            _InteractionButton.targetGraphic = isVictory ? _VictoryScreenObjectHolder.screenButtonBackground : _DefeatScreenObjectHolder.screenButtonBackground;
        }
    }
}
