using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ReplicaProjects.MagicSort
{
    [CreateAssetMenu(fileName ="ColorPallete", menuName ="MagicSort/Create Color Pallete")]
    public class ColorPallete : ScriptableObject
    {
        public List<Color> colorList;
    }
}
