// Copyright (c) 2016 - 2018 Ez Entertainment SRL. All Rights Reserved.
// This code can only be used under the standard Unity Asset Store End User License Agreement
// A Copy of the EULA APPENDIX 1 is available at http://unity3d.com/company/legal/as_terms

using QuickEditor;
using UnityEditor;

#pragma warning disable 0162
namespace Ez.Internal
{
    [InitializeOnLoad]
    public class DataManagerSymbolLoader
    {
        static DataManagerSymbolLoader()
        {
            EditorApplication.update += RunOnce;
        }

        static void RunOnce()
        {
            EditorApplication.update -= RunOnce;
            CreateMissingFolders();
            LoadSymbol();
        }

        static void CreateMissingFolders()
        {
            if(!AssetDatabase.IsValidFolder(EZT.PATH + "/DataManager/Editor")) { AssetDatabase.CreateFolder(EZT.PATH + "/DataManager", "Editor"); }
            if(!AssetDatabase.IsValidFolder(EZT.PATH + "/DataManager/Editor/Resources")) { AssetDatabase.CreateFolder(EZT.PATH + "/DataManager/Editor", "Resources"); }
            if(!AssetDatabase.IsValidFolder(EZT.PATH + "/DataManager/Editor/Resources/EZT")) { AssetDatabase.CreateFolder(EZT.PATH + "/DataManager/Editor/Resources", "EZT"); }
            if(!AssetDatabase.IsValidFolder(EZT.PATH + "/DataManager/Editor/Resources/EZT/DataManager")) { AssetDatabase.CreateFolder(EZT.PATH + "/DataManager/Editor/Resources/EZT", "DataManager"); }
            if(!AssetDatabase.IsValidFolder(EZT.PATH + "/DataManager/Editor/Resources/EZT/DataManager/Settings")) { AssetDatabase.CreateFolder(EZT.PATH + "/DataManager/Editor/Resources/EZT/DataManager", "Settings"); }
            if(!AssetDatabase.IsValidFolder(EZT.PATH + "/DataManager/Editor/Resources/EZT/DataManager/Version")) { AssetDatabase.CreateFolder(EZT.PATH + "/DataManager/Editor/Resources/EZT/DataManager", "Version"); }

            if(!AssetDatabase.IsValidFolder(EZT.PATH + "/DataManager/Resources")) { AssetDatabase.CreateFolder(EZT.PATH + "/DataManager", "Resources"); }
            if(!AssetDatabase.IsValidFolder(EZT.PATH + "/DataManager/Resources/EZT")) { AssetDatabase.CreateFolder(EZT.PATH + "/DataManager/Resources", "EZT"); }
            if(!AssetDatabase.IsValidFolder(EZT.PATH + "/DataManager/Resources/EZT/DataManager")) { AssetDatabase.CreateFolder(EZT.PATH + "/DataManager/Resources/EZT", "DataManager"); }
            if(!AssetDatabase.IsValidFolder(EZT.PATH + "/DataManager/Resources/EZT/DataManager/Keys")) { AssetDatabase.CreateFolder(EZT.PATH + "/DataManager/Resources/EZT/DataManager", "Keys"); }
        }

        static void LoadSymbol()
        {
#if EZ_SOURCE
            return;
#endif
            QUtils.AddScriptingDefineSymbol(EZT.SYMBOL_EZ_DATA_MANAGER);
        }
    }
}
#pragma warning restore 0162
