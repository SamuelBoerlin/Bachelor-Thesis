using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Voxel
{
    public abstract class CustomBrushContainer<TIndexer, TBrushType, TEvaluator> : MonoBehaviour
        where TIndexer : struct, IIndexer
        where TBrushType : struct
        where TEvaluator : struct, IBrushSdfEvaluator<TBrushType>
    {
        private CustomBrush<TBrushType, TEvaluator> _instance;
        public CustomBrush<TBrushType, TEvaluator> Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new CustomBrush<TBrushType, TEvaluator>(CreateBrushSdfEvaluator());
                }
                return _instance;
            }
        }

        protected CustomBrushSdfRenderer<TIndexer, TBrushType, TEvaluator> _customBrushRenderer;
        public CustomBrushSdfRenderer<TIndexer, TBrushType, TEvaluator> CustomBrushRenderer
        {
            get
            {
                return _customBrushRenderer;
            }
        }

        protected abstract TEvaluator CreateBrushSdfEvaluator();

        protected virtual void Awake()
        {
            _customBrushRenderer = GetComponent<CustomBrushSdfRenderer<TIndexer, TBrushType, TEvaluator>>();
        }
    }
}
