using ObjLoader.Loader.Loaders;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public class DummyObjMaterialLoader : IMaterialStreamProvider
{
    public Stream Open(string materialFilePath)
    {
        return null;
    }
}
