using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Voxel
{
    public class CustomBrush : MonoBehaviour
    {
        public bool NeedsRebuild
        {
            private set;
            get;
        }

        public CustomBrushSdf CreateSdf()
        {
            return new CustomBrushSdf
            {

            };
        }
    }
}
