// Copyright (c) 2016 - 2018 Ez Entertainment SRL. All Rights Reserved.
// This code can only be used under the standard Unity Asset Store End User License Agreement
// A Copy of the EULA APPENDIX 1 is available at http://unity3d.com/company/legal/as_terms

using Ez.DataManager.Internal;
using QuickEditor;
using QuickEngine.Core;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.AnimatedValues;
using UnityEditorInternal;
using UnityEngine;

namespace Ez.DataManager
{
    [CustomEditor(typeof(EzDataManager))]
    [DisallowMultipleComponent]
    public class EzDataManagerEditor : QEditor
    {
        EzDataManager ezDataManager { get { return (EzDataManager)target; } }

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

        public const string CATEGORY_START = "CTGSTRT_";
        public const string CATEGORY_END = "CTGEND_";

        #region variableTypes, typeNames, invalidVariableNames
        public string[] variableTypes = new string[]
        {
            "variable", "array", "list"
        };

        public string[] typeNames = new string[]
        {
            "AnimationCurve","AudioClip",
            "bool",
            "Color","Color32",
            "double",
            "float",
            "GameObject",
            "int",
            "long",
            "Material","Mesh",
            "Object",
            "ParticleSystem",
            "Quaternion",
            "Rect","RectTransform",
            "Sprite","string",
            "TerrainData","Texture","Transform",
            "Vector2","Vector3","Vector4"
        };

        public string[] invalidVariableNames = new string[]
        {
            //typeNames
            "AnimationCurve","AudioClip",
            "bool",
            "Color","Color32",
            "double",
            "float",
            "GameObject",
            "int",
            "long",
            "Material","Mesh",
            "Object",
            "ParticleSystem",
            "Quaternion",
            "Rect","RectTransform",
            "Sprite","string",
            "TerrainData","Texture","Transform",
            "Vector2","Vector3","Vector4",
            //C# Keywords
            "abstract", "as", "base", "break", "byte", "case", "catch", "char", "checked", "class", "const", "continue", "decimal", "default", "delegate", "do", "else", "enum", "event", "explicit", "extern",
            "false", "finally", "fixed", "for", "foreach", "goto", "if", "implicit", "in", "interface", "internal", "is", "lock", "namespace", "new", "null", "object", "operator", "out", "override",
            "params", "private", "protected", "public", "readonly", "ref", "return", "sbyte", "sealed", "short", "sizeof", "stackalloc", "static", "struct", "switch", "this", "throw", "true", "try", "typeof",
            "uint", "ulong", "unchecked", "unsafe", "ushort", "using", "virtual", "void", "volatile", "while",
            //Contextual Keywords
            "add", "alias", "ascending", "async", "await", "descending", "dynamic", "from", "get", "global", "group", "into", "join", "let", "orderby", "partial", "remove", "select", "set", "value", "var", "where", "yield"
        };
        #endregion

        [Serializable]
        public class Variable
        {
            public string variableType;
            public string typeName;
            public string name;
            public SerializedProperty sp;
            public bool saveVariable;
            public bool useEncryption;
            public int encryptionKey = -1;
        }

        FieldInfo[] fields;
        const BindingFlags fieldsFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;

        Dictionary<string, List<Variable>> data;
        Dictionary<string, List<float>> dataHeight;
        Dictionary<string, AnimBool> showCategory;
        Dictionary<string, ReorderableList> rData;

        SerializedProperty autoDataSave, autoDataLoad;

        AnimFloat variableNameWidth;

        AnimBool addCategory, showSettings, unsavedChanges, renameCategory, addVariable;

        string newCategoryName, renameCategoryName, activeCategoryName = string.Empty;
        string addVariableVariableType, addVariableTypeName, addVariableName = string.Empty;
        int addVariableVariableTypeIndex = 0;
        int addVariableTypeNameIndex = 4;

        int[] encryptionKeysArray;
        string[] encryptionKeysDisplayOptionArray;

        protected override void OnEnable()
        {
            base.OnEnable();

            requiresContantRepaint = true;
            CreateEncryptionKeysArray();
        }

        protected override void OnDisable()
        {
            if(variableNameWidth.target != EzDataManagerSettings.variableNameWidth)
            {
                SaveVariableNameWidth((int)variableNameWidth.target);
            }
        }

        private void CreateEncryptionKeysArray()
        {
            if(EzEncryptionKeys.GetKeyCount() == 0)
            {
                return; //if no encryption keys have been generated we leave encryptionKeysArray as null
            }

            encryptionKeysArray = new int[EzEncryptionKeys.GetKeyCount()];
            encryptionKeysDisplayOptionArray = new string[encryptionKeysArray.Length];

            for(int i = 0; i < encryptionKeysArray.Length; i++)
            {
                encryptionKeysArray[i] = i;
                encryptionKeysDisplayOptionArray[i] = "key " + i.ToString();
            }
        }

        protected override void SerializedObjectFindProperties()
        {
            base.SerializedObjectFindProperties();

            autoDataSave = serializedObject.FindProperty("autoDataSave");
            autoDataLoad = serializedObject.FindProperty("autoDataLoad");
        }

        protected override void InitAnimBools()
        {
            base.InitAnimBools();

            addCategory = new AnimBool(false, Repaint);
            showSettings = new AnimBool(false, Repaint);
            unsavedChanges = new AnimBool(false, Repaint);
            addVariable = new AnimBool(false, Repaint);
            renameCategory = new AnimBool(false, Repaint);

            variableNameWidth = new AnimFloat(EzDataManagerSettings.variableNameWidth, Repaint);
        }

        protected override void GenerateInfoMessages()
        {
            base.GenerateInfoMessages();

            infoMessage.Add("DataSavingDisclaimer", new InfoMessage()
            {
                title = "Data Saving",
                message = "EzDataManager will attempt to save all variables marked to be saved to PlayerPrefs when the app pauses, loses focus or quits." +
                            "\n\n" +
                            "This code is not guaranteed to run since the operating system might force the application to quit early." +
                            "\n\n" +
                            "It is highly recommended that you manually call EzDataManager's data saving methods at break points in your game to ensure the data is saved accordingly.",
                type = InfoMessageType.Help,
                show = new AnimBool(false, Repaint)
            });

            infoMessage.Add("DataLoadingDisclaimer",
                            new InfoMessage()
                            {
                                title = "Data Loading",
                                message = "EzDataManager will automatically load the variables marked to be loaded from PlayerPrefs on Awake.",
                                type = InfoMessageType.Help,
                                show = new AnimBool(false, Repaint)
                            });

            infoMessage.Add("CategoryIsEmpty",
                            new InfoMessage()
                            {
                                title = "Category is empty",
                                message = "",
                                type = InfoMessageType.Info,
                                show = new AnimBool(true, Repaint)
                            });

            infoMessage.Add("ApplyChanges",
                            new InfoMessage()
                            {
                                title = "Apply Changes",
                                message = "In order for the changes that you made to be persistent, you need to click the 'Apply Changes' button.",
                                type = InfoMessageType.Warning,
                                show = new AnimBool(false, Repaint)
                            });
        }

