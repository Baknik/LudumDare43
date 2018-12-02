// Copyright (c) 2016 - 2017 Ez Entertainment SRL. All Rights Reserved.
// This code can only be used under the standard Unity Asset Store End User License Agreement
// A Copy of the EULA APPENDIX 1 is available at http://unity3d.com/company/legal/as_terms

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ez.DataManager
{
    [Serializable]
    public class EzDataManagerSettings : ScriptableObject
    {
        public bool showUnityPlayerPrefs = false;

        public int variableNameWidth = 120;

        public List<VariableData> variableData = new List<VariableData>();
    }

    [Serializable]
    public class VariableData
    {
        public string name = string.Empty;
        public bool saveVariable = false;
        public bool useEncryption = false;
        public int encryptionKey = -1;

        public VariableData(string Name)
        {
            name = Name;
        }

        public VariableData(string Name, bool SaveVariable, bool UseEncryption, int EncryptionKey)
        {
            name = Name;
            saveVariable = SaveVariable;
            useEncryption = UseEncryption;
            encryptionKey = EncryptionKey;
        }

        public bool Equals(VariableData other)
        {
            return name.Equals(other.name);
        }
    }
}
