using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Collections;
using System.Collections.Generic;
using System;

//만들고자 하는것
//에셋번들을 다운로드하고 안에서 에셋을 비동기로 로드한뒤 로드가 완료되었을때의 처리를
//콜백함수로 처리하는 싱글톤 클래스
 
//TODO: 일괄 패치나 패치 진행도라던지 이런거
//플랫폼에 따른 처리
//여러가지 에셋번들을 만들어 테스트해보기 (패치 진행UI 만들어서)
//WWW의 캐시기능을 이용하면 데이터가 계속 쌓이므로 다른 방법 생각해보기

public class AssetBundleManager : MonoBehaviour
{
    public bool _simulateMode = false;
  
    private const string _defaultAssetBundlePath = "file://C:/Users/AidenYang/Documents/AssetBundleProj/Assets/AssetBundles/";

#if UNITY_EDITOR || UNITY_STANDALONE
    private const string _platform = "editor";
#elif UNITY_ANDROID
    private const string _platform = "android";
#endif

    Dictionary<string, AssetBundle> _loadedBundleDict = new Dictionary<string, AssetBundle>();

    private AssetBundleManifest _assetBundleManifest;

    static private AssetBundleManager _instance;
    public static AssetBundleManager instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new GameObject("AssetBundleManager").AddComponent<AssetBundleManager>();
                DontDestroyOnLoad(_instance.gameObject);

                _instance.Init();
            }
            return _instance;
        }
    }

    private void Start()
    {
        if(_instance != this)
        {
            Destroy(gameObject);
        }
    }

    public delegate void OnPatchDoneDelegate();

    public bool isPatching
    {
        get;
        private set;
    }

    private float _totalPatchProgress;
    public float totalPatchProgress
    {
        get
        {
            if(!isPatching)
            {
                return -1f;
            }

            return _totalPatchProgress;
        }
    }

    private float _currentPatchProgress;
    public float currentPatchProgress
    {
        get
        {
            if (!isPatching)
            {
                return -1f;
            }

            return _currentPatchProgress;
        }
    }

    private event OnPatchDoneDelegate _onPatchDone = null;
    
    public void AddPatchDoneListener(OnPatchDoneDelegate del)
    {
        _onPatchDone += del;
    }

    public void DeletePatchDoneListener(OnPatchDoneDelegate del)
    {
        _onPatchDone -= del;
    }

    private string GetFullPathFromBundleName(string bundleName)
    {
        return _defaultAssetBundlePath + _platform + "/" + bundleName;
    }

    private void Init()
    {
#if UNITY_EDITOR
        Debug.Log("에셋번들 시뮬레이트 모드 " + (_simulateMode ? "On" : "Off"));
#endif

        _assetBundleManifest = null;
        isPatching = false;
        StartCoroutine(LoadManifest());
    }

    private IEnumerator LoadManifest()
    {
        string fullPath = GetFullPathFromBundleName(_platform);

        //단일 매니페스트로 다른 번들의 갱신유무를 파악해야하므로 캐싱하지 않는다
        using (WWW www = new WWW(fullPath))
        {
            yield return www;

            if (www.error != null)
            {
                throw new Exception("WWW download had an error:" + www.error);
            }

            var bundle = www.assetBundle;
            _assetBundleManifest = bundle.LoadAsset<AssetBundleManifest>("AssetBundleManifest");

            bundle.Unload(false);
        }
    }  

    IEnumerator WaitForLoadManifest()
    {
        while(_assetBundleManifest == null)
        {
            yield return null;
        }
    }

    //모든 에셋번들에 대해 변경사항이 있을시 다운로드 받는다.
    public void PatchAllAssetBundlesAsync()
    {
        StartCoroutine(PatchAllAssetBundlesAsyncInternal());
    }

    private IEnumerator PatchAllAssetBundlesAsyncInternal()
    {
        isPatching = true;

        yield return StartCoroutine(WaitForLoadManifest());

        string[] bundleNames = _assetBundleManifest.GetAllAssetBundles();
        string[] bundlePaths = new string[bundleNames.Length];
        Hash128[] bundleHashs = new Hash128[bundlePaths.Length];

        for (int i = 0; i < bundleNames.Length; ++i)
        {
            bundlePaths[i] = GetFullPathFromBundleName(bundleNames[i]);
        }    

        List<int> patchIdxList = new List<int>();
        for (int i = 0; i < bundlePaths.Length; ++i)
        {
            bundleHashs[i] = _assetBundleManifest.GetAssetBundleHash(bundleNames[i]);

            if(!Caching.IsVersionCached(bundlePaths[i], bundleHashs[i]))
            {
                patchIdxList.Add(i);
            }
        }

        for (int i = 0; i < patchIdxList.Count; ++i)
        {
            int index = patchIdxList[i];

            yield return StartCoroutine(DownloadAssetBundle(bundleNames[index], bundleHashs[index]));

            _totalPatchProgress = ((float)i) / ((float)patchIdxList.Count);
        }

        Debug.Log("패치 완료. 패치한 갯수" + patchIdxList.Count);

        _totalPatchProgress = 1f;
       
        _onPatchDone();
        _onPatchDone = null;

        isPatching = false;
    }

    //해시값에 해당하는 번들이 캐시되어 있으면 건너뛰고 아니면 다운로드하여 캐시해둔다.
    private IEnumerator DownloadAssetBundle(string bundleName, Hash128 bundleHash)
    {
        _currentPatchProgress = 0f;

        yield return StartCoroutine(WaitForLoadManifest());

        string fullPath = GetFullPathFromBundleName(bundleName);

        using (WWW www = WWW.LoadFromCacheOrDownload(fullPath, bundleHash))
        {
            while(!www.isDone)
            {
                Debug.Log(www.progress);
                _currentPatchProgress = www.progress;
                yield return null;
            }

            if (www.error != null)
            {
                throw new Exception("WWW download had an error:" + www.error);
            }
        }
    }

    public void LoadAssetAsync<T>(string bundleName, string assetName, Action<AssetBundle, T> callback) where T : UnityEngine.Object
    {
        StartCoroutine(LoadAssetAsyncInternal(bundleName, assetName, false, callback));
    }

    public void LoadSceneAsync(string bundleName, string sceneName)
    {
        StartCoroutine(LoadAssetAsyncInternal<UnityEngine.Object>(bundleName, sceneName, true, null));
    }

    //개선필요: 같은 번들에대해 동시에 www로 다운로드 하면 어떤일이 생길까
    //만약 로컬에서 번들을 불러오고 그안에서 에셋을 꺼낸뒤 사용하고 다시 번들을 언로드하는 과정이
    //매번 반복하기에 비용이 많이 나가는 작업이라면 번들을 한번 로드한뒤 캐시해둘 필요가 있겠다.
    //콜백함수에 번들을 전달할 필요가 있을까
    private IEnumerator LoadAssetAsyncInternal<T>(string bundleName, string assetName, bool isSceneAsset, Action<AssetBundle, T> callback) where T : UnityEngine.Object
    {
#if UNITY_EDITOR
        if(_simulateMode)
        {
            LoadAssetInSimulateMode(bundleName, assetName, isSceneAsset, callback);
            yield break;
        }
#endif

        yield return StartCoroutine(WaitForLoadManifest());

        yield return StartCoroutine(LoadAssetBundleAsync(bundleName));

        AssetBundle bundle;
        if (_loadedBundleDict.TryGetValue(bundleName, out bundle))
        {
            var request = bundle.LoadAssetAsync<T>(assetName);
            yield return request;

            if (isSceneAsset)
            {
                string[] sceneNameWithoutExpend = assetName.Split('.');
                yield return Application.LoadLevelAsync(sceneNameWithoutExpend[0]);
            }
            else
            {
                callback(bundle, request.asset as T);
            }
        }
        UnloadAssetBundle(bundleName);
    }

