using UnityEngine;
using UnityEngine.UI;

namespace ReplicaProjects.MagicSort
{
    public class InGameUI : MonoBehaviour
    {
        public event System.Action RestartLevelButtonPressed;
        public event System.Action SwitchThemePressed;

        [SerializeField] private Canvas _Canvas;
        [SerializeField] private Button _RestartButton;
        [SerializeField] private Button _SwitchThemeButton;

        private void Awake()
        {
            _RestartButton.onClick.AddListener(OnRestartLevelButtonPressed);
            _SwitchThemeButton.onClick.AddListener(OnSwitchThemeButtonPressed);
        }

        private void OnRestartLevelButtonPressed()
        {
            RestartLevelButtonPressed?.Invoke();
        }

        private void OnSwitchThemeButtonPressed()
        {
            SwitchThemePressed?.Invoke();
        }

        private void OnDestroy()
        {
            _RestartButton.onClick.RemoveAllListeners();
            _SwitchThemeButton.onClick.RemoveAllListeners();
        }

        public void Show() => _Canvas.enabled = true;
        public void Hide() => _Canvas.enabled = false;

    }
}
