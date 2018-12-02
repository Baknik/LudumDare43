// Copyright (c) 2016 - 2018 Ez Entertainment SRL. All Rights Reserved.
// This code can only be used under the standard Unity Asset Store End User License Agreement
// A Copy of the EULA APPENDIX 1 is available at http://unity3d.com/company/legal/as_terms

using Ez.DataManager;
using Ez.DataManager.Internal;
using QuickEditor;
using QuickEngine.Core;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.AnimatedValues;
using UnityEngine;

namespace Ez
{
    public partial class ControlPanelWindow : QWindow
    {
#if EZ_DATA_MANAGER

        EzDataManagerSettings _ezDataManagerSettings;
        EzDataManagerSettings EzDataManagerSettings
        {
            get
            {
                if(_ezDataManagerSettings == null)
                {
                    _ezDataManagerSettings = Q.GetResource<EzDataManagerSettings>(EZT.RESOURCES_PATH_DATA_MANAGER_SETTINGS, "EzDataManagerSettings");
                    if(_ezDataManagerSettings == null)
                    {
                        _ezDataManagerSettings = Q.CreateAsset<EzDataManagerSettings>(EZT.RELATIVE_PATH_DATA_MANAGER_SETTINGS, "EzDataManagerSettings");
                    }
                }
                return _ezDataManagerSettings;
            }
        }

        EzEncryptionKeys _ezEncryptionKeys;
        EzEncryptionKeys EzEncryptionKeys
        {
            get
            {
                if(_ezEncryptionKeys == null)
                {
                    _ezEncryptionKeys = Q.GetResource<EzEncryptionKeys>(EZT.RESOURCES_PATH_DATA_MANAGER_KEYS, "EzEncryptionKeys");
                    if(_ezEncryptionKeys == null)
                    {
                        _ezEncryptionKeys = Q.CreateAsset<EzEncryptionKeys>(EZT.RELATIVE_PATH_DATA_MANAGER_KEYS, "EzEncryptionKeys");
                    }
                }
                return _ezEncryptionKeys;
            }
        }

        EzDataManager _ezDataManager;
        EzDataManager EzDataManager
        {
            get
            {
                if(_ezDataManager == null)
                {
                    _ezDataManager = FindObjectOfType<EzDataManager>();
                }
                return _ezDataManager;
            }
        }

        private int DATA_MANAGER_TYPE_FIELD_WIDTH { get { return 50; } }
        private int DATA_MANAGER_DATA_FIELD_WIDTH { get { return (int) (WindowSettings.CurrentPageContentWidth - 6 - 50 - 16) / 2; } }

        string[] UnitySpecificPlayerPrefs = new string[]
        {
            "UnityGraphicsQuality",
            "unity."
        };

        private enum Tabs
        {
            PlayerPrefs,
            Encryption
        }
        private Tabs CurrentTab = Tabs.PlayerPrefs;

        /// <summary>
        /// PlayerPrefs key-value record
        /// </summary>
        struct PlayerPrefPair
        {
            public string Key { get; set; }
            public object Value { get; set; }
        }

        readonly DateTime MISSING_DATETIME = new DateTime(1601, 1, 1);
        /// <summary>
        /// Native PlayerPrefs types
        /// </summary>
        enum PlayerPrefType { Float = 0, Int = 1, String = 2 };
        /// <summary>
        /// Stored cache of player prefs fetched from registry (Windows) or plist (Mac)
        /// </summary>
        List<PlayerPrefPair> deserializedPlayerPrefs = new List<PlayerPrefPair>();
        /// <summary>
        /// Filtered list of player prefs records. Used when a regex filter is in effect.
        /// </summary>
        List<PlayerPrefPair> filteredPlayerPrefs = new List<PlayerPrefPair>();

        /// <summary>
        /// Keeps track of the last successful deserialization in order to prevent this happening too often.
        /// On OSX this uses the player prefs file last modified time, on Windows we just poll repeatedly and use this to prevent polling again too soon.
        /// </summary>
        DateTime? lastDeserialization = null;

        /// <summary>
        /// Key value used to delete a player prefs record. Needed because there are issues when deleting inside OnGUI, thus it needs to be deferred to OnInspectorUpdate() instead. 
        /// </summary>
        string keyQueuedForDeletion = null;

#pragma warning disable 0414
        /// <summary>
        /// Company name used to import player prefs from other projects
        /// </summary>
        string importCompanyName = "";
        /// <summary>
        /// Product name used to import player prefs from other projects.
        /// </summary>
        string importProductName = "";
#pragma warning restore 0414

        PlayerPrefType newEntryType = PlayerPrefType.String;
        string newEntryKey = "";
        float newEntryValueFloat = 0;
        int newEntryValueInt = 0;
        string newEntryValueString = "";

        InfoMessage aboutEncryptionInfoMessage, addEncryptionKeyInfoMessage;

        void GenerateInfoMessages()
        {
            aboutEncryptionInfoMessage = new InfoMessage()
            {
                title = "About Encryption",
                message = "To allow the secure saving and loading of sensitive data, EzDataManager makes uses of the AES128 encryption algorithm with a pre-generated key and IV pair." +
                          "\n\n" +
                          "If you are using encryption, make sure to carefully manage your encryption key(s), as it is impossible to retrieve data without them." +
                          "\n\n" +
                          "It is higly recommended to use EzDataManager's key exporter function to make a backup of your encryption key(s) and keep this backup in a secure location.",
                show = new AnimBool(true, Repaint),
                type = InfoMessageType.Help
            };

            addEncryptionKeyInfoMessage = new InfoMessage()
            {
                title = "To create a new Encryption Key press the [+] button",
                show = new AnimBool(false, Repaint),
                type = InfoMessageType.Info
            };
        }

