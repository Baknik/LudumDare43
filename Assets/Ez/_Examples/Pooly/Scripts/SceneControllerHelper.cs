// Copyright (c) 2016 - 2017 Ez Entertainment SRL. All Rights Reserved.
// This code can only be used under the standard Unity Asset Store End User License Agreement
// A Copy of the EULA APPENDIX 1 is available at http://unity3d.com/company/legal/as_terms

using UnityEngine;

namespace Ez.Pooly.Examples
{
    public class SceneControllerHelper : MonoBehaviour
    {
        public PoolySpawner spawner;
        private SceneController sc;

        private void Awake()
        {
            sc = FindObjectOfType<SceneController>();
        }

        private void OnEnable()
        {
            sc.Zone3Spawner = spawner;
        }

        private void OnDisable()
        {
            sc.Zone3Spawner = null;
        }

    }
}
