using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;


namespace CrossFadeDemo
{
    /// <summary>
    /// Unity Behavior class to handle Scene Crossfades. Utilizes a canvas in UI overlay mode and a canvas group to fade in/out
    /// a panel.
    /// </summary>
    public class CrossFadeSceneLoader : MonoBehaviour
    {
        [Tooltip("Length of time the fade in should last.")]
        public float FadeInTime = 0.25f;

        [Tooltip("Length of time the fade out should last.")]
        public float FadeOutTime = 0.25f;

        [Tooltip("Length of time to wait before loading the next scene.")]
        public float SceneLoadDelay = 0.5f;

        [Tooltip("Scene name to load")]
        public string SceneNameToLoad;




        AsyncOperation _asyncop = null; // Reference to the async load operation
        public float LoadProgress
        {
            get
            {
                if (_asyncop != null)
                    return _asyncop.progress;
                else
                    return -1;
            }
        }

        // Sentinel value to indicate loading is in progress
        bool _isLoading = false;
        public bool IsLoading { get {return _isLoading;} }

        Canvas _overlayCanvas;


        private void Awake()
        {
            _overlayCanvas = GetComponentInChildren<Canvas>();
            if(_overlayCanvas != null)
            {
                CanvasGroup canvasGroup = _overlayCanvas.GetComponentInChildren<CanvasGroup>();
                if(canvasGroup != null)
                {
                    canvasGroup.alpha = 0.0f;
                }
                else
                {
                    Debug.LogError("Overlay CanvasGroup missing from CrossFade Canvas Game Object!");
                }
                _overlayCanvas.gameObject.SetActive(false);
            }
            else
            {
                Debug.LogError("Overlay Canvas missing from CrossFade Game Object!");
            }
        }

        // Start is called before the first frame update
        void Start()
        {
            DontDestroyOnLoad(gameObject);
        }

        // Update is called once per frame
        void Update()   { }

        public void LoadScene()
        {
            string sceneToLoad = SceneNameToLoad;
            // Already loading!
            if (_asyncop != null || _isLoading)
            {
                Debug.LogWarning("Crossfade Scene loading already started.");
                return;
            }

            StartCoroutine(FadeOverlay(FadeInTime, 0, 1, ()=> 
                { 
                    StartCoroutine(DoSceneLoad(sceneToLoad)); 
                }));

        }


        IEnumerator FadeOverlay(float fadeTime, float lerpFrom, float lerpTo, Action fadeCompleteCallAction)
        {
            if (fadeTime > 0.0 && _overlayCanvas != null)
            {
                _overlayCanvas.gameObject.SetActive(true);
                CanvasGroup canvasGroup = _overlayCanvas.GetComponentInChildren<CanvasGroup>();
                if(canvasGroup != null)
                {
                    canvasGroup.alpha = lerpFrom;
                    float fadeRate = 1.0f / fadeTime;
                    float currentFadeVal = 0;
                    while(currentFadeVal <= 1.0f)
                    {
                        canvasGroup.alpha = Mathf.Lerp(lerpFrom, lerpTo, currentFadeVal);
                        currentFadeVal += Time.deltaTime * fadeRate;
                        yield return null;
                    }

                    canvasGroup.alpha = lerpTo;
                }
            }

            fadeCompleteCallAction?.Invoke();

        }

        IEnumerator DoSceneLoad(string sceneNameToLoad)
        {
            if( string.IsNullOrEmpty(sceneNameToLoad))
            {
                Debug.LogError("Empty Scene Name provided to DoSceneLoad.");
            }

            _asyncop = SceneManager.LoadSceneAsync(sceneNameToLoad);
            if (_asyncop != null)
            {
                _asyncop.allowSceneActivation = false;

                // Watch progress until 90%, then continue
                while (_asyncop.progress < 0.9f)
                {
                    yield return null;
                }

                float delayTime = 0;
                // Delay Scene Activation
                while (delayTime < SceneLoadDelay)
                {
                    delayTime += Time.deltaTime;
                    yield return null;
                }

                // Finally: Activate the new scene 
                _asyncop.allowSceneActivation = true;
                _asyncop = null;
            }

            StartCoroutine(FadeOverlay(FadeOutTime, 1, 0, SceneLoadComplete));
        }

        void SceneLoadComplete()
        {
            _isLoading = false;
            if(_overlayCanvas)
            {
                _overlayCanvas.gameObject.SetActive(false);
            }
            Destroy(gameObject);
        }


    }
}