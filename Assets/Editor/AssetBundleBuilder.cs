using UnityEngine;
using UnityEditor;
using System.Collections;
using System.IO;

public class AssetBundleBuilder
{

    [MenuItem("AssetBundle/에셋 번들 빌드")]
    static void BuildAssetBundles()
    {
        string path = "Assets/AssetBundles/";

        Debug.Log("에디터용 빌드 중");
        BuildPlatform(path, BuildTarget.StandaloneWindows64);
    }

    static void BuildPlatform(string path, BuildTarget target)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }

        BuildPipeline.BuildAssetBundles(path, BuildAssetBundleOptions.None, target);
    }
}