        protected override void InitializeVariables()
        {
            base.InitializeVariables();

            unsavedChanges.target = false;

            variableNameWidth.value = Mathf.Clamp(variableNameWidth.value, 40, 210);

            data = new Dictionary<string, List<Variable>>();
            dataHeight = new Dictionary<string, List<float>>();
            showCategory = new Dictionary<string, AnimBool>();
            rData = new Dictionary<string, ReorderableList>();
            fields = ezDataManager.GetType().GetFields(fieldsFlags);
            string currentCategory = string.Empty;

            foreach(var field in fields)
            {
                if(field.Name.Contains(CATEGORY_START)) //starting to add variables to a new active category
                {
                    currentCategory = serializedObject.FindProperty(field.Name).stringValue;
                    AddCategory(currentCategory);
                }
                else
                {
                    if(field.Name.Contains(CATEGORY_END)) currentCategory = string.Empty; //stopping adding values to the active category
                }

                if(!string.IsNullOrEmpty(currentCategory) && !field.Name.Contains(CATEGORY_START)) //add the variable to the database only if we have an active category selected and if this is not a category 'header'
                {
                    VariableData variableData = EzDataManagerSettings.variableData.Find(new VariableData(field.Name).Equals);
                    if(variableData == null) { variableData = new VariableData(field.Name); }

                    data[currentCategory].Add(new Variable()
                    {
                        name = field.Name,
                        variableType = GetVariableType(field.FieldType.ToString()),
                        typeName = GetTypeName(field.FieldType.ToString()),
                        sp = serializedObject.FindProperty(field.Name),
                        saveVariable = variableData.saveVariable,
                        useEncryption = variableData.useEncryption,
                        encryptionKey = variableData.encryptionKey
                    });
                    dataHeight[currentCategory].Add(EditorGUIUtility.singleLineHeight);
                }
            }

            if(renameCategory.value) { RenameCategoryReset(); }
            if(addVariable.value) { AddVariableReset(); }
        }

        public override void OnInspectorGUI()
        {
            DrawHeader(EZResources.editorHeaderEzDataManager.texture, WIDTH_420, HEIGHT_42);
            if(EditorApplication.isCompiling)
            {
                QUI.GhostTitle("Editor is compiling...", QColors.Color.Gray, WIDTH_420);
                QUI.Space(SPACE_4);
                return;
            }
            CheckDragNDrop();
            serializedObject.Update();

            infoMessage["ApplyChanges"].show.target = unsavedChanges.target;
            DrawInfoMessage("ApplyChanges", WIDTH_420);

            QUI.Space(SPACE_4 * unsavedChanges.faded);

            DrawTopMenu();
            if(data == null || data.Keys == null) { InitializeVariables(); }
            if(data.Keys.Count > 0)
            {
                QUI.Space(SPACE_2);
                foreach(var key in data.Keys)
                {
                    QUI.Space(SPACE_2);
                    QUI.BeginHorizontal();
                    {
                        if(QUI.GhostBar(key, QColors.Color.Gray, showCategory[key], WIDTH_420, 24))
                        {
                            if(showCategory[key].value)
                            {
                                showCategory[key].target = false;
                                CloseCategory(key);
                                if(key.Equals(activeCategoryName))
                                {
                                    if(renameCategory.value) { RenameCategoryReset(); }
                                    if(addVariable.value) { AddVariableReset(); }
                                }

                            }
                            else
                            {
                                showCategory[key].target = true;

                            }
                        }
                        QUI.FlexibleSpace();
                    }
                    QUI.EndHorizontal();

                    if(QUI.BeginFadeGroup(showCategory[key].faded))
                    {
                        QUI.Space(SPACE_2);
                        if(showCategory[key].faded > 0.1f)
                        {
                            DrawCategory(key);
                        }
                    }
                    QUI.EndFadeGroup();
                }
            }
            serializedObject.ApplyModifiedProperties();
            QUI.Space(SPACE_4);
        }

        private void CheckDragNDrop()
        {
            switch(Event.current.type)
            {
                //case EventType.MouseDown: DragAndDrop.PrepareStartDrag(); break; //Debug.Log("MouseDown"); //reset the DragAndDrop Data
                case EventType.DragUpdated: DragAndDrop.visualMode = DragAndDropVisualMode.Copy; break; //Debug.Log("DragUpdated " + Event.current.mousePosition);
                case EventType.DragPerform: DragAndDrop.AcceptDrag(); break; //Debug.Log("Drag accepted");
                //case EventType.MouseDrag: DragAndDrop.StartDrag("Dragging"); Event.current.Use(); break; //Debug.Log("MouseDrag: " + Event.current.mousePosition);
                case EventType.MouseUp: DragAndDrop.PrepareStartDrag(); break; //Debug.Log("MouseUp had " + DragAndDrop.GetGenericData("GameObject"));  //Clean up, in case MouseDrag never occurred
                case EventType.DragExited: break; //Debug.Log("DragExited");
            }
        }

        private void ToggleNewCategory()
        {
            addCategory.target = !addCategory.value;
            newCategoryName = string.Empty;
            showSettings.target = false;
        }

        private void DrawTopMenu()
        {
            QUI.BeginHorizontal(WIDTH_420);
            {
                QUI.FlexibleSpace();
                if(unsavedChanges.faded > 0.05f)
                {
                    if(QUI.SlicedButton("Apply Changes", QColors.Color.Green, 120 * unsavedChanges.faded, 24))
                    {
                        UpdateVariableData(data);
                        EzDataManagerWriter.GenerateEzDataManagerScript(data);
                    }
                    QUI.Space(SPACE_2 * unsavedChanges.faded);
                }

                if(QUI.GhostButton("Reload Data", QColors.Color.Blue, 139 - (37 * unsavedChanges.faded), 24))
                {
                    InitializeVariables();
                }
                QUI.Space(SPACE_2);
                if(QUI.GhostButton("New Category", QColors.Color.Green, 139 - (27 * unsavedChanges.faded), 24, addCategory.value))
                {
                    ToggleNewCategory();
                }
                QUI.Space(SPACE_2);
                if(QUI.GhostButton("Settings", QColors.Color.Purple, 139 - (59 * unsavedChanges.faded), 24, showSettings.value))
                {
                    showSettings.target = !showSettings.value;
                    addCategory.target = false;
                    newCategoryName = string.Empty;
                }
                QUI.FlexibleSpace();
            }
            QUI.EndHorizontal();

            if(addCategory.faded == 1 &&
               Event.current.isKey &&
               Event.current.keyCode == KeyCode.Escape &&
               Event.current.type == EventType.KeyUp)
            {
                AddCategoryReset();
            }

            if(addCategory.faded == 1 &&
                Event.current.isKey &&
                Event.current.keyCode == KeyCode.Return &&
                Event.current.type == EventType.KeyUp &&
                GUI.GetNameOfFocusedControl() == "newCategoryName")
            {
                if(AddCategory(newCategoryName))
                {
                    unsavedChanges.target = true;
                }
                QUI.ExitGUI();
            }

            if(showSettings.faded == 1 &&
            Event.current.isKey &&
            Event.current.keyCode == KeyCode.Escape &&
            Event.current.type == EventType.KeyUp)
            {
                showSettings.target = false;
                addCategory.target = false;
                newCategoryName = string.Empty;
            }

            DrawNewCategoryFields();
            DrawSettingsFields();
        }

