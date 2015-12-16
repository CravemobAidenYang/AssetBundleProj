using UnityEngine;
using System.Collections;

public class Patcher : MonoBehaviour 
{
    public float curPatchProgress;
    public float totalPatchProgress;

	// Use this for initialization
	void Start () 
    {
        AssetBundleManager.instance.AddPatchDoneListener(() => 
        {
            Debug.Log("패치 끝");
            AssetBundleManager.instance.LoadSceneAsync("test.unity3d", "test");
        });
        AssetBundleManager.instance.PatchAllAssetBundles();

        StartCoroutine(CheckProgress());
	}
	
	// Update is called once per frame
	void Update () 
    {

	}

    IEnumerator CheckProgress()
    {
        while (true)
        {
            if (AssetBundleManager.instance.isPatching)
            {
                curPatchProgress = AssetBundleManager.instance.currentPatchProgress;
                totalPatchProgress = AssetBundleManager.instance.totalPatchProgress;
            }
            yield return null;
        }
    }
}