        void InitDataManager()
        {
            PageLastScrollPosition = Vector2.zero;

            importCompanyName = PlayerSettings.companyName;
            importProductName = PlayerSettings.productName;

            GenerateInfoMessages();
        }

        PlayerPrefPair[] GetSavedPrefs(string companyName, string productName)
        {
            if(Application.platform == RuntimePlatform.WindowsEditor)
            {
                // https://docs.unity3d.com/ScriptReference/PlayerPrefs.html
                // On Windows, PlayerPrefs are stored in the registry under HKCU\Software\[company name]\[product name] key, where company and product names are the names set up in Project Settings.

#if UNITY_5_5_OR_NEWER
                // From Unity 5.5 editor player prefs moved to a specific location
                Microsoft.Win32.RegistryKey registryKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("Software\\Unity\\UnityEditor\\" + companyName + "\\" + productName);
#else
                Microsoft.Win32.RegistryKey registryKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("Software\\" + companyName + "\\" + productName);
#endif
                if(registryKey != null) //parse the registry if the specified registryKey exists
                {
                    string[] valueNames = registryKey.GetValueNames(); //gets an array of all the keys (registry value names) are stored
                    PlayerPrefPair[] tempPlayerPrefs = new PlayerPrefPair[valueNames.Length]; //temp aray of player prefs pairs
                    int i = 0;
                    foreach(string valueName in valueNames)
                    {
                        string key = valueName;
                        int index = key.LastIndexOf("_"); //remove the _h193410979 style suffix used on player pref keys in Windows registry
                        key = key.Remove(index, key.Length - index);
                        object ambiguousValue = registryKey.GetValue(valueName); //get the value from the registry

                        if(ambiguousValue.GetType() == typeof(int)) //unfortunately floats will come back as an int (at least on 64 bit) because the float is stored as 64 bit but marked as 32 bit - which confuses the GetValue() method greatly! 
                        {
                            if(PlayerPrefs.GetInt(key, -1) == -1 && PlayerPrefs.GetInt(key, 0) == 0) //if the player pref is not actually an int then it must be a float, this will evaluate to true (impossible for it to be 0 and -1 at the same time)
                            {
                                ambiguousValue = PlayerPrefs.GetFloat(key); //fetch the float value from PlayerPrefs in memory
                            }
                        }
                        else if(ambiguousValue.GetType() == typeof(byte[]))
                        {
                            ambiguousValue = System.Text.Encoding.Default.GetString((byte[]) ambiguousValue); //on Unity 5 a string may be stored as binary, so convert it back to a string
                        }
                        tempPlayerPrefs[i] = new PlayerPrefPair() { Key = key, Value = ambiguousValue }; //assign the key and value into the respective record in our output array
                        i++;
                    }
                    return tempPlayerPrefs; //return the results
                }
                else
                {
                    return new PlayerPrefPair[0]; //no existing player prefs saved (which is valid), so just return an empty array
                }
            }
            else if(Application.platform == RuntimePlatform.OSXEditor)
            {
                // https://docs.unity3d.com/ScriptReference/PlayerPrefs.html
                // On macOS PlayerPrefs are stored in ~/Library/Preferences folder, in a file named unity.[company name].[product name].plist, where company and product names are the names set up in Project Settings.
                // The same .plist file is used for both Projects run in the Editor and standalone players.

                string plistFilename = string.Format("unity.{0}.{1}.plist", companyName, productName); //construct the plist filename from the project's settings
                string playerPrefsPath = Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "Library/Preferences"), plistFilename); ////construct the fully qualified path
                                                                                                                                                                          // Parse the player prefs file if it exists
                if(File.Exists(playerPrefsPath)) //parse the player prefs file if it exists
                {
                    //UnityEditor.iOS.Xcode.PlistDocument Plist = new UnityEditor.iOS.Xcode.PlistDocument();

                    ////object plist = Plist.readPlist(playerPrefsPath); //parse the plist then cast it to a Dictionary
                    //Plist.ReadFromFile(playerPrefsPath);
                    //object plist = Plist; //parse the plist then cast it to a Dictionary
                    try
                    {
                        Dictionary<string, object> parsed = PlistCS.Plist.readPlist(playerPrefsPath) as Dictionary<string, object>;

                        PlayerPrefPair[] tempPlayerPrefs = new PlayerPrefPair[parsed.Count]; //convert the dictionary data into an array of PlayerPrefPairs
                        int i = 0;
                        foreach(KeyValuePair<string, object> pair in parsed)
                        {
                            if(pair.Value.GetType() == typeof(double))
                            {
                                tempPlayerPrefs[i] = new PlayerPrefPair() { Key = pair.Key, Value = (float) (double) pair.Value }; //some float values may come back as double, so convert them back to floats
                            }
                            else
                            {
                                tempPlayerPrefs[i] = new PlayerPrefPair() { Key = pair.Key, Value = pair.Value };
                            }
                            i++;
                        }
                        return tempPlayerPrefs; //return the results
                    }
                    catch(Exception) { return new PlayerPrefPair[0]; } // failsafe; this REALLY shouldn't happen...
                }
                else
                {
                    return new PlayerPrefPair[0]; //no existing player prefs saved (which is valid), so just return an empty array
                }

            }
            else
            {
                throw new NotSupportedException("[EZ][DataManager] PlayerPrefs editing not supported on this Unity Editor platform.");
            }
        }

        void DrawDataManager()
        {
            DrawPageHeader("DATA MANAGER", QColors.Green, "PlayerPrefs / Encryption Settings", QUI.IsProSkin ? QColors.UnityLight : QColors.UnityMild, EZResources.IconDataManager);
            QUI.Space(6);
            DrawDataManagerAddRemoveEzDataManagerButton(WindowSettings.CurrentPageContentWidth, 20);
            QUI.Space(SPACE_8);
            DrawDataManagerTabs(WindowSettings.CurrentPageContentWidth);
            QUI.Space(SPACE_4);
            switch(CurrentTab)
            {
                case Tabs.PlayerPrefs: DrawDataManagerPlayerPrefs(WindowSettings.CurrentPageContentWidth); break;
                case Tabs.Encryption: DrawDataManagerEncryption(WindowSettings.CurrentPageContentWidth); break;
            }
            QUI.Space(SPACE_4);
        }

        void DataManagerOnInspectorUpdate()
        {
            if(!string.IsNullOrEmpty(keyQueuedForDeletion)) //if a player pref has been specified for deletion
            {
                if(deserializedPlayerPrefs != null)//if the user just deleted a player pref, find the ID and defer it for deletion by OnInspectorUpdate()
                {
                    int entryCount = deserializedPlayerPrefs.Count;
                    for(int i = 0; i < entryCount; i++)
                    {
                        if(deserializedPlayerPrefs[i].Key == keyQueuedForDeletion)
                        {
                            deserializedPlayerPrefs.RemoveAt(i);
                            break;
                        }
                    }
                }

                keyQueuedForDeletion = null; //remove the queued key since we've just deleted it

                //UpdateSearch(); //update the search results and repaint the window
            }
        }
        void DrawDataManagerAddRemoveEzDataManagerButton(float width, float buttonHeight)
        {
            QUI.BeginHorizontal(width);
            {
                QUI.Space(SPACE_4);
                if(EzDataManager == null)
                {
                    if(QUI.SlicedButton("Add EzDataManager to Scene", QColors.Color.Gray, width + SPACE_8, buttonHeight))
                    {
                        Undo.RegisterCreatedObjectUndo(new GameObject("EzDataManager", typeof(EzDataManager)), "AddEzDataManagerToScene");
                        Selection.activeObject = EzDataManager.gameObject;
                    }
                }
                else
                {
                    if(QUI.SlicedButton("Remove EzDataManager from Scene", QColors.Color.Red, width + SPACE_8, buttonHeight))
                    {
                        if(QUI.DisplayDialog("Remove EzDataManager",
                                                       "Are you sure you want to remove (delete) the EzDataManager gameObject from the current scene?" +
                                                       "\n\n\n" +
                                                       "You will lose all the references and values you set in the inspector.",
                                                       "Ok",
                                                       "Cancel"))
                        {
                            Undo.DestroyObjectImmediate(EzDataManager.gameObject);
                        }
                    }
                }
                QUI.FlexibleSpace();
            }
            QUI.EndHorizontal();
        }
        void DrawDataManagerTabs(float width)
        {
            QUI.BeginHorizontal(width);
            {
                QUI.Space(SPACE_4);
                if(QUI.GhostButton(Tabs.PlayerPrefs.ToString(), QColors.Color.Green, width / 4, 24, CurrentTab == Tabs.PlayerPrefs)) { CurrentTab = Tabs.PlayerPrefs; }
                QUI.Space(SPACE_2);
                if(QUI.GhostButton(Tabs.Encryption.ToString(), QColors.Color.Green, width / 4, 24, CurrentTab == Tabs.Encryption)) { CurrentTab = Tabs.Encryption; }
                QUI.FlexibleSpace();
            }
            QUI.EndHorizontal();
        }

        void DrawDataManagerPlayerPrefs(float width)
        {
            DrawDataManagerPlayerPrefsOptions(width);
            QUI.Space(SPACE_16);
            DrawDataManagerPlayerPrefsList(width);
        }
        void DrawDataManagerPlayerPrefsOptions(float width)
        {
            QUI.Space(SPACE_8);
            QUI.BeginHorizontal(width);
            {
                QUI.Space(SPACE_8);
                QUI.BeginChangeCheck();
                {
                    QUI.SetGUIBackgroundColor(EditorGUIUtility.isProSkin ? QColors.Green.Color : QColors.GreenLight.Color);
                    EzDataManagerSettings.showUnityPlayerPrefs = QUI.Toggle(EzDataManagerSettings.showUnityPlayerPrefs);
                    QUI.ResetColors();
                }
                if(QUI.EndChangeCheck())
                {
                    QUI.SetDirty(EzDataManagerSettings);
                    AssetDatabase.SaveAssets();
                }
                QUI.Label("Show Unity specific PlayerPrefs", Style.Text.Normal);
                QUI.FlexibleSpace();
            }
            QUI.EndHorizontal();
            QUI.BeginHorizontal(width);
            {
                QUI.Space(SPACE_8);
                string delim = EzEncryptionKeys.stringDelimiter.ToString();
                QUI.BeginChangeCheck();
                {
                    QUI.SetGUIBackgroundColor(EditorGUIUtility.isProSkin ? QColors.Green.Color : QColors.GreenLight.Color);
                    delim = EditorGUILayout.DelayedTextField(delim, GUILayout.Width(36));
                    QUI.ResetColors();
                }
                if(QUI.EndChangeCheck())
                {
                    Undo.RecordObject(EzDataManagerSettings, "String Delimiter Change");
                    EzEncryptionKeys.stringDelimiter = string.IsNullOrEmpty(delim) ? '|' : delim[0];
                    QUI.SetDirty(EzDataManagerSettings);
                    AssetDatabase.SaveAssets();
                }
                QUI.Label("PlayerPrefs string delimiter (1 char)", Style.Text.Normal);
                QUI.FlexibleSpace();
            }
            QUI.EndHorizontal();

            QUI.Space(SPACE_8);

            GUIContent content = new GUIContent();
            QUI.BeginHorizontal(width);
            {
                QUI.Space(SPACE_8);
                content.text = "type";
                QUI.Label(content.text, Style.Text.Tiny, DATA_MANAGER_TYPE_FIELD_WIDTH);

                content.text = "key";
                QUI.Label(content.text, Style.Text.Tiny, DATA_MANAGER_DATA_FIELD_WIDTH);

                content.text = "value (" + newEntryType.ToString() + ")";
                QUI.Label(content.text, Style.Text.Tiny, DATA_MANAGER_DATA_FIELD_WIDTH);
            }
            QUI.EndHorizontal();
            QUI.Space(-SPACE_4);
            QUI.BeginHorizontal(width);
            {
                QUI.Space(SPACE_4 + SPACE_2);
                QUI.SetGUIBackgroundColor(EditorGUIUtility.isProSkin ? QColors.Green.Color : QColors.GreenLight.Color);
                newEntryType = (PlayerPrefType) EditorGUILayout.EnumPopup(newEntryType, GUILayout.Width(DATA_MANAGER_TYPE_FIELD_WIDTH));
                GUI.SetNextControlName("newEntryKey"); //track the next control so we can detect key events in it
                newEntryKey = EditorGUILayout.TextField(newEntryKey, GUILayout.Width(DATA_MANAGER_DATA_FIELD_WIDTH)); //UI for the new key text box
                GUI.SetNextControlName("newEntryValue"); //track the next control so we can detect key events in it
                switch(newEntryType) //display the correct UI field editor based on what type of player pref is being created
                {
                    case PlayerPrefType.Float: newEntryValueFloat = EditorGUILayout.FloatField(newEntryValueFloat, GUILayout.Width(DATA_MANAGER_DATA_FIELD_WIDTH)); break;
                    case PlayerPrefType.Int: newEntryValueInt = EditorGUILayout.IntField(newEntryValueInt, GUILayout.Width(DATA_MANAGER_DATA_FIELD_WIDTH)); break;
                    case PlayerPrefType.String: newEntryValueString = EditorGUILayout.TextField(newEntryValueString, GUILayout.Width(DATA_MANAGER_DATA_FIELD_WIDTH)); break;
                }

                bool keyboardEnterPressed = Event.current.isKey &&
                                          Event.current.keyCode == KeyCode.Return &&
                                          Event.current.type == EventType.KeyUp &&
                                          (GUI.GetNameOfFocusedControl() == "newEntryKey" || GUI.GetNameOfFocusedControl() == "newEntryValue"); //if the user hits Enter while either the key or value fields were being edited

                QUI.ResetColors();
                //if the user clicks the Add button or hits return (and there is a non-empty key), create the player pref
                if(QUI.ButtonPlus() || keyboardEnterPressed)
                {
                    if(string.IsNullOrEmpty(newEntryKey))
                    {
                        QUI.DisplayDialog("New PlayerPref",
                                                    "Please enter a key in order to create a new PlayerPref.",
                                                    "Ok");
                    }
                    else
                    {
                        switch(newEntryType)
                        {
                            case PlayerPrefType.Float:
                                PlayerPrefs.SetFloat(newEntryKey, newEntryValueFloat); //record the new player pref in PlayerPrefs
                                CacheRecord(newEntryKey, newEntryValueFloat); //cache the addition
                                break;
                            case PlayerPrefType.Int:
                                PlayerPrefs.SetInt(newEntryKey, newEntryValueInt); //record the new player pref in PlayerPrefs
                                CacheRecord(newEntryKey, newEntryValueInt); //cache the addition
                                break;
                            case PlayerPrefType.String:
                                PlayerPrefs.SetString(newEntryKey, newEntryValueString); //record the new player pref in PlayerPrefs
                                CacheRecord(newEntryKey, newEntryValueString); //cache the addition
                                break;
                        }

                        PlayerPrefs.Save(); //save PlayerPrefs

                        Repaint(); //force a repaint since hitting the return key won't invalidate layout on its own

                        newEntryKey = ""; //reset the value
                        newEntryValueFloat = 0; //reset the value
                        newEntryValueInt = 0; //reset the value
                        newEntryValueString = ""; //reset the value

                        QUI.ResetKeyboardFocus();
                    }
                }
                QUI.FlexibleSpace();
            }
            QUI.EndHorizontal();
            QUI.Space(SPACE_16);
            QUI.BeginHorizontal(width);
            {
                QUI.FlexibleSpace();
                if(QUI.GhostButton("Delete All PlayerPrefs", QColors.Color.Red, 120, 16))
                {
                    if(QUI.DisplayDialog("Delete All PlayerPrefs",
                                                   "Are you sure you want to delete all the PlayerPrefs?" +
                                                   "\n\n" +
                                                   "This operation cannot be undone!",
                                                   "Yes",
                                                   "No"))
                    {
                        ClearPlayerPrefs();
                    }
                }
                QUI.Space(SPACE_8);
            }
            QUI.EndHorizontal();
            QUI.Space(-SPACE_8);
        }
        void DrawDataManagerPlayerPrefsList(float width)
        {
            if(Event.current.type == EventType.KeyUp) { QUI.ExitGUI(); }

            RefreshPlayerPrefs();

            List<PlayerPrefPair> activePlayerPrefs = deserializedPlayerPrefs;
            if(!string.IsNullOrEmpty(SearchPattern))
            {
                activePlayerPrefs = filteredPlayerPrefs;
            }

            int entryCount = activePlayerPrefs.Count; //cache entry count

            PageLastScrollPosition = PageScrollPosition; //record the last scroll position so we can calculate if the user has scrolled this frame
            if(PageScrollPosition.y < 0) { PageScrollPosition.y = 0; } //ensure the scroll doesn't go below zero

            // The following code has been optimised so that rather than attempting to draw UI for every single PlayerPref
            // it instead only draws the UI for those currently visible in the scroll view and pads above and below those
            // results to maintain the right size using GUILayout.Space(). This enables us to work with thousands of 
            // PlayerPrefs without slowing the interface to a halt.

            float rowHeight = 24; //fixed height of one of the rows in the table
            //int visibleCount = Mathf.CeilToInt(Screen.height / rowHeight); //determine how many rows are visible on screen. For simplicity, use Screen.height (the overhead is negligible)
            int visibleCount = Screen.height; //determine how many rows are visible on screen. For simplicity, use Screen.height (the overhead is negligible)
            int firstShownIndex = Mathf.FloorToInt(PageScrollPosition.y / rowHeight); //determine the index of the first player pref that should be drawn as visible in the scrollable area
            int shownIndexLimit = firstShownIndex + visibleCount; //determine the bottom limit of the visible player prefs (last shown index + 1)
            if(entryCount < shownIndexLimit) { shownIndexLimit = entryCount; } //if the actual number of player prefs is smaller than the caculated limit, reduce the limit to match
            if(shownIndexLimit - firstShownIndex < visibleCount) { firstShownIndex -= visibleCount - (shownIndexLimit - firstShownIndex); } //if the number of displayed player prefs is smaller than the number we can display (like we're at the end of the list) then move the starting index back to adjust
            if(firstShownIndex < 0) { firstShownIndex = 0; } //can't have a negative index of a first shown player pref, so clamp to 0

            QUI.Space(firstShownIndex * rowHeight); //pad above the on screen results so that we're not wasting draw calls on invisible UI and the drawn player prefs end up in the same place in the list

            //QUI.Space(-12);

            DrawDataManagerDoubleLine(width);
            QUI.Space(SPACE_4);

            bool atLeastOneRowWasDrawn = false;

            for(int i = firstShownIndex; i < shownIndexLimit; i++) //for each of the on screen results
            {
                string fullKey = activePlayerPrefs[i].Key; //the full key is the key that's actually stored in player prefs
                string displayKey = fullKey; //display key is used so in the case of encrypted keys, we display the decrypted version instead (in auto-decrypt mode).
                object deserializedValue = activePlayerPrefs[i].Value; //used for accessing the type information stored against the player pref
                if(EzDataManagerSettings.showUnityPlayerPrefs == false)
                {
                    bool showRow = true;
                    for(int j = 0; j < UnitySpecificPlayerPrefs.Length; j++)
                    {
                        if(activePlayerPrefs[i].Key.Contains(UnitySpecificPlayerPrefs[j]))
                        {
                            showRow = false;
                            break;
                        }
                    }
                    if(!showRow)
                    {
                        continue;
                    }
                }
                if(deserializedValue == null) { continue; }

                QUI.BeginHorizontal(width);
                {
                    QUI.Space(SPACE_8);

                    Type valueType = deserializedValue.GetType(); //the type of player pref being stored

                    if(valueType == typeof(float)) //value display and user editing
                    {
                        QUI.Label("float", Style.Text.Small, DATA_MANAGER_TYPE_FIELD_WIDTH);
                        QUI.Label(displayKey, Style.Text.Normal, DATA_MANAGER_DATA_FIELD_WIDTH - 2); //display the PlayerPref key
                        float initialValue = PlayerPrefs.GetFloat(fullKey); //fetch the latest plain value from PlayerPrefs in memory
                        QUI.SetGUIBackgroundColor(EditorGUIUtility.isProSkin ? QColors.Green.Color : QColors.GreenLight.Color);
                        float newValue = EditorGUILayout.FloatField(initialValue, GUILayout.Width(DATA_MANAGER_DATA_FIELD_WIDTH)); //display the float editor field and get any changes in value
                        QUI.ResetColors();
                        if(newValue != initialValue) //if the value has changed
                        {
                            PlayerPrefs.SetFloat(fullKey, newValue);
                            PlayerPrefs.Save();
                        }
                    }
                    else if(valueType == typeof(int)) //value display and user editing
                    {
                        QUI.Label("int", Style.Text.Small, DATA_MANAGER_TYPE_FIELD_WIDTH);
                        QUI.Label(displayKey, Style.Text.Normal, DATA_MANAGER_DATA_FIELD_WIDTH - 2); //display the PlayerPref key
                        int initialValue = PlayerPrefs.GetInt(fullKey); //fetch the latest plain value from PlayerPrefs in memory
                        QUI.SetGUIBackgroundColor(EditorGUIUtility.isProSkin ? QColors.Green.Color : QColors.GreenLight.Color);
                        int newValue = EditorGUILayout.IntField(initialValue, GUILayout.Width(DATA_MANAGER_DATA_FIELD_WIDTH)); //display the int editor field and get any changes in value
                        QUI.ResetColors();
                        if(newValue != initialValue) //if the value has changed
                        {
                            PlayerPrefs.SetInt(fullKey, newValue);
                            PlayerPrefs.Save();
                        }
                    }
                    else if(valueType == typeof(string)) //value display and user editing
                    {
                        QUI.Label("string", Style.Text.Small, DATA_MANAGER_TYPE_FIELD_WIDTH);
                        QUI.Label(displayKey, Style.Text.Normal, DATA_MANAGER_DATA_FIELD_WIDTH - 2); //display the PlayerPref key
                        string initialValue = PlayerPrefs.GetString(fullKey); //fetch the latest plain value from PlayerPrefs in memory
                        QUI.SetGUIBackgroundColor(EditorGUIUtility.isProSkin ? QColors.Green.Color : QColors.GreenLight.Color);
                        string newValue = EditorGUILayout.TextField(initialValue, GUILayout.Width(DATA_MANAGER_DATA_FIELD_WIDTH)); //display the string editor field and get any changes in value
                        QUI.ResetColors();
                        if(newValue != initialValue) //if the value has changed
                        {
                            PlayerPrefs.SetString(fullKey, newValue);
                            PlayerPrefs.Save();
                        }
                    }

                    if(QUI.ButtonMinus())
                    {
                        PlayerPrefs.DeleteKey(fullKey); //delete the key from player prefs
                        PlayerPrefs.Save(); //save PlayerPrefs
                        DeleteCachedRecord(fullKey); //delete the cached record so the list updates immediately
                    }

                    QUI.FlexibleSpace();
                }
                QUI.EndHorizontal();

                QUI.Space(SPACE_8);

                if(i == shownIndexLimit - 1) //this is the last entry (draw a double line)
                {
                    DrawDataManagerDoubleLine(width);
                }
                else
                {
                    DrawDataManagerLine(width);
                }

                QUI.Space(SPACE_4);
                atLeastOneRowWasDrawn = true;
            }

            float bottomPadding = (entryCount - shownIndexLimit) * rowHeight; //calculate the padding at the bottom of the scroll view (because only visible player pref rows are drawn)
            if(bottomPadding > 0) //if the padding is positive, pad the bottom so that the layout and scroll view size is correct still
            {
                QUI.Space(bottomPadding);
            }

            if(!atLeastOneRowWasDrawn)
            {
                QUI.BeginHorizontal(width);
                {
                    QUI.Space(SPACE_8);
                    QUI.Label("PlayerPrefs list is empty...", Style.Text.Comment);
                    QUI.FlexibleSpace();
                }
                QUI.EndHorizontal();

                QUI.Space(SPACE_2);

                QUI.BeginHorizontal(width);
                {
                    QUI.Space(SPACE_8);
                    QUI.DrawLine(QColors.Color.Gray, width - SPACE_8 - SPACE_8);
                    QUI.Space(SPACE_8);
                }
                QUI.EndHorizontal();
                QUI.Space(SPACE_2);
                QUI.BeginHorizontal(width);
                {
                    QUI.Space(SPACE_8);
                    QUI.DrawLine(QColors.Color.Gray, width - SPACE_8 - SPACE_8);
                    QUI.Space(SPACE_8);
                }
                QUI.EndHorizontal();
            }
        }
        void RefreshPlayerPrefs()
        {
            if(Application.platform == RuntimePlatform.WindowsEditor)
            {

                if(!lastDeserialization.HasValue || DateTime.UtcNow - lastDeserialization.Value > TimeSpan.FromMilliseconds(500)) //Windows works a bit differently to OSX, we just regularly query the registry. So don't query too often
                {
                    deserializedPlayerPrefs = new List<PlayerPrefPair>(GetSavedPrefs(PlayerSettings.companyName, PlayerSettings.productName)); //deserialize the actual player prefs from registry into a cache
                    lastDeserialization = DateTime.UtcNow; //record the latest time, so we don't fetch again too quickly
                }
            }
            else if(Application.platform == RuntimePlatform.OSXEditor)
            {
                string plistFilename = string.Format("unity.{0}.{1}.plist", PlayerSettings.companyName, PlayerSettings.productName); //construct the plist filename from the project's settings
                string playerPrefsPath = Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "Library/Preferences"), plistFilename); //construct the fully qualified path                  
                DateTime lastWriteTime = File.GetLastWriteTimeUtc(playerPrefsPath); //determine when the plist was last written to
                if(!lastDeserialization.HasValue || lastDeserialization.Value != lastWriteTime) //if we haven't deserialized the player prefs already, or the written file has changed then deserialize the latest version
                {
                    deserializedPlayerPrefs = new List<PlayerPrefPair>(GetSavedPrefs(PlayerSettings.companyName, PlayerSettings.productName)); //deserialize the actual player prefs from file into a cache
                    lastDeserialization = lastWriteTime; //record the version of the file we just read, so we know if it changes in the future
                }

                if(lastWriteTime != MISSING_DATETIME)
                {
                    GUILayout.Label("PList Last Written: " + lastWriteTime.ToString());
                }
                else
                {
                    GUILayout.Label("PList Does Not Exist");
                }
            }
            else
            {
                Debug.Log("[EZ][DataManager] PlayerPrefs editing not supported on this Unity Editor platform.");
            }
        }
        void CacheRecord(string key, object value)
        {
            bool replaced = false; //first of all check if this key already exists, if so replace it's value with the new value

            int entryCount = deserializedPlayerPrefs.Count;
            for(int i = 0; i < entryCount; i++)
            {
                if(deserializedPlayerPrefs[i].Key == key) //found the key - it exists already
                {
                    deserializedPlayerPrefs[i] = new PlayerPrefPair() { Key = key, Value = value }; //update the cached pref with the new value
                    replaced = true; //mark the replacement so we no longer need to add it
                    break;
                }
            }

            if(!replaced) //player pref doesn't already exist (and wasn't replaced) so add it as new
            {
                deserializedPlayerPrefs.Add(new PlayerPrefPair() { Key = key, Value = value }); //cache a player pref the user just created so it can be instantly display (mainly for OSX)
            }

            //UpdateSearch(); //update the search if it's active
        }
        void DeleteCachedRecord(string fullKey)
        {
            keyQueuedForDeletion = fullKey;
        }
        void ClearPlayerPrefs()
        {
            RefreshPlayerPrefs();
            List<PlayerPrefPair> activePlayerPrefs = deserializedPlayerPrefs;
            if(activePlayerPrefs == null) { return; }
            for(int i = 0; i < activePlayerPrefs.Count; i++)
            {
                if(EzDataManagerSettings.showUnityPlayerPrefs == false)
                {
                    bool deleteKey = true;
                    for(int j = 0; j < UnitySpecificPlayerPrefs.Length; j++)
                    {
                        if(activePlayerPrefs[i].Key.Contains(UnitySpecificPlayerPrefs[j]))
                        {
                            deleteKey = false;
                            break;
                        }
                    }
                    if(!deleteKey)
                    {
                        continue;
                    }
                }
                PlayerPrefs.DeleteKey(activePlayerPrefs[i].Key); //delete the key from player prefs
                DeleteCachedRecord(activePlayerPrefs[i].Key); //delete the cached record so the list updates immediately
            }
            PlayerPrefs.Save(); //save PlayerPrefs
        }

        void DrawDataManagerEncryption(float width)
        {

            QUI.Space(SPACE_16);
            DrawDataManagerSettingsEncryptionKeys(width);
            QUI.Space(SPACE_8);
            DrawDataManagerSettingsDisclaimer(width + SPACE_8);

        }
        void DrawDataManagerSettingsEncryptionKeys(float width)
        {

            QUI.BeginHorizontal(width + SPACE_8);
            {
                QUI.Space(SPACE_8);
                QUI.Label("List of AES Encryption Data", Style.Text.Small);
                QUI.FlexibleSpace();
                if(QUI.GhostButton("Backup to CSV", QColors.Color.Gray, 120))
                {
                    if(EzEncryptionKeys.GetKeyCount() == 0)
                    {
                        QUI.DisplayDialog("Backup to CSV", "You cannot backup an empty list of AES Encryption Data.", "Ok");
                        return;
                    }
                    EzDataManagerUtils.BackupEncryptionKeysToCSV(EzEncryptionKeys);
                }
                QUI.Space(SPACE_2);
                if(QUI.GhostButton("Restore from CSV", QColors.Color.Gray, 120))
                {
                    if(EzDataManagerUtils.RestoreEncryptionKeysFromCSV(EzEncryptionKeys))
                    {
                        QUI.DisplayDialog("Success", "AES Encryption Data Keys have been restored successfully from CSV!", "Ok");
                        Selection.activeObject = null;
                    }
                    else
                    {
                        QUI.DisplayDialog("Failure", "Import error! The selected file does not contain valid AES Encryption Data Keys!", "Ok");
                    }
                }
            }
            QUI.EndHorizontal();

            QUI.Space(SPACE_4);
            DrawDataManagerDoubleLine(width + SPACE_16);
            QUI.Space(SPACE_4);

            if(EzEncryptionKeys.GetKeyCount() == 0)
            {
                QUI.BeginHorizontal(width);
                {
                    QUI.Space(SPACE_8);
                    QUI.Label("No encryption key has been created...", Style.Text.Comment);
                    QUI.FlexibleSpace();
                }
                QUI.EndHorizontal();
                QUI.Space(SPACE_4);
                DrawDataManagerDoubleLine(width + SPACE_16);
                QUI.Space(SPACE_4);
            }
            else
            {
                for(int i = 0; i < EzEncryptionKeys.GetKeyCount(); i++)
                {
                    QUI.BeginHorizontal(width + SPACE_8);
                    {
                        QUI.Space(SPACE_8);
                        QUI.Label("[ " + i + " ]", Style.Text.Normal, 30);
                        QUI.Space(-SPACE_4);
                        QUI.Label("Key", Style.Text.Normal);
                        QUI.FlexibleSpace();
                        if(QUI.ButtonMinus())
                        {
                            if(QUI.DisplayDialog("Delete Encryption Key",
                                                           "Are you sure you want to delete the [" + i + "] encryption key?" +
                                                           "\n\n" +
                                                           "Any data that has been encrypted with this key will be lost.",
                                                           "Delete Key",
                                                           "Cancel"))
                            {
                                EzEncryptionKeys.RemoveKeyAndIVAtIndex(i);
                                QUI.SetDirty(EzEncryptionKeys);
                                AssetDatabase.SaveAssets();
                                Selection.activeObject = null;
                                QUI.ExitGUI();
                            }
                        }
                    }
                    QUI.EndHorizontal();

                    QUI.Space(SPACE_2);

                    byte[] key = EzEncryptionKeys.GetKey(i);
                    QUI.BeginHorizontal(width);
                    {
                        for(int keyIndex = 0; keyIndex < key.Length; keyIndex++)
                        {
                            if(keyIndex % 16 == 0)
                            {
                                QUI.FlexibleSpace();
                                QUI.EndHorizontal();
                                QUI.Space(-SPACE_2);
                                QUI.BeginHorizontal(width);
                                QUI.Space(SPACE_8 + 30);
                            }
                            QUI.Label(key[keyIndex].ToString(), Style.Text.Small, (int) (width - SPACE_8 - 30 - SPACE_16 - SPACE_16 - SPACE_16 - SPACE_16) / 16);
                        }
                        QUI.FlexibleSpace();
                    }
                    QUI.EndHorizontal();

                    QUI.BeginHorizontal(width);
                    {
                        QUI.Space(SPACE_8 + 30);
                        QUI.Label("Initialization Vector", Style.Text.Normal);
                        QUI.FlexibleSpace();
                    }
                    QUI.EndHorizontal();

                    byte[] iv = EzEncryptionKeys.GetIV(i);
                    QUI.BeginHorizontal(width);
                    {
                        for(int ivIndex = 0; ivIndex < iv.Length; ivIndex++)
                        {
                            if(ivIndex % 16 == 0)
                            {
                                QUI.EndHorizontal();
                                QUI.Space(-SPACE_2);
                                QUI.BeginHorizontal(width);
                                QUI.Space(SPACE_8 + 30);
                            }
                            QUI.Label(iv[ivIndex].ToString(), Style.Text.Small, (int) (width - SPACE_8 - 30 - SPACE_16 - SPACE_16 - SPACE_16 - SPACE_16) / 16);
                        }
                    }
                    QUI.EndHorizontal();

                    QUI.Space(SPACE_4);
                    if(i == EzEncryptionKeys.GetKeyCount() - 1)
                    {
                        DrawDataManagerDoubleLine(width + SPACE_16);
                    }
                    else
                    {
                        DrawDataManagerLine(width + SPACE_16);
                    }
                    QUI.Space(SPACE_4);
                }

            }
            QUI.Space(SPACE_2);
            addEncryptionKeyInfoMessage.show.target = EzEncryptionKeys.GetKeyCount() == 0;
            QUI.DrawInfoMessage(addEncryptionKeyInfoMessage, width + SPACE_8);
            if(addEncryptionKeyInfoMessage.show.target) { QUI.Space(-20); }
            QUI.BeginHorizontal(width + SPACE_8);
            {
                QUI.FlexibleSpace();
                if(QUI.ButtonPlus())
                {
                    if(QUI.DisplayDialog("New Encryption Key",
                                                   "Do you want to geneterate a new AES Encryption Data set (Key and IV)?",
                                                   "Generate",
                                                   "Cancel"))
                    {
                        CreateNewEncryptionKey();
                        Selection.activeObject = null;
                    }
                }
                QUI.Space(SPACE_4 * addEncryptionKeyInfoMessage.show.faded);
            }
            QUI.EndHorizontal();
            QUI.Space(SPACE_4 * addEncryptionKeyInfoMessage.show.faded);
        }
        void DrawDataManagerSettingsDisclaimer(float width)
        {
            QUI.DrawInfoMessage(aboutEncryptionInfoMessage, width);
        }

        void CreateNewEncryptionKey()
        {
            byte[] key, iv;
            QuickEngine.Utils.QEncryption.GenerateAesKeyAndIV(out key, out iv);
            EzEncryptionKeys.AddNewKeyAndIV(key, iv);
            QUI.SetDirty(EzEncryptionKeys);
            AssetDatabase.SaveAssets();
            Repaint();
        }

        void DrawDataManagerLine(float width)
        {
            QUI.BeginHorizontal(width);
            {
                QUI.Space(SPACE_8);
                QUI.DrawLine(QColors.Color.Gray, width - SPACE_8 - SPACE_8);
                QUI.Space(SPACE_8);
            }
            QUI.EndHorizontal();
        }
        void DrawDataManagerDoubleLine(float width)
        {
            QUI.BeginHorizontal(width);
            {
                QUI.Space(SPACE_8);
                QUI.DrawLine(QColors.Color.Gray, width - SPACE_8 - SPACE_8);
                QUI.FlexibleSpace();
            }
            QUI.EndHorizontal();
            QUI.Space(SPACE_2);
            QUI.BeginHorizontal(width);
            {
                QUI.Space(SPACE_8);
                QUI.DrawLine(QColors.Color.Gray, width - SPACE_8 - SPACE_8);
                QUI.FlexibleSpace();
            }
            QUI.EndHorizontal();
        }
#endif
    }
}