        private void UpdateVariableData(Dictionary<string, List<Variable>> data)
        {
            EzDataManagerSettings.variableData = new List<VariableData>();
            foreach(var key in data.Keys)
            {
                foreach(var item in data[key])
                {
                    EzDataManagerSettings.variableData.Add(new VariableData(item.name, item.saveVariable, item.useEncryption, item.encryptionKey));
                }
            }
            QUI.SetDirty(EzDataManagerSettings);
            AssetDatabase.SaveAssets();
        }

        private void DrawNewCategoryFields()
        {
            if(QUI.BeginFadeGroup(addCategory.faded))
            {
                QUI.Space(SPACE_8 * addCategory.faded);
                QUI.BeginHorizontal(WIDTH_420);
                {
                    GUI.SetNextControlName("newCategoryName");
                    newCategoryName = QUI.TextField(newCategoryName, EditorGUIUtility.isProSkin ? QColors.Green.Color : QColors.GreenLight.Color, (WIDTH_420 - 38) * addCategory.faded);
                    if(QUI.ButtonCancel())
                    {
                        AddCategoryReset();
                    }
                    QUI.Space(SPACE_2);
                    if(QUI.ButtonOk())
                    {
                        if(AddCategory(newCategoryName))
                        {
                            unsavedChanges.target = true;
                        }
                    }
                    QUI.FlexibleSpace();
                }
                QUI.EndHorizontal();
                QUI.Space(SPACE_8 * addCategory.faded);
            }
            QUI.EndFadeGroup();
        }

        private void DrawSettingsFields()
        {
            if(QUI.BeginFadeGroup(showSettings.faded))
            {
                QUI.Space(SPACE_8 * showSettings.faded);
                QUI.BeginHorizontal(WIDTH_420);
                {
                    QUI.Space(SPACE_8 * showSettings.faded);
                    QUI.Toggle(autoDataSave, "Auto Data Save");
                    QUI.FlexibleSpace();
                }
                QUI.EndHorizontal();
                QUI.BeginHorizontal(WIDTH_420);
                {
                    QUI.Space(SPACE_8 * showSettings.faded);
                    infoMessage["DataSavingDisclaimer"].show.target = autoDataSave.boolValue;
                    DrawInfoMessage("DataSavingDisclaimer", WIDTH_420 - SPACE_16);
                    QUI.Space(SPACE_8 * showSettings.faded);
                }
                QUI.EndHorizontal();

                QUI.Space(SPACE_4 * infoMessage["DataSavingDisclaimer"].show.faded);

                QUI.BeginHorizontal(WIDTH_420);
                {
                    QUI.Space(SPACE_8 * showSettings.faded);
                    QUI.Toggle(autoDataLoad, "Auto Data Load");
                    QUI.FlexibleSpace();
                }
                QUI.EndHorizontal();
                QUI.BeginHorizontal(WIDTH_420);
                {
                    QUI.Space(SPACE_8 * showSettings.faded);
                    infoMessage["DataLoadingDisclaimer"].show.target = autoDataLoad.boolValue;
                    DrawInfoMessage("DataLoadingDisclaimer", WIDTH_420 - SPACE_16);
                    QUI.Space(SPACE_8 * showSettings.faded);
                }
                QUI.EndHorizontal();

                QUI.Space(SPACE_4 + SPACE_8 * infoMessage["DataLoadingDisclaimer"].show.faded);

                QUI.BeginHorizontal(WIDTH_420);
                {
                    QUI.Space(SPACE_8 * showSettings.faded);
                    QUI.Label("Variable Name Width", Style.Text.Normal, 126 * showSettings.faded);
                    QUI.SetGUIBackgroundColor(EditorGUIUtility.isProSkin ? QColors.Orange.Color : QColors.OrangeLight.Color);
                    variableNameWidth.target = EditorGUILayout.Slider(variableNameWidth.target, 40, 210, GUILayout.Width(252 * showSettings.faded));
                    QUI.ResetColors();
                    if(QUI.ButtonReset())
                    {
                        variableNameWidth.target = 120f;
                        SaveVariableNameWidth((int)variableNameWidth.target);
                    }
                    QUI.FlexibleSpace();
                }
                QUI.EndHorizontal();
                QUI.Space(SPACE_8);
                QUI.BeginHorizontal(WIDTH_420);
                {
                    QUI.Space(SPACE_8);
                    QUI.DrawTexture(QResources.lineGray.texture, 20 * showSettings.faded, 6);
                    QUI.Space(20 * showSettings.faded - 8);
                    QUI.DrawTexture(QResources.lineOrange.texture, variableNameWidth.value * showSettings.faded, 6);
                    QUI.Space(variableNameWidth.value * showSettings.faded - 8);
                    QUI.DrawTexture(QResources.lineGray.texture, (WIDTH_420 - 26 - variableNameWidth.value - 10) * showSettings.faded, 6);
                    GUILayout.FlexibleSpace();
                }
                QUI.EndHorizontal();
                QUI.Space(SPACE_16 * showSettings.faded);
            }
            QUI.EndFadeGroup();
        }

        private void SaveVariableNameWidth(int newValue)
        {
            EzDataManagerSettings.variableNameWidth = newValue;
            QUI.SetDirty(EzDataManagerSettings);
            AssetDatabase.SaveAssets();
        }

