using UnityEngine;
using UnityEngine.UI;

namespace ReplicaProjects.Common
{
    [System.Serializable]
    public class EndScreenObjectHolder
    {
        public GameObject screenBanner;
        public Image screenButtonBackground;

        public void Set(bool activeness)
        {
            screenBanner.SetActive(activeness);
            screenButtonBackground.enabled = activeness;
        }
    }
}

