using UnityEngine;
using UnityEditor;
using System.Collections;
using System.IO;

public class AssetBundleBuilder
{
    [MenuItem("Assets/Build Asset Bundles")]
    static void BuildAssetBundles()
    {
        if (!Directory.Exists("Assets/AssetBundles"))
        {
            Directory.CreateDirectory("Assets/AssetBundles");
        }

        BuildPipeline.BuildAssetBundles("Assets/AssetBundles");
    }

}