        private void DrawCategory(string categoryName)
        {
            QUI.BeginHorizontal(WIDTH_420);
            {
                QUI.Space(SPACE_8);
                if(!addVariable.value && !renameCategory.value)
                {
                    if(QUI.GhostButton("Add Variable", QColors.Color.Gray, (WIDTH_420 - SPACE_8 - SPACE_2 - SPACE_2) / 3, 20 * showCategory[categoryName].faded * (1 - addVariable.faded) * (1 - renameCategory.faded)))
                    {
                        addVariable.target = true;
                        addVariableTypeNameIndex = 0;
                        activeCategoryName = categoryName;
                        addVariableName = string.Empty;
                        unsavedChanges.target = true;
                    }
                    QUI.Space(SPACE_2);
                    if(QUI.GhostButton("Rename Category", QColors.Color.Gray, (WIDTH_420 - SPACE_8 - SPACE_2 - SPACE_2) / 3, 20 * showCategory[categoryName].faded * (1 - addVariable.faded) * (1 - renameCategory.faded)))
                    {
                        activeCategoryName = categoryName;

                        bool categoryExistsInTheFile = false;
                        foreach(var field in fields)
                        {
                            if(field.Name.Contains(CATEGORY_START) && serializedObject.FindProperty(field.Name).stringValue.Equals(activeCategoryName))
                            {
                                categoryExistsInTheFile = true;
                            }
                        }
                        if(categoryExistsInTheFile == false)
                        {
                            EzDataManagerWriter.GenerateEzDataManagerScript(data); //we don't have this category in the file so we force a file write
                        }

                        renameCategory.target = true;
                        renameCategoryName = categoryName;
                    }

                    if(renameCategory.target)
                    {
                        QUI.FocusTextInControl("renameCategoryName" + categoryName);
                    }

                    QUI.Space(SPACE_2);
                    if(QUI.GhostButton("Delete Category", QColors.Color.Gray, (WIDTH_420 - SPACE_8 - SPACE_2 - SPACE_2) / 3, 20 * showCategory[categoryName].faded * (1 - addVariable.faded) * (1 - renameCategory.faded)))
                    {
                        if(QUI.DisplayDialog("Delete Category", "Are you sure you want to delete the '" + categoryName + "' Category? This will delete all the variables inside of it as well. Operation cannot be undone!", "Ok", "Cancel"))
                        {
                            DeleteCategory(categoryName);
                        }
                    }
                }
                else if(addVariable.faded > 0.5f && activeCategoryName.Equals(categoryName))
                {
                    QUI.SetGUIBackgroundColor(EditorGUIUtility.isProSkin ? QColors.Blue.Color : QColors.BlueLight.Color);
                    addVariableVariableTypeIndex = EditorGUILayout.Popup(addVariableVariableTypeIndex, variableTypes, GUILayout.Width(60), GUILayout.Height(EditorGUIUtility.singleLineHeight * addVariable.faded));
                    addVariableVariableType = variableTypes[addVariableVariableTypeIndex];
                    addVariableTypeNameIndex = EditorGUILayout.Popup(addVariableTypeNameIndex, typeNames, GUILayout.Width(100), GUILayout.Height(EditorGUIUtility.singleLineHeight * addVariable.faded));
                    addVariableTypeName = typeNames[addVariableTypeNameIndex];
                    GUI.SetNextControlName("addVariableName");
                    addVariableName = QUI.TextField(addVariableName, 204, EditorGUIUtility.singleLineHeight * addVariable.faded);
                    QUI.ResetColors();
                    if(QUI.ButtonCancel()) { AddVariableReset(); }
                    QUI.Space(SPACE_2);
                    if(QUI.ButtonOk()) { if(AddVariable()) { unsavedChanges.target = true; } }

                    if(addVariable.faded == 1 &&
                        Event.current.isKey &&
                        Event.current.keyCode == KeyCode.Escape &&
                        Event.current.type == EventType.KeyUp)
                    {
                        AddVariableReset();
                    }

                    if(addVariable.faded == 1 &&
                        Event.current.isKey &&
                        Event.current.keyCode == KeyCode.Return &&
                        Event.current.type == EventType.KeyUp &&
                        GUI.GetNameOfFocusedControl() == "addVariableName")
                    {
                        if(AddVariable())
                        {
                            unsavedChanges.target = true;
                        }
                    }

                }
                else if(renameCategory.faded > 0.5f && activeCategoryName.Equals(categoryName))
                {
                    GUI.SetNextControlName("renameCategoryName" + categoryName);
                    renameCategoryName = QUI.TextField(renameCategoryName, EditorGUIUtility.isProSkin ? QColors.Orange.Color : QColors.OrangeLight.Color, 372, EditorGUIUtility.singleLineHeight * renameCategory.faded);
                    if(QUI.ButtonCancel()) { RenameCategoryReset(); }
                    QUI.Space(SPACE_2);
                    if(QUI.ButtonOk()) { if(RenameCategory()) { unsavedChanges.target = true; } }

                    if(renameCategory.faded == 1
                        && QUI.DetectKeyUp(Event.current, KeyCode.Escape))
                    {
                        RenameCategoryReset();
                    }

                    if(renameCategory.faded == 1
                        && QUI.DetectKeyUp(Event.current, KeyCode.Return)
                        && GUI.GetNameOfFocusedControl() == "renameCategoryName" + categoryName)
                    {
                        if(RenameCategory())
                        {
                            unsavedChanges.target = true;
                        }
                    }
                }
                else
                {
                    QUI.Space(SPACE_8);
                }
                QUI.FlexibleSpace();
            }
            QUI.EndHorizontal();

            QUI.Space(SPACE_4);

            if(rData[categoryName].count == 0)
            {
                QUI.BeginHorizontal(WIDTH_420);
                {
                    QUI.Space(SPACE_8);
                    DrawInfoMessage("CategoryIsEmpty", WIDTH_420 - SPACE_8);
                }
                QUI.EndHorizontal();
                QUI.Space(SPACE_2);
            }
            else
            {
                QUI.BeginHorizontal(WIDTH_420);
                {
                    QUI.Space(SPACE_8);
                    rData[categoryName].DoLayoutList();
                    QUI.FlexibleSpace();
                }
                QUI.EndHorizontal();
            }
        }

