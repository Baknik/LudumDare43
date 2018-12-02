// Copyright (c) 2016 - 2018 Ez Entertainment SRL. All Rights Reserved.
// This code can only be used under the standard Unity Asset Store End User License Agreement
// A Copy of the EULA APPENDIX 1 is available at http://unity3d.com/company/legal/as_terms

using System.Collections.Generic;
using UnityEngine;

namespace Ez.DataManager.Internal
{
    public class EzEncryptionKeys : ScriptableObject
    {
        [System.Serializable]
        public class DataHolder
        {
            [SerializeField]
            private byte[] _data;
            public byte[] Data { get { return _data; } }
            public DataHolder(byte[] newData) { _data = newData; }
        }

        [SerializeField]
        public char stringDelimiter = '|';
        [SerializeField]
        public List<DataHolder> keys = new List<DataHolder>();
        [SerializeField]
        public List<DataHolder> ivs = new List<DataHolder>();


        public void AddNewKeyAndIV(byte[] newKey, byte[] newIV)
        {
            if(newKey == null || newKey.Length != 32) { throw new System.ArgumentException("New key is null or has illegal length!"); }
            if(newIV == null || newIV.Length != 16) { throw new System.ArgumentException("New IV is null or has illegal length!"); }
            keys.Add(new DataHolder(newKey));
            ivs.Add(new DataHolder(newIV));
        }

        public bool ValidateKeyAndIV(byte[] newKey, byte[] newIV)
        {
            if(newKey == null || newKey.Length != 32) { return false; }
            if(newIV == null || newIV.Length != 16) { return false; }
            return true;
        }

        public void RemoveKeyAndIVAtIndex(int index)
        {
            keys.RemoveAt(index);
            ivs.RemoveAt(index);
        }

        public int GetKeyCount()
        {
            if(keys.Count != ivs.Count) { throw new System.Exception("The key and IV lists have different lengths!"); }
            return keys.Count;
        }

        public void ClearEncryptionKeysAndIVs()
        {
            keys.Clear();
            ivs.Clear();
        }

        public byte[] GetKey(int keyIndex = 0)
        {
            return keys[keyIndex].Data;
        }


        public byte[] GetIV(int ivIndex = 0)
        {
            return ivs[ivIndex].Data;
        }
    }
}