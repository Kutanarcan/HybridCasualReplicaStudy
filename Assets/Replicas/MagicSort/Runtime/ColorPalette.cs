using System.Collections.Generic;
using UnityEngine;

namespace ReplicaProjects.MagicSort
{
    [CreateAssetMenu(fileName = "ColorPalette", menuName = "MagicSort/Create Color Palette")]
    public class ColorPalette : ScriptableObject
    {
        public List<Color> colorList;
    }
}