        private bool AddCategory(string categoryName)
        {
            if(string.IsNullOrEmpty(categoryName))
            {
                QUI.DisplayDialog("New category", "You cannot create a new category with no name.", "Ok");
                return false;
            }
            if(data == null) { data = new Dictionary<string, List<Variable>>(); }
            if(dataHeight == null) { dataHeight = new Dictionary<string, List<float>>(); }
            if(showCategory == null) { showCategory = new Dictionary<string, AnimBool>(); }

            if(!data.ContainsKey(categoryName))
            {
                data.Add(categoryName, new List<Variable>());
                dataHeight.Add(categoryName, new List<float>());
                showCategory[categoryName] = new AnimBool(false, Repaint);
                rData[categoryName] = new ReorderableList(data[categoryName], typeof(Variable), true, false, false, false)
                {
                    onReorderCallback = (list) =>
                    {
                        unsavedChanges.target = true;
                    },
                    showDefaultBackground = false,
                    drawElementBackgroundCallback = (rect, index, active, focused) => { },
                    elementHeightCallback = (index) =>
                    {
                        Repaint();
                        float height = EditorGUIUtility.singleLineHeight + SPACE_4;
                        try { height = dataHeight[categoryName][index]; }
                        catch(ArgumentOutOfRangeException e) { Debug.LogWarning(e.Message); }

                        return height;
                    }
                };

                rData[categoryName].drawElementCallback = (rect, index, active, focused) =>
                {
                    if(index == rData[categoryName].list.Count) return;
                    float height = EditorGUIUtility.singleLineHeight + SPACE_4;
                    float buttonSize = 16;
                    float dragBumbWidth = 0;
                    rect.x += dragBumbWidth;
                    Variable v = (Variable)rData[categoryName].list[index];
                    QUI.Label(new Rect(rect.x, rect.y, variableNameWidth.value, EditorGUIUtility.singleLineHeight), v.name);
                    rect.x += variableNameWidth.value;
                    rect.x += SPACE_2;
                    float valueFieldWidth = WIDTH_420 - SPACE_8 - variableNameWidth.value - SPACE_2 - buttonSize - SPACE_16 - SPACE_8;
                    #region Variable Value Field
                    if(v.sp == null)
                    {
                        if(!addVariable.value && !renameCategory.value)
                        {
                            if(QUI.SlicedButton(new Rect(rect.x, rect.y, valueFieldWidth, EditorGUIUtility.singleLineHeight), "Add variable to Data Manager", QColors.Color.Green))
                            {
                                EzDataManagerWriter.GenerateEzDataManagerScript(data);
                            }
                        }
                        return;
                    }


                    //check what variable type we have in order to calculate the valueFieldWidth (to accomodate two new buttons)
                    bool addSavebutton = false;
                    bool addEncryptionbutton = false;
                    if(v.typeName == "bool" ||
                       v.typeName == "float" ||
                       v.typeName == "int" ||
                       v.typeName == "string")
                    {
                        addSavebutton = true;
                        valueFieldWidth -= (buttonSize + 2); //save button size + spacing

                        if(v.saveVariable)
                        {
                            addEncryptionbutton = true;
                            valueFieldWidth -= (buttonSize + 2); //encryption button size + spacing
                            if(v.useEncryption)
                            {
                                valueFieldWidth -= buttonSize * 3 + 2; //encryption dropdown list width
                            }
                        }
                        else
                        {
                            v.useEncryption = false;
                            v.encryptionKey = -1;
                        }
                    }

                    if(v.variableType.Equals("variable"))
                    {
                        switch(v.typeName)
                        {
                            case "Quaternion": DrawQuaternion(v.sp, rect, valueFieldWidth - 3); break;
                            case "Rect": DrawRect(v.sp, rect, valueFieldWidth - 3); break;
                            case "Vector4": DrawVector4(v.sp, rect, valueFieldWidth - 3); break;
                            default: EditorGUI.PropertyField(new Rect(rect.x, rect.y, valueFieldWidth, EditorGUIUtility.singleLineHeight), v.sp, GUIContent.none, true); break;
                        }

                        DrawSaveAndEncryptButtons(addSavebutton, addEncryptionbutton, rect, valueFieldWidth, buttonSize, v);

                        if(QUI.ButtonCancel(new Rect(rect.x + valueFieldWidth + 4 + (addSavebutton ? buttonSize + 2 : 0) + (addEncryptionbutton ? buttonSize + 2 : 0) + (v.useEncryption ? buttonSize * 3 + 2 : 0), rect.y, buttonSize, buttonSize)))
                        {
                            DeleteVariable(categoryName, index);
                        }
                    }
                    else
                    {
                        string varBarText = string.Empty;
                        if(v.variableType.Equals("array")) varBarText = v.typeName + " [" + v.sp.arraySize + "]";
                        if(v.variableType.Equals("list")) varBarText = "List<" + v.typeName + "> [" + v.sp.arraySize + "]";
                        Rect dropBox = new Rect(rect.x - variableNameWidth.value, rect.y, WIDTH_420 - 40, EditorGUIUtility.singleLineHeight);
                        if(!v.sp.isExpanded)
                        {
                            if(QUI.GhostButton(new Rect(rect.x, rect.y, valueFieldWidth, EditorGUIUtility.singleLineHeight), varBarText, QColors.Color.Gray, v.sp.isExpanded))
                            {
                                v.sp.isExpanded = true;
                            }

                            DrawSaveAndEncryptButtons(addSavebutton, addEncryptionbutton, rect, valueFieldWidth, buttonSize, v);

                            if(QUI.ButtonCancel(new Rect(rect.x + valueFieldWidth + 4 + (addSavebutton ? buttonSize + 2 : 0) + (addEncryptionbutton ? buttonSize + 2 : 0) + (v.useEncryption ? buttonSize * 3 + 2 : 0), rect.y, buttonSize, buttonSize)))
                            {
                                DeleteVariable(categoryName, index);
                            }
                        }
                        else
                        {
                            if(addSavebutton)
                            {
                                valueFieldWidth += (buttonSize + 2); //save button size + spacing

                                if(v.saveVariable)
                                {
                                    valueFieldWidth += (buttonSize + 2); //encryption button size + spacing
                                    if(v.useEncryption)
                                    {
                                        valueFieldWidth += buttonSize * 3 + 2; //encryption dropdown list width
                                    }
                                }
                            }

                            valueFieldWidth += buttonSize + 5;
                            if(QUI.GhostButton(new Rect(rect.x, rect.y, valueFieldWidth, EditorGUIUtility.singleLineHeight), varBarText, QColors.Color.Gray, v.sp.isExpanded))
                            {
                                v.sp.isExpanded = false;
                            }
                            if(v.sp.arraySize > 0)
                            {
                                for(int i = 0; i < v.sp.arraySize; i++)
                                {
                                    rect.y += EditorGUIUtility.singleLineHeight + 4;
                                    height += EditorGUIUtility.singleLineHeight + 4;
                                    GUIContent indexText = new GUIContent("[" + i + "]");
                                    Vector2 indexWidth = QStyles.CalcSize(indexText, Style.Text.Normal);
                                    QUI.Label(new Rect(rect.x - indexWidth.x - 2, rect.y, indexWidth.x, EditorGUIUtility.singleLineHeight), indexText.text, Style.Text.Normal);
                                    switch(v.typeName)
                                    {
                                        case "Quaternion": DrawQuaternion(v.sp.GetArrayElementAtIndex(i), rect, valueFieldWidth - 3 - 2 * buttonSize, true); break;
                                        case "Rect": DrawRect(v.sp.GetArrayElementAtIndex(i), rect, valueFieldWidth - 3 - 2 * buttonSize, true); break;
                                        case "Vector4": DrawVector4(v.sp.GetArrayElementAtIndex(i), rect, valueFieldWidth - 3 - 2 * buttonSize, true); break;
                                        default: EditorGUI.PropertyField(new Rect(rect.x, rect.y, valueFieldWidth - 2 * buttonSize - 4, EditorGUIUtility.singleLineHeight), v.sp.GetArrayElementAtIndex(i), GUIContent.none, true); break;
                                    }
                                    var currentIndex = i;
                                    if(QUI.ButtonMinus(new Rect(rect.x + valueFieldWidth - 2 * buttonSize - 2, rect.y, buttonSize, buttonSize)))
                                    {
                                        DeleteArrayElement(v.sp, currentIndex);
                                    }
                                    if(QUI.ButtonPlus(new Rect(rect.x + valueFieldWidth - 1 * buttonSize - 1, rect.y, buttonSize, buttonSize)))
                                    {
                                        InsertArrayElement(v.sp, currentIndex, v.typeName);
                                    }
                                }
                            }
                            rect.y += EditorGUIUtility.singleLineHeight + 4;
                            height += EditorGUIUtility.singleLineHeight + 4;
                            if(QUI.ButtonPlus(new Rect(rect.x + valueFieldWidth - 1 * buttonSize - 1, rect.y, buttonSize, buttonSize)))
                            {
                                InsertArrayElement(v.sp, v.sp.arraySize, v.typeName);
                            }
                            rect.y += EditorGUIUtility.singleLineHeight + 4;
                            height += EditorGUIUtility.singleLineHeight + 4;
                        }

                        if(v.typeName.Equals("AudioClip") ||
                            v.typeName.Equals("GameObject") ||
                            v.typeName.Equals("Material") ||
                            v.typeName.Equals("Mesh") ||
                            v.typeName.Equals("ParticleSystem") ||
                            v.typeName.Equals("RectTransform") ||
                            v.typeName.Equals("Sprite") ||
                            v.typeName.Equals("Texture") ||
                            v.typeName.Equals("TerrainData") ||
                            v.typeName.Equals("Transform") ||
                            v.typeName.Equals("Object"))
                        {
                            if(DragAndDrop.objectReferences.Length > 0 && dropBox.Contains(Event.current.mousePosition))
                            {
                                if(IsObjectValid(DragAndDrop.objectReferences[0], v.typeName))
                                {
                                    GUI.Box(new Rect(dropBox.x + variableNameWidth.value, dropBox.y, valueFieldWidth, dropBox.height),
                                            DragAndDrop.objectReferences.Length == 1
                                            ? "Drop " + DragAndDrop.objectReferences.Length + " item..."
                                            : "Drop " + DragAndDrop.objectReferences.Length + " items...",
                                            QStyles.GetStyle(Style.SlicedButton.Green));
                                }
                                else
                                {
                                    GUI.Box(new Rect(dropBox.x + variableNameWidth.value, dropBox.y, valueFieldWidth, dropBox.height),
                                            "Invalid type",
                                            QStyles.GetStyle(Style.SlicedButton.Red));
                                }
                            }

                            if(Event.current.type == EventType.DragPerform)
                            {
                                if(!dropBox.Contains(Event.current.mousePosition)) return;
                                Event.current.Use();
                                DragAndDrop.AcceptDrag();
                                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                                foreach(var obj in DragAndDrop.objectReferences)
                                {
                                    if(IsObjectValid(obj, v.typeName))
                                    {
                                        v.sp.isExpanded = true;
                                        v.sp.InsertArrayElementAtIndex(v.sp.arraySize);
                                        switch(v.typeName)
                                        {
                                            case "Sprite": v.sp.GetArrayElementAtIndex(v.sp.arraySize - 1).objectReferenceValue = AssetDatabase.LoadAssetAtPath(AssetDatabase.GetAssetPath(obj), typeof(Sprite)); break;
                                            case "Texture": v.sp.GetArrayElementAtIndex(v.sp.arraySize - 1).objectReferenceValue = AssetDatabase.LoadAssetAtPath(AssetDatabase.GetAssetPath(obj), typeof(Texture)); break;
                                            case "Material": v.sp.GetArrayElementAtIndex(v.sp.arraySize - 1).objectReferenceValue = AssetDatabase.LoadAssetAtPath(AssetDatabase.GetAssetPath(obj), typeof(Material)); break;
                                            case "ParticleSystem": GameObject go = (GameObject)obj; v.sp.GetArrayElementAtIndex(v.sp.arraySize - 1).objectReferenceValue = go.GetComponent<ParticleSystem>(); break;
                                            default: v.sp.GetArrayElementAtIndex(v.sp.arraySize - 1).objectReferenceValue = obj; break;
                                        }

                                    }
                                }
                            }
                        }
                    }
                    #endregion
                    dataHeight[categoryName][index] = height;
                };

                AddCategoryReset();
            }
            else
            {
                QUI.DisplayDialog("New category", "There is another category with the name '" + categoryName + "' in the database. Try saving with another name or delete that one and try again.", "Ok");
                return false;
            }
            return true;
        }

