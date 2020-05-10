using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using static CreateVoxelTerrain;

namespace Voxel
{
    /// <summary>
    /// A collection of custom brush primitives that form one custom compound SDF brush-
    /// </summary>
    /// <typeparam name="TBrushType">Custom brush datatype</typeparam>
    /// <typeparam name="TEvaluator"><see cref="IBrushSdfEvaluator{TBrushType}"/> that evaluates the SDF of a given <see cref="TBrushType"/></typeparam>
    public class CustomBrush<TBrushType, TEvaluator> : IDisposable
        where TBrushType : struct
        where TEvaluator : struct, IBrushSdfEvaluator<TBrushType>
    {
        /// <summary>
        /// The brush primitives that this custom brush is made of
        /// </summary>
        public NativeList<CustomBrushPrimitive<TBrushType>> Primitives {
            private set;
            get;
        }

        /// <summary>
        /// The evaluator that evaluates the SDF of a given <see cref="TBrushType"/>
        /// </summary>
        public TEvaluator Evaluator
        {
            private set;
            get;
        }

        public CustomBrush(TEvaluator evaluator)
        {
            Primitives = new NativeList<CustomBrushPrimitive<TBrushType>>(Allocator.Persistent);
            Evaluator = evaluator;
        }

        /// <summary>
        /// Adds a brush primitive
        /// </summary>
        /// <param name="type">Type of the brush</param>
        /// <param name="operation">CSG operation of the brush</param>
        /// <param name="blend">Smooth blend distance in voxel units</param>
        /// <param name="transform">Transform to be applied to the brush primitive</param>
        public void AddPrimitive(TBrushType type, BrushOperation operation, float blend, float4x4 transform)
        {
            Primitives.Add(new CustomBrushPrimitive<TBrushType>(type, operation, blend, transform));
        }

        public void Dispose()
        {
            Primitives.Dispose();
        }

        /// <summary>
        /// Creates an <see cref="ISdf"/> that returns the values of the compound SDF of this custom brush
        /// </summary>
        /// <returns></returns>
        public CustomBrushSdf<TBrushType, TEvaluator> CreateSdf()
        {
            return new CustomBrushSdf<TBrushType, TEvaluator>(Primitives, Evaluator);
        }

        /// <summary>
        /// Returns the SDF type.
        /// Used in the brush renderer.
        /// </summary>
        /// <returns></returns>
        public Type GetSdfType()
        {
            return typeof(CustomBrushSdf<TBrushType, TEvaluator>);
        }
    }
}
