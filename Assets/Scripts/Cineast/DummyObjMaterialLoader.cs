using ObjLoader.Loader.Loaders;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class DummyObjMaterialLoader : IMaterialStreamProvider
{
    public Stream Open(string materialFilePath)
    {
        return null;
    }
}