        private void DrawSaveAndEncryptButtons(bool addSavebutton, bool addEncryptionbutton, Rect rect, float valueFieldWidth, float buttonSize, Variable v)
        {
            if(addSavebutton)
            {
                if(QUI.ButtonSave(new Rect(rect.x + valueFieldWidth + 4, rect.y, buttonSize, buttonSize), v.saveVariable))
                {
                    v.saveVariable = !v.saveVariable;
                    unsavedChanges.target = true;
                }
            }

            if(addEncryptionbutton)
            {
                if(v.useEncryption && encryptionKeysArray == null)
                {
                    v.useEncryption = false;
                    v.encryptionKey = -1;
                }

                if(v.useEncryption && encryptionKeysArray != null && encryptionKeysArray.Length > 0 && v.encryptionKey > encryptionKeysArray.Length - 1)
                {
                    v.encryptionKey = 0;
                }

                if(v.useEncryption && encryptionKeysArray != null && encryptionKeysArray.Length > 0 && v.encryptionKey == -1)
                {
                    v.encryptionKey = 0;
                }

                if(QUI.ButtonLock(new Rect(rect.x + valueFieldWidth + 4 + buttonSize + 2, rect.y, buttonSize, buttonSize), v.useEncryption))
                {
                    if(encryptionKeysArray == null || encryptionKeysArray.Length == 0)
                    {
                        QUI.DisplayDialog("Action Required", "No encryption keys have been found!" +
                                                                       "\n\n" +
                                                                       "Please go to" +
                                                                       "\n" +
                                                                       "Tools/EZ/Control Panel -> DataManager -> Settings" +
                                                                       "\n" +
                                                                       "And generate at least one key (press the [+] button).", "Ok");
                    }
                    else
                    {
                        v.useEncryption = !v.useEncryption;
                        unsavedChanges.target = true;
                    }
                }

                if(v.useEncryption)
                {
                    QUI.SetGUIBackgroundColor(EditorGUIUtility.isProSkin ? QColors.Blue.Color : QColors.BlueLight.Color);
                    QUI.BeginChangeCheck();
                    {
                        v.encryptionKey = EditorGUI.IntPopup(new Rect(rect.x + valueFieldWidth + 4 + buttonSize + 2 + buttonSize + 2, rect.y, buttonSize * 3, buttonSize), v.encryptionKey, encryptionKeysDisplayOptionArray, encryptionKeysArray);
                    }
                    if(QUI.EndChangeCheck())
                    {
                        unsavedChanges.target = true;
                    }
                    QUI.ResetColors();
                }
            }
        }

        private void AddCategoryReset()
        {
            addCategory.target = false;
            newCategoryName = string.Empty;
        }

        private void DeleteArrayElement(SerializedProperty sp, int index)
        {
            if(sp.GetArrayElementAtIndex(index).propertyType == SerializedPropertyType.ObjectReference &&
                sp.GetArrayElementAtIndex(index).objectReferenceValue != null)
            {
                sp.DeleteArrayElementAtIndex(index);
            }
            sp.DeleteArrayElementAtIndex(index);
            serializedObject.ApplyModifiedProperties();
            QUI.ExitGUI();
        }

        private void InsertArrayElement(SerializedProperty sp, int index, string typeName)
        {
            sp.InsertArrayElementAtIndex(index);
            if(sp.GetArrayElementAtIndex(index).propertyType == SerializedPropertyType.ObjectReference)
            {
                sp.GetArrayElementAtIndex(index).objectReferenceValue = null;
            }
            else
            {
                switch(typeName)
                {
                    case "AnimationCurve": sp.GetArrayElementAtIndex(index).animationCurveValue = new AnimationCurve(); break;
                    case "bool": sp.GetArrayElementAtIndex(index).boolValue = false; break;
                    case "Color": sp.GetArrayElementAtIndex(index).colorValue = new Color(0, 0, 0, 0); break;
                    case "Color32": sp.GetArrayElementAtIndex(index).colorValue = new Color(0, 0, 0, 0); break;
                    case "double": sp.GetArrayElementAtIndex(index).doubleValue = 0; break;
                    case "float": sp.GetArrayElementAtIndex(index).floatValue = 0; break;
                    case "int": sp.GetArrayElementAtIndex(index).intValue = 0; break;
                    case "long": sp.GetArrayElementAtIndex(index).longValue = 0; break;
                    case "Quaternion": sp.GetArrayElementAtIndex(index).quaternionValue = new Quaternion(); break;
                    case "Rect": sp.GetArrayElementAtIndex(index).rectValue = new Rect(); break;
                    case "string": sp.GetArrayElementAtIndex(index).stringValue = string.Empty; break;
                    case "Vector2": sp.GetArrayElementAtIndex(index).vector2Value = Vector2.zero; break;
                    case "Vector3": sp.GetArrayElementAtIndex(index).vector3Value = Vector3.zero; break;
                    case "Vector4": sp.GetArrayElementAtIndex(index).vector4Value = Vector4.zero; break;
                }
            }
            serializedObject.ApplyModifiedProperties();
            QUI.ExitGUI();
        }

