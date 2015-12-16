using UnityEngine;
using System.Collections;

public class CubeTest : MonoBehaviour 
{
	// Use this for initialization
	void Start () 
    {
        AssetBundleManager.instance.LoadAssetAsync<GameObject>("cube.unity3d", "cube", (AssetBundle bundle, GameObject obj) =>
        {
            Instantiate(obj);
        });

        
	}
	
	// Update is called once per frame
	void Update ()
    {
	
	}
}
