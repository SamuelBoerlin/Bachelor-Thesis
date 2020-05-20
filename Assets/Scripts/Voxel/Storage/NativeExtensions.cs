using UnityEngine;
using System.Collections;
using Unity.Collections;
using System;
using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;

public static class NativeExtensions
{
    //Source: https://github.com/Eldemarkki/Marching-Cubes-Terrain/blob/e8eee9f150b84bea23a082d91571051a1b9f9c6d/Assets/Scripts/Utils.cs#L58.
    //Modified to accept a list instead of slice.
    //
    //MIT License
    //
    //Copyright(c) 2019 Eldemarkki
    //
    //Permission is hereby granted, free of charge, to any person obtaining a copy
    //of this software and associated documentation files(the "Software"), to deal
    //in the Software without restriction, including without limitation the rights
    //to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    //copies of the Software, and to permit persons to whom the Software is
    //furnished to do so, subject to the following conditions:
    //
    //The above copyright notice and this permission notice shall be included in all
    //copies or substantial portions of the Software.
    //
    //THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    //IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    //FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
    //AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    //LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    //OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
    //SOFTWARE.
    public static unsafe void CopyToFast<T, A>(this NativeList<T> source, A[] target)
        where T : struct
        where A : struct
    {
        if (target.Length == 0)
        {
            return;
        }

        if (target == null)
        {
            throw new NullReferenceException(nameof(target) + " is null");
        }

        if (Marshal.SizeOf(default(T)) != Marshal.SizeOf(default(A)))
        {
            throw new ArgumentException("Types " + nameof(T) + " and " + nameof(A) + " do not have the same size");
        }

        int nativeArrayLength = source.Length;
        if (target.Length < nativeArrayLength)
        {
            throw new IndexOutOfRangeException(nameof(target) + " is shorter than " + nameof(source));
        }

        int byteLength = source.Length * Marshal.SizeOf(default(T));
        void* managedBuffer = UnsafeUtility.AddressOf(ref target[0]);
        void* nativeBuffer = source.GetUnsafePtr();
        Buffer.MemoryCopy(nativeBuffer, managedBuffer, byteLength, byteLength);
    }
}
