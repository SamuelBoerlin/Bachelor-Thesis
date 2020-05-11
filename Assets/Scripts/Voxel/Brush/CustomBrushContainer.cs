using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Voxel
{
    public class CustomBrushContainer : MonoBehaviour
    {
        private CustomBrush<DefaultCustomBrushType, DefaultCustomBrushSdfEvaluator> _instance;
        public CustomBrush<DefaultCustomBrushType, DefaultCustomBrushSdfEvaluator> Instance
        {
            get
            {
                if(_instance == null)
                {
                    _instance = new CustomBrush<DefaultCustomBrushType, DefaultCustomBrushSdfEvaluator>(new DefaultCustomBrushSdfEvaluator());
                    Initialise();
                }
                return _instance;
            }
        }

        private void Initialise()
        {
            Matrix4x4 globalTransform = Matrix4x4.identity;

            Instance.AddPrimitive(DefaultCustomBrushType.BOX, CreateVoxelTerrain.BrushOperation.Union, 5f, globalTransform * Matrix4x4.Translate(new Vector3(0.5f, 0.5f, 0.5f)));
            Instance.AddPrimitive(DefaultCustomBrushType.BOX, CreateVoxelTerrain.BrushOperation.Union, 5f, globalTransform * Matrix4x4.Translate(new Vector3(8.5f, 4.5f, 2.5f)));
            Instance.AddPrimitive(DefaultCustomBrushType.SPHERE, CreateVoxelTerrain.BrushOperation.Difference, 5.0f, globalTransform * Matrix4x4.Translate(new Vector3(8.5f, 4.5f, -3.5f)));

            /*Instance.AddPrimitive(DefaultCustomBrushType.SPHERE, CreateVoxelTerrain.BrushOperation.Union, 5f, globalTransform * Matrix4x4.Translate(new Vector3(0.5f, 0.5f, 0.5f)));
            Instance.AddPrimitive(DefaultCustomBrushType.BOX, CreateVoxelTerrain.BrushOperation.Difference, 2f, globalTransform * Matrix4x4.Translate(new Vector3(8.5f, 0.5f, 0.5f)));
            Instance.AddPrimitive(DefaultCustomBrushType.BOX, CreateVoxelTerrain.BrushOperation.Difference, 2f, globalTransform * Matrix4x4.Translate(new Vector3(0.5f, 8.5f, 0.5f)));
            Instance.AddPrimitive(DefaultCustomBrushType.BOX, CreateVoxelTerrain.BrushOperation.Difference, 2f, globalTransform * Matrix4x4.Translate(new Vector3(0.5f, 0.5f, 8.5f)));
            Instance.AddPrimitive(DefaultCustomBrushType.BOX, CreateVoxelTerrain.BrushOperation.Difference, 2f, globalTransform * Matrix4x4.Translate(new Vector3(-7.5f, 0.5f, 0.5f)));
            Instance.AddPrimitive(DefaultCustomBrushType.BOX, CreateVoxelTerrain.BrushOperation.Difference, 2f, globalTransform * Matrix4x4.Translate(new Vector3(0.5f, -7.5f, 0.5f)));
            Instance.AddPrimitive(DefaultCustomBrushType.BOX, CreateVoxelTerrain.BrushOperation.Difference, 2f, globalTransform * Matrix4x4.Translate(new Vector3(0.5f, 0.5f, -7.5f)));*/

            /*Instance.AddPrimitive(DefaultCustomBrushType.BOX, CreateVoxelTerrain.BrushOperation.Union, 5f, globalTransform * Matrix4x4.Translate(new Vector3(0.5f, 0.5f, 0.5f)));
            Instance.AddPrimitive(DefaultCustomBrushType.SPHERE, CreateVoxelTerrain.BrushOperation.Union, 3f, globalTransform * Matrix4x4.Translate(new Vector3(0.5f, 7.5f, 0.5f)));*/
        }

        public void OnApplicationQuit()
        {
            Instance.Dispose();
        }
    }
}