        private string GetTypeName(string t)
        {
            if(t.Contains("[]")) t = t.Replace("[]", "");
            else if(t.Contains("System.Collections.Generic.List`1")) { t = t.Replace("System.Collections.Generic.List`1", ""); t = t.Replace("[", ""); t = t.Replace("]", ""); }

            switch(t)
            {
                case "UnityEngine.AnimationCurve": return "AnimationCurve";
                case "UnityEngine.AudioClip": return "AudioClip";
                case "System.Boolean": return "bool";
                case "UnityEngine.Color": return "Color";
                case "UnityEngine.Color32": return "Color32";
                case "System.Double": return "double";
                case "System.Single": return "float";
                case "UnityEngine.GameObject": return "GameObject";
                case "System.Int32": return "int";
                case "System.Int64": return "long";
                case "UnityEngine.Material": return "Material";
                case "UnityEngine.Mesh": return "Mesh";
                case "UnityEngine.Object": return "Object";
                case "UnityEngine.ParticleSystem": return "ParticleSystem";
                case "UnityEngine.Quaternion": return "Quaternion";
                case "UnityEngine.Rect": return "Rect";
                case "UnityEngine.RectTransform": return "RectTransform";
                case "UnityEngine.Sprite": return "Sprite";
                case "System.String": return "string";
                case "UnityEngine.TerrainData": return "TerrainData";
                case "UnityEngine.Transform": return "Transform";
                case "UnityEngine.Texture": return "Texture";
                case "UnityEngine.Vector2": return "Vector2";
                case "UnityEngine.Vector3": return "Vector3";
                case "UnityEngine.Vector4": return "Vector4";
                default: return "Object";
            }
        }

