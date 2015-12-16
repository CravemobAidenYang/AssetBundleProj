using UnityEngine;
using System.Collections;

public class CacheDeleter : MonoBehaviour 
{
    public bool deleteCacheWhenStartUp = false;
	// Use this for initialization
	void Awake ()
    {
        if (deleteCacheWhenStartUp)
        {
            Caching.CleanCache();
        }
	}
	
	// Update is called once per frame
	void Update ()
    {
	
	}
}
