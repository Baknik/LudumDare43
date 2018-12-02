// Copyright (c) 2016 - 2018 Ez Entertainment SRL. All Rights Reserved.
// This code can only be used under the standard Unity Asset Store End User License Agreement
// A Copy of the EULA APPENDIX 1 is available at http://unity3d.com/company/legal/as_terms

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ez.DataManager
{
    public partial class EzDataManager : MonoBehaviour
    {
        #region Singleton
        protected EzDataManager() { }

        private static EzDataManager _instance;
        public static EzDataManager Instance
        {
            get
            {
                if(_instance == null)
                {
                    if(applicationIsQuitting) { return null; }
                    GameObject singleton = new GameObject(("(singleton) " + typeof(EzDataManager).ToString()));
                    _instance = singleton.AddComponent<EzDataManager>();
                    DontDestroyOnLoad(singleton);
                }
                return _instance;
            }
        }

        private static bool applicationIsQuitting = false;
        private void OnApplicationQuit()
        {
            applicationIsQuitting = true;
            if(autoDataSave) { AutomaticSaveAll(); }
        }
        #endregion

        public bool autoDataSave = true;
        public bool autoDataLoad = true;

        private void Awake()
        {
            if(_instance != null)
            {
                Debug.Log("[EZ][DataManager] There cannot be two EzDataManagers active at the same time. Destryoing this one!");
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);

            if(autoDataLoad) { AutomaticLoadAll(); }
        }

        partial void AutomaticLoadAll();
        partial void AutomaticSaveAll();

        private void OnApplicationPause(bool pause)
        {
            if(pause && autoDataSave) { AutomaticSaveAll(); }
        }

        private void OnApplicationFocus(bool focus)
        {
            if(!focus && autoDataSave) { AutomaticSaveAll(); }
        }
    }
}