        /// <summary>
        /// Renames a category.  If ok is TRUE it will consider that the user pressed OK, otherwise it will consider that the user pressed Cancel
        /// </summary>
        private bool RenameCategory()
        {
            if(string.IsNullOrEmpty(renameCategoryName))
            {
                QUI.DisplayDialog("Rename Category", "Please enter a category name!", "Ok");
                return false;
            }

            if(data.ContainsKey(renameCategoryName))
            {
                QUI.DisplayDialog("Rename Category", "There is another category with the same name '" + renameCategoryName + "' in the database. Try renaming to another name or delete/rename that one and try again.", "Ok");
                return false;
            }

            foreach(var field in fields)
            {
                if(field.Name.Contains(CATEGORY_START) && serializedObject.FindProperty(field.Name).stringValue.Equals(activeCategoryName))
                {
                    serializedObject.FindProperty(field.Name).stringValue = renameCategoryName;
                }
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            InitializeVariables();
            RenameCategoryReset();
            QUI.ExitGUI();

            return true;
        }

        private void RenameCategoryReset()
        {
            renameCategory.target = false;
            activeCategoryName = string.Empty;
            renameCategoryName = string.Empty;
        }

        private void DeleteCategory(string categoryName)
        {
            rData.Remove(categoryName);
            data.Remove(categoryName);
            dataHeight.Remove(categoryName);
            showCategory.Remove(categoryName);
            unsavedChanges.target = true;
            QUI.ExitGUI();
        }

        private void CloseCategory(string categoryName)
        {
            if(data != null && data[categoryName].Count > 0)
            {
                foreach(var item in data[categoryName])
                {
                    if(item != null && item.sp != null)
                    {
                        item.sp.isExpanded = false;
                    }
                }
            }
        }

        private bool IsObjectValid(UnityEngine.Object obj, string typeName)
        {
            GameObject go = null;
            switch(typeName)
            {
                case "AudioClip": return obj.GetType() == typeof(AudioClip);
                case "GameObject": return obj.GetType() == typeof(GameObject);
                case "Material": return obj.GetType() == typeof(Material);
                case "Mesh": return obj.GetType() == typeof(Mesh);
                case "ParticleSystem": if(obj.GetType() != typeof(GameObject)) { return false; } go = (GameObject)obj; return go != null && go.GetComponent<ParticleSystem>() != null;
                case "RectTransform": if(obj.GetType() != typeof(GameObject)) { return false; } go = (GameObject)obj; return go != null && go.GetComponent<RectTransform>() != null;
                case "Sprite": if(obj.GetType() == typeof(Texture2D)) { TextureImporter ti = TextureImporter.GetAtPath(AssetDatabase.GetAssetPath(obj)) as TextureImporter; return ti.textureType == TextureImporterType.Sprite; } return false;
                case "Texture": if(obj.GetType() == typeof(Texture2D)) { TextureImporter ti = TextureImporter.GetAtPath(AssetDatabase.GetAssetPath(obj)) as TextureImporter; return ti.textureType != TextureImporterType.Sprite; } return false;
                case "TerrainData": return obj.GetType() == typeof(TerrainData);
                case "Transform": if(obj.GetType() != typeof(GameObject)) { return false; } go = (GameObject)obj; return go != null && go.GetComponent<RectTransform>() == null;
                case "Object": return true;
                default: return false;
            }
        }

        private string GetVariableType(string t)
        {
            if(t.Contains("[]")) return "array";
            else if(t.Contains("System.Collections.Generic.List")) return "list";
            else return "variable";
        }

        /// <summary>
        /// Adds a new variable to the active category. If ok is TRUE it will consider that the user pressed OK, otherwise it will consider that the user pressed Cancel
        /// </summary>
        /// <param name="ok">TURE if OK button was pressed. FALSE if Cancel button was pressed</param>
        private bool AddVariable()
        {
            addVariableName = EzDataManagerWriter.CleanString(addVariableName);
            if(string.IsNullOrEmpty(addVariableName))
            {
                QUI.DisplayDialog("New variable", "Please enter a variable name!", "Ok");
                return false;
            }

            if(!IsVariableNameValid(addVariableName))
            {
                QUI.DisplayDialog("New variable", "You cannot add a new variable named: " + addVariableName, "Ok");
                return false;
            }

            if(VariableNameAlreadyExistInTheDatabase(addVariableName))
            {
                QUI.DisplayDialog("New variable", "There is another variable with the name '" + addVariableName + "' in the database. Try saving with another name or delete that one and try again.", "Ok");
                return false;
            }

            data[activeCategoryName].Add(new Variable() { variableType = addVariableVariableType, typeName = addVariableTypeName, name = addVariableName.Trim() });
            dataHeight[activeCategoryName].Add(EditorGUIUtility.singleLineHeight + 2);
            AddVariableReset();
            GUIUtility.ExitGUI();
            return true;
        }

        private bool VariableNameAlreadyExistInTheDatabase(string vName)
        {
            foreach(var field in fields) { if(field.Name.Equals(vName)) return true; }
            foreach(var v in data[activeCategoryName]) { if(v.name.Equals(vName)) return true; }
            return false;
        }

        private bool IsVariableNameValid(string vName) { foreach(var s in invalidVariableNames) { if(s.Equals(vName)) return false; } return true; }

        private void AddVariableReset()
        {
            addVariable.target = false;
            activeCategoryName = string.Empty;
            addVariableVariableType = string.Empty;
            addVariableTypeName = string.Empty;
            addVariableName = string.Empty;
        }

        private void DeleteVariable(string categoryName, int index)
        {
            data[categoryName].RemoveAt(index); dataHeight[categoryName].RemoveAt(index);
            unsavedChanges.target = true;
            QUI.ExitGUI();
        }

        private void DrawVector4(SerializedProperty sp, Rect rect, float width, bool isArrayElement = false)
        {
            Vector4 v4 = sp.vector4Value; int arrayElementAdjustment = isArrayElement ? 2 : 0;
            EditorGUI.LabelField(new Rect(rect.x + width / 4 * 0, rect.y, 13, EditorGUIUtility.singleLineHeight), "X"); v4.x = EditorGUI.FloatField(new Rect(rect.x + width / 4 * 0 + 13, rect.y, width / 4 - 14 - arrayElementAdjustment, EditorGUIUtility.singleLineHeight), v4.x);
            EditorGUI.LabelField(new Rect(rect.x + width / 4 * 1 + 2 - arrayElementAdjustment, rect.y, 16, EditorGUIUtility.singleLineHeight), "Y"); v4.y = EditorGUI.FloatField(new Rect(rect.x + width / 4 * 1 + 14 - arrayElementAdjustment, rect.y, width / 4 - 16 - arrayElementAdjustment, EditorGUIUtility.singleLineHeight), v4.y);
            EditorGUI.LabelField(new Rect(rect.x + width / 4 * 2 + 2 - arrayElementAdjustment, rect.y, 16, EditorGUIUtility.singleLineHeight), "Z"); v4.z = EditorGUI.FloatField(new Rect(rect.x + width / 4 * 2 + 14 - arrayElementAdjustment, rect.y, width / 4 - 16 - arrayElementAdjustment, EditorGUIUtility.singleLineHeight), v4.z);
            EditorGUI.LabelField(new Rect(rect.x + width / 4 * 3 + 2 - arrayElementAdjustment, rect.y, 16, EditorGUIUtility.singleLineHeight), "W"); v4.w = EditorGUI.FloatField(new Rect(rect.x + width / 4 * 3 + 16 - arrayElementAdjustment, rect.y, width / 4 - 13 - arrayElementAdjustment, EditorGUIUtility.singleLineHeight), v4.w);
            sp.vector4Value = v4;
        }

        private void DrawRect(SerializedProperty sp, Rect rect, float width, bool isArrayElement = false)
        {
            Rect rct = sp.rectValue; int arrayElementAdjustment = isArrayElement ? 2 : 0;
            EditorGUI.LabelField(new Rect(rect.x + width / 4 * 0, rect.y, 13, EditorGUIUtility.singleLineHeight), "X"); rct.x = EditorGUI.FloatField(new Rect(rect.x + width / 4 * 0 + 13, rect.y, width / 4 - 14 - arrayElementAdjustment, EditorGUIUtility.singleLineHeight), rct.x);
            EditorGUI.LabelField(new Rect(rect.x + width / 4 * 1 + 2 - arrayElementAdjustment, rect.y, 16, EditorGUIUtility.singleLineHeight), "Y"); rct.y = EditorGUI.FloatField(new Rect(rect.x + width / 4 * 1 + 14 - arrayElementAdjustment, rect.y, width / 4 - 16 - arrayElementAdjustment, EditorGUIUtility.singleLineHeight), rct.y);
            EditorGUI.LabelField(new Rect(rect.x + width / 4 * 2 + 2 - arrayElementAdjustment, rect.y, 16, EditorGUIUtility.singleLineHeight), "W"); rct.width = EditorGUI.FloatField(new Rect(rect.x + width / 4 * 2 + 14 - arrayElementAdjustment, rect.y, width / 4 - 16 - arrayElementAdjustment, EditorGUIUtility.singleLineHeight), rct.width);
            EditorGUI.LabelField(new Rect(rect.x + width / 4 * 3 + 2 - arrayElementAdjustment, rect.y, 16, EditorGUIUtility.singleLineHeight), "H"); rct.height = EditorGUI.FloatField(new Rect(rect.x + width / 4 * 3 + 16 - arrayElementAdjustment, rect.y, width / 4 - 13 - arrayElementAdjustment, EditorGUIUtility.singleLineHeight), rct.height);
            sp.rectValue = rct;
        }

        private void DrawQuaternion(SerializedProperty sp, Rect rect, float width, bool isArrayElement = false)
        {
            Quaternion quat = sp.quaternionValue; int arrayElementAdjustment = isArrayElement ? 2 : 0;
            EditorGUI.LabelField(new Rect(rect.x + width / 4 * 0, rect.y, 13, EditorGUIUtility.singleLineHeight), "X"); quat.x = EditorGUI.FloatField(new Rect(rect.x + width / 4 * 0 + 13, rect.y, width / 4 - 14 - arrayElementAdjustment, EditorGUIUtility.singleLineHeight), quat.x);
            EditorGUI.LabelField(new Rect(rect.x + width / 4 * 1 + 2 - arrayElementAdjustment, rect.y, 16, EditorGUIUtility.singleLineHeight), "Y"); quat.y = EditorGUI.FloatField(new Rect(rect.x + width / 4 * 1 + 14 - arrayElementAdjustment, rect.y, width / 4 - 16 - arrayElementAdjustment, EditorGUIUtility.singleLineHeight), quat.y);
            EditorGUI.LabelField(new Rect(rect.x + width / 4 * 2 + 2 - arrayElementAdjustment, rect.y, 16, EditorGUIUtility.singleLineHeight), "Z"); quat.z = EditorGUI.FloatField(new Rect(rect.x + width / 4 * 2 + 14 - arrayElementAdjustment, rect.y, width / 4 - 16 - arrayElementAdjustment, EditorGUIUtility.singleLineHeight), quat.z);
            EditorGUI.LabelField(new Rect(rect.x + width / 4 * 3 + 2 - arrayElementAdjustment, rect.y, 16, EditorGUIUtility.singleLineHeight), "W"); quat.w = EditorGUI.FloatField(new Rect(rect.x + width / 4 * 3 + 16 - arrayElementAdjustment, rect.y, width / 4 - 13 - arrayElementAdjustment, EditorGUIUtility.singleLineHeight), quat.w);
            sp.quaternionValue = quat;
        }

    }
}