#if UNITY_EDITOR
    private void LoadAssetInSimulateMode<T>(string bundleName, string assetName, bool isSceneAsset, Action<AssetBundle, T> callback) where T : UnityEngine.Object
    {
        string assetNameWithoutExpend = assetName.Split('.')[0];
        var paths = AssetDatabase.GetAssetPathsFromAssetBundleAndAssetName(bundleName, assetNameWithoutExpend);

        foreach (var path in paths)
        {
            if (path.ToLower().Contains(assetName.ToLower()))
            {
                if (isSceneAsset)
                {
                    EditorApplication.LoadLevelInPlayMode(path);
                }
                else
                {
                    T obj = AssetDatabase.LoadAssetAtPath<T>(path);
                    callback(null, obj);
                }

                return;
            }
        }

        Debug.LogError(string.Format("에셋번들 시뮬레이션 모드에서 에셋을 찾지 못함. bundleName:{0}, assetName:{1}", bundleName, assetName));
    }
#endif

    private IEnumerator LoadAssetBundleAsync(string bundleName)
    {
        if (_loadedBundleDict.ContainsKey(bundleName))
        {
            while (_loadedBundleDict[bundleName] == null)
            {
                //딕셔너리에 있는데 널값이라는 얘기는
                //다른 어딘가에서 다운로드가 진행중이라는 것이므로
                //그 다운로드가 끝날때까지 대기한다.
                yield return null;
            }
        }
        else
        {
            _loadedBundleDict.Add(bundleName, null);

            //의존 관계에 있는 에셋번들을 우선 로드한다.
            string[] dependencies = _assetBundleManifest.GetAllDependencies(bundleName);
            for (int i = 0; i < dependencies.Length; ++i)
            {
                yield return StartCoroutine(LoadAssetBundleAsync(dependencies[i]));
            }

            string fullPath = GetFullPathFromBundleName(bundleName);

            Hash128 hash = _assetBundleManifest.GetAssetBundleHash(bundleName);
            Debug.Log(bundleName + " 번들 해시:" + hash.ToString());

            if (!Caching.IsVersionCached(fullPath, hash))
            {
                Debug.LogWarning(bundleName + "이 미리 다운로드 되지 않았음! hash: " + hash.ToString());
            }

            using (WWW www = WWW.LoadFromCacheOrDownload(fullPath, hash))
            {
                yield return www;

                if (www.error != null)
                {
                    throw new Exception("WWW download had an error:" + www.error);
                }

                _loadedBundleDict[bundleName] = www.assetBundle;
            }
        }
    }

    private void UnloadAssetBundle(string bundleName)
    {
        AssetBundle loadedBundle;
        if(_loadedBundleDict.TryGetValue(bundleName, out loadedBundle))
        {
            //추후 사운드에셋 같은것은 트루로 해줘야 할수도 있다.
            loadedBundle.Unload(false);
            _loadedBundleDict.Remove(bundleName);
        }

        //의존관계에 있는 에셋번들을 언로드한다.
        string[] dependencies = _assetBundleManifest.GetAllDependencies(bundleName);
        for (int i = 0; i < dependencies.Length; ++i)
        {
            UnloadAssetBundle(dependencies[i]);
        }
    }
}
