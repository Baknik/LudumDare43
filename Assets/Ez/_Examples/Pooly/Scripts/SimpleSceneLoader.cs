// Copyright (c) 2016 - 2017 Ez Entertainment SRL. All Rights Reserved.
// This code can only be used under the standard Unity Asset Store End User License Agreement
// A Copy of the EULA APPENDIX 1 is available at http://unity3d.com/company/legal/as_terms

using UnityEngine;
using UnityEngine.SceneManagement;

namespace Ez.Pooly.Examples
{
    public class SimpleSceneLoader : MonoBehaviour
    {

        public void LoadScene(int sceneNumber)
        {
            SceneManager.LoadScene("PoolExtensionTest" + sceneNumber, LoadSceneMode.Additive);
        }

        public void UnloadScene(int sceneNumber)
        {
            SceneManager.UnloadSceneAsync("PoolExtensionTest" + sceneNumber);
        }
    }
}
