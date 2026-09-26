using DG.Tweening;
using UnityEngine;

namespace ReplicaProjects.Arrows
{
    /// <summary>Copies the Core camera rig onto the Unity camera each frame and layers a shake on top.</summary>
    public class CameraController : MonoBehaviour
    {
        [SerializeField] private float _shakeStrength = 0.35f;
        [SerializeField] private float _shakeDuration = 0.25f;

        private Camera _camera;
        private CameraRig _rig;

        // Shake is layered on top of the rig's clamped position, so clamping never cancels it.
        private Vector3 _shakeOffset;
        private Tween _shakeTween;

        public void Initialize(Camera camera, CameraRig rig)
        {
            _camera = camera;
            _rig = rig;
        }

        public void DeInitialize()
        {
            _shakeTween?.Kill();
            _shakeOffset = Vector3.zero;
        }

        private void LateUpdate()
        {
            if (_rig == null)
                return;

            var z = _camera.transform.position.z;
            _camera.orthographicSize = _rig.OrthographicSize;
            _camera.transform.position = new Vector3(_rig.X, _rig.Y, z) + _shakeOffset;
        }

        // Quick decaying random shake, e.g. on a wrong answer.
        public void Shake()
        {
            _shakeTween?.Kill();

            float strength = _shakeStrength;
            _shakeTween = DOTween.To(() => strength, x => strength = x, 0f, _shakeDuration)
                .SetEase(Ease.OutQuad)
                .OnUpdate(() => _shakeOffset = Random.insideUnitCircle * strength)
                .OnComplete(() => _shakeOffset = Vector3.zero)
                .SetLink(gameObject);
        }
    }
}
