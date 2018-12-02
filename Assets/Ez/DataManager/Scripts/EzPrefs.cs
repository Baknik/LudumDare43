// Copyright (c) 2016 - 2018 Ez Entertainment SRL. All Rights Reserved.
// This code can only be used under the standard Unity Asset Store End User License Agreement
// A Copy of the EULA APPENDIX 1 is available at http://unity3d.com/company/legal/as_terms

using System;
using System.Collections.Generic;
using System.Linq;
using Ez.DataManager.Internal;
using QuickEngine.Extensions;
using QuickEngine.Utils;
using UnityEngine;
using QuickEngine.Core;

namespace Ez.DataManager.Prefs
{
    public static class EzPrefs
    {
        private static EzEncryptionKeys _keyHolder;
        private static EzEncryptionKeys KeyHolder
        {
            get
            {
                if(_keyHolder == null)
                {
                    _keyHolder = Q.GetResource<EzEncryptionKeys>(EZT.RESOURCES_PATH_DATA_MANAGER_KEYS, "EzEncryptionKeys");
                }
                return _keyHolder;
            }
        }

        private static char _stringDelimiter = ' ';
        private static char StringDelimiter
        {
            get
            {
                if(_stringDelimiter == ' ')
                {
                    _stringDelimiter = KeyHolder.stringDelimiter;
                }
                return _stringDelimiter;
            }
        }

        #region ByteArray Extension
        public static void Save(this byte[] byteArray, string prefsKey)
        {
            PlayerPrefs.SetString(prefsKey, byteArray.ToString(StringDelimiter));
        }
        #endregion

        #region Float Extensions
        public static void Save(this float value, string prefsKey)
        {
            PlayerPrefs.SetFloat(prefsKey, value);
        }

        public static byte[] Encrypt(this float value, int keyIndex = 0)
        {
            return QEncryption.EncryptString(value.ToString(), KeyHolder.GetKey(keyIndex), KeyHolder.GetIV(keyIndex));
        }

        public static float DecryptFloat(this string value, int keyIndex = 0)
        {
            return float.Parse(QEncryption.DecryptBytes(Array.ConvertAll(value.Split(StringDelimiter), byte.Parse), KeyHolder.GetKey(keyIndex), KeyHolder.GetIV(keyIndex)));
        }

        public static float DecryptFloat(this byte[] byteArray, int keyIndex = 0)
        {
            return float.Parse(QEncryption.DecryptBytes(byteArray, KeyHolder.GetKey(keyIndex), KeyHolder.GetIV(keyIndex)));
        }

        public static float Load(this float defaultFloat, string prefsKey, bool encrypted = false, int keyIndex = 0)
        {
            try
            {
                if(encrypted) { return PlayerPrefs.GetString(prefsKey).DecryptFloat(keyIndex); }
                else { return PlayerPrefs.GetFloat(prefsKey, defaultFloat); }
            }
            catch(Exception) { return defaultFloat; }
        }
        #endregion

        #region Int Extensions
        public static void Save(this int value, string prefsKey)
        {
            PlayerPrefs.SetInt(prefsKey, value);
        }

        public static byte[] Encrypt(this int value, int keyIndex = 0)
        {
            return QEncryption.EncryptString(value.ToString(), KeyHolder.GetKey(keyIndex), KeyHolder.GetIV(keyIndex));
        }

        public static int DecryptInt(this string value, int keyIndex = 0)
        {
            return int.Parse(QEncryption.DecryptBytes(Array.ConvertAll(value.Split(StringDelimiter), byte.Parse), KeyHolder.GetKey(keyIndex), KeyHolder.GetIV(keyIndex)));
        }

        public static int DecryptInt(this byte[] byteArray, int keyIndex = 0)
        {
            return int.Parse(QEncryption.DecryptBytes(byteArray, KeyHolder.GetKey(keyIndex), KeyHolder.GetIV(keyIndex)));
        }

        public static int Load(this int defaultInt, string prefsKey, bool encrypted = false, int keyIndex = 0)
        {
            try
            {
                if(encrypted) { return PlayerPrefs.GetString(prefsKey).DecryptInt(keyIndex); }
                else { return PlayerPrefs.GetInt(prefsKey, defaultInt); }
            }
            catch(Exception) { return defaultInt; }
        }
        #endregion

        #region Bool Extensions
        public static void Save(this bool value, string prefsKey)
        {
            PlayerPrefs.SetString(prefsKey, value.ToString());
        }

        public static byte[] Encrypt(this bool value, int keyIndex = 0)
        {
            return QEncryption.EncryptString(value.ToString(), KeyHolder.GetKey(keyIndex), KeyHolder.GetIV(keyIndex));
        }

        public static bool ToBool(this string value)
        {
            if(string.IsNullOrEmpty(value)) { throw new ArgumentNullException(); }
            return bool.Parse(value);
        }

        public static bool DecryptBool(this string value, int keyIndex = 0)
        {
            return bool.Parse(QEncryption.DecryptBytes(Array.ConvertAll(value.Split(StringDelimiter), byte.Parse), KeyHolder.GetKey(keyIndex), KeyHolder.GetIV(keyIndex)));
        }

        public static bool DecryptBool(this byte[] byteArray, int keyIndex = 0)
        {
            return bool.Parse(QEncryption.DecryptBytes(byteArray, KeyHolder.GetKey(keyIndex), KeyHolder.GetIV(keyIndex)));
        }

        public static bool Load(this bool defaultBool, string prefsKey, bool encrypted = false, int keyIndex = 0)
        {
            try
            {
                if(encrypted) { return PlayerPrefs.GetString(prefsKey).DecryptBool(keyIndex); }
                else { return PlayerPrefs.GetString(prefsKey, null).ToBool(); }
            }
            catch(Exception) { return defaultBool; }
        }
        #endregion

        #region String Extensions
        public static void Save(this string value, string prefsKey)
        {
            PlayerPrefs.SetString(prefsKey, value);
        }

        public static byte[] Encrypt(this string value, int keyIndex = 0)
        {
            return QEncryption.EncryptString(value, KeyHolder.GetKey(keyIndex), KeyHolder.GetIV(keyIndex));
        }

        public static string DecryptString(this string value, int keyIndex = 0)
        {
            return QEncryption.DecryptBytes(Array.ConvertAll(value.Split(StringDelimiter), byte.Parse), KeyHolder.GetKey(keyIndex), KeyHolder.GetIV(keyIndex));
        }

        public static string DecryptString(this byte[] byteArray, int keyIndex = 0)
        {
            return QEncryption.DecryptBytes(byteArray, KeyHolder.GetKey(keyIndex), KeyHolder.GetIV(keyIndex));
        }

        public static string Load(this string defaultString, string prefsKey, bool encrypted = false, int keyIndex = 0)
        {
            try
            {
                if(encrypted) { return PlayerPrefs.GetString(prefsKey).DecryptString(keyIndex); }
                else { return PlayerPrefs.GetString(prefsKey, defaultString); }
            }
            catch(Exception) { return defaultString; }
        }
        #endregion

        #region FloatArray Extensions
        public static void Save(this float[] floatArray, string prefsKey)
        {
            PlayerPrefs.SetString(prefsKey, floatArray.ToString(StringDelimiter));
        }

        public static byte[] Encrypt(this float[] floatArray, int keyIndex = 0)
        {
            return QEncryption.EncryptString(floatArray.ToString(StringDelimiter), KeyHolder.GetKey(keyIndex), KeyHolder.GetIV(keyIndex));
        }

        public static float[] ToFloatArray(this string value)
        {
            if(string.IsNullOrEmpty(value)) { throw new ArgumentNullException(); }
            return Array.ConvertAll(value.Split(StringDelimiter), float.Parse);
        }

        public static float[] DecryptFloatArray(this string value, int keyIndex = 0)
        {
            return Array.ConvertAll(QEncryption.DecryptBytes(Array.ConvertAll(value.Split(StringDelimiter), byte.Parse), KeyHolder.GetKey(keyIndex), KeyHolder.GetIV(keyIndex)).Split(StringDelimiter), float.Parse);
        }

        public static float[] DecryptFloatArray(this byte[] byteArray, int keyIndex = 0)
        {
            return Array.ConvertAll(QEncryption.DecryptBytes(byteArray, KeyHolder.GetKey(keyIndex), KeyHolder.GetIV(keyIndex)).Split(StringDelimiter), float.Parse);
        }

        public static float[] Load(this float[] defaultArray, string prefsKey, bool encrypted = false, int keyIndex = 0)
        {
            try
            {
                if(encrypted) { return PlayerPrefs.GetString(prefsKey).DecryptFloatArray(keyIndex); }
                else { return PlayerPrefs.GetString(prefsKey, null).ToFloatArray(); }
            }
            catch(Exception) { return defaultArray; }
        }
        #endregion

        #region FloatList Extensions
        public static void Save(this List<float> floatList, string prefsKey)
        {
            PlayerPrefs.SetString(prefsKey, floatList.ToString(StringDelimiter));
        }

        public static byte[] Encrypt(this List<float> floatList, int keyIndex = 0)
        {
            return QEncryption.EncryptString(floatList.ToString(StringDelimiter), KeyHolder.GetKey(keyIndex), KeyHolder.GetIV(keyIndex));
        }

        public static List<float> ToFloatList(this string value)
        {
            if(string.IsNullOrEmpty(value)) { throw new ArgumentNullException(); }
            return Array.ConvertAll(value.Split(StringDelimiter), float.Parse).ToList();
        }

        public static List<float> DecryptFloatList(this string value, int keyIndex = 0)
        {
            return Array.ConvertAll(QEncryption.DecryptBytes(Array.ConvertAll(value.Split(StringDelimiter), byte.Parse), KeyHolder.GetKey(keyIndex), KeyHolder.GetIV(keyIndex)).Split(StringDelimiter), float.Parse).ToList();
        }

        public static List<float> DecryptFloatList(this byte[] byteArray, int keyIndex = 0)
        {
            return Array.ConvertAll(QEncryption.DecryptBytes(byteArray, KeyHolder.GetKey(keyIndex), KeyHolder.GetIV(keyIndex)).Split(StringDelimiter), float.Parse).ToList();
        }

        public static List<float> Load(this List<float> defaultList, string prefsKey, bool encrypted = false, int keyIndex = 0)
        {
            try
            {
                if(encrypted) { return PlayerPrefs.GetString(prefsKey).DecryptFloatList(keyIndex); }
                else { return PlayerPrefs.GetString(prefsKey, null).ToFloatList(); }
            }
            catch(Exception) { return defaultList; }
        }
        #endregion

        #region IntArray Extensions
        public static void Save(this int[] intArray, string prefsKey)
        {
            PlayerPrefs.SetString(prefsKey, intArray.ToString(StringDelimiter));
        }

        public static byte[] Encrypt(this int[] intArray, int keyIndex = 0)
        {
            return QEncryption.EncryptString(intArray.ToString(StringDelimiter), KeyHolder.GetKey(keyIndex), KeyHolder.GetIV(keyIndex));
        }

        public static int[] ToIntArray(this string value)
        {
            if(string.IsNullOrEmpty(value)) { throw new ArgumentNullException(); }
            return Array.ConvertAll(value.Split(StringDelimiter), int.Parse);
        }

        public static int[] DecryptIntArray(this string value, int keyIndex = 0)
        {
            return Array.ConvertAll(QEncryption.DecryptBytes(Array.ConvertAll(value.Split(StringDelimiter), byte.Parse), KeyHolder.GetKey(keyIndex), KeyHolder.GetIV(keyIndex)).Split(StringDelimiter), int.Parse);
        }

        public static int[] DecryptIntArray(this byte[] byteArray, int keyIndex = 0)
        {
            return Array.ConvertAll(QEncryption.DecryptBytes(byteArray, KeyHolder.GetKey(keyIndex), KeyHolder.GetIV(keyIndex)).Split(StringDelimiter), int.Parse);
        }

        public static int[] Load(this int[] defaultArray, string prefsKey, bool encrypted = false, int keyIndex = 0)
        {
            try
            {
                if(encrypted) { return PlayerPrefs.GetString(prefsKey).DecryptIntArray(keyIndex); }
                else { return PlayerPrefs.GetString(prefsKey, null).ToIntArray(); }
            }
            catch(Exception) { return defaultArray; }
        }
        #endregion

        #region IntList Extensions
        public static void Save(this List<int> intList, string prefsKey)
        {
            PlayerPrefs.SetString(prefsKey, intList.ToString(StringDelimiter));
        }

        public static byte[] Encrypt(this List<int> intList, int keyIndex = 0)
        {
            return QEncryption.EncryptString(intList.ToString(StringDelimiter), KeyHolder.GetKey(keyIndex), KeyHolder.GetIV(keyIndex));
        }

        public static List<int> ToIntList(this string value)
        {
            if(string.IsNullOrEmpty(value)) { throw new ArgumentNullException(); }
            return Array.ConvertAll(value.Split(StringDelimiter), int.Parse).ToList();
        }

        public static List<int> DecryptIntList(this string value, int keyIndex = 0)
        {
            return Array.ConvertAll(QEncryption.DecryptBytes(Array.ConvertAll(value.Split(StringDelimiter), byte.Parse), KeyHolder.GetKey(keyIndex), KeyHolder.GetIV(keyIndex)).Split(StringDelimiter), int.Parse).ToList();
        }

        public static List<int> DecryptIntList(this byte[] byteArray, int keyIndex = 0)
        {
            return Array.ConvertAll(QEncryption.DecryptBytes(byteArray, KeyHolder.GetKey(keyIndex), KeyHolder.GetIV(keyIndex)).Split(StringDelimiter), int.Parse).ToList();
        }

        public static List<int> Load(this List<int> defaultList, string prefsKey, bool encrypted = false, int keyIndex = 0)
        {
            try
            {
                if(encrypted) { return PlayerPrefs.GetString(prefsKey).DecryptIntList(keyIndex); }
                else { return PlayerPrefs.GetString(prefsKey, null).ToIntList(); }
            }
            catch(Exception) { return defaultList; }
        }
        #endregion

        #region BoolArray Extensions
        public static void Save(this bool[] boolArray, string prefsKey)
        {
            PlayerPrefs.SetString(prefsKey, boolArray.ToString(StringDelimiter));
        }

        public static byte[] Encrypt(this bool[] boolArray, int keyIndex = 0)
        {
            return QEncryption.EncryptString(boolArray.ToString(StringDelimiter), KeyHolder.GetKey(keyIndex), KeyHolder.GetIV(keyIndex));
        }

        public static bool[] ToBoolArray(this string value)
        {
            if(string.IsNullOrEmpty(value)) { throw new ArgumentNullException(); }
            return Array.ConvertAll(value.Split(StringDelimiter), bool.Parse);
        }

        public static bool[] DecryptBoolArray(this string value, int keyIndex = 0)
        {
            return Array.ConvertAll(QEncryption.DecryptBytes(Array.ConvertAll(value.Split(StringDelimiter), byte.Parse), KeyHolder.GetKey(keyIndex), KeyHolder.GetIV(keyIndex)).Split(StringDelimiter), bool.Parse);
        }

        public static bool[] DecryptBoolArray(this byte[] byteArray, int keyIndex = 0)
        {
            return Array.ConvertAll(QEncryption.DecryptBytes(byteArray, KeyHolder.GetKey(keyIndex), KeyHolder.GetIV(keyIndex)).Split(StringDelimiter), bool.Parse);
        }

        public static bool[] Load(this bool[] defaultArray, string prefsKey, bool encrypted = false, int keyIndex = 0)
        {
            try
            {
                if(encrypted) { return PlayerPrefs.GetString(prefsKey).DecryptBoolArray(keyIndex); }
                else { return PlayerPrefs.GetString(prefsKey, null).ToBoolArray(); }
            }
            catch(Exception) { return defaultArray; }
        }
        #endregion

        #region BoolList Extensions
        public static void Save(this List<bool> boolList, string prefsKey)
        {
            PlayerPrefs.SetString(prefsKey, boolList.ToString(StringDelimiter));
        }

        public static byte[] Encrypt(this List<bool> boolList, int keyIndex = 0)
        {
            return QEncryption.EncryptString(boolList.ToString(StringDelimiter), KeyHolder.GetKey(keyIndex), KeyHolder.GetIV(keyIndex));
        }

        public static List<bool> ToBoolList(this string value)
        {
            if(string.IsNullOrEmpty(value)) { throw new ArgumentNullException(); }
            return Array.ConvertAll(value.Split(StringDelimiter), bool.Parse).ToList();
        }

        public static List<bool> DecryptBoolList(this string value, int keyIndex = 0)
        {
            return Array.ConvertAll(QEncryption.DecryptBytes(Array.ConvertAll(value.Split(StringDelimiter), byte.Parse), KeyHolder.GetKey(keyIndex), KeyHolder.GetIV(keyIndex)).Split(StringDelimiter), bool.Parse).ToList();
        }

        public static List<bool> DecryptBoolList(this byte[] byteArray, int keyIndex = 0)
        {
            return Array.ConvertAll(QEncryption.DecryptBytes(byteArray, KeyHolder.GetKey(keyIndex), KeyHolder.GetIV(keyIndex)).Split(StringDelimiter), bool.Parse).ToList();
        }

        public static List<bool> Load(this List<bool> defaultList, string prefsKey, bool encrypted = false, int keyIndex = 0)
        {
            try
            {
                if(encrypted) { return PlayerPrefs.GetString(prefsKey).DecryptBoolList(keyIndex); }
                else { return PlayerPrefs.GetString(prefsKey, null).ToBoolList(); }
            }
            catch(Exception) { return defaultList; }
        }
        #endregion

        #region StringArray Extensions
        public static void Save(this string[] stringArray, string prefsKey)
        {
            PlayerPrefs.SetString(prefsKey, stringArray.ToString(StringDelimiter));
        }

        public static byte[] Encrypt(this string[] stringArray, int keyIndex = 0)
        {
            return QEncryption.EncryptString(stringArray.ToString(StringDelimiter), KeyHolder.GetKey(keyIndex), KeyHolder.GetIV(keyIndex));
        }

        public static string[] ToStringArray(this string value)
        {
            if(string.IsNullOrEmpty(value)) { throw new ArgumentNullException(); }
            return value.Split(StringDelimiter);
        }

        public static string[] DecryptStringArray(this string value, int keyIndex = 0)
        {
            return QEncryption.DecryptBytes(Array.ConvertAll(value.Split(StringDelimiter), byte.Parse), KeyHolder.GetKey(keyIndex), KeyHolder.GetIV(keyIndex)).Split(StringDelimiter);
        }

        public static string[] DecryptStringArray(this byte[] byteArray, int keyIndex = 0)
        {
            return QEncryption.DecryptBytes(byteArray, KeyHolder.GetKey(keyIndex), KeyHolder.GetIV(keyIndex)).Split(StringDelimiter);
        }

        public static string[] Load(this string[] defaultArray, string prefsKey, bool encrypted = false, int keyIndex = 0)
        {
            try
            {
                if(encrypted) { return PlayerPrefs.GetString(prefsKey).DecryptStringArray(keyIndex); }
                else { return PlayerPrefs.GetString(prefsKey, null).ToStringArray(); }
            }
            catch(Exception) { return defaultArray; }
        }
        #endregion

        #region StringList Extensions
        public static void Save(this List<string> stringList, string prefsKey)
        {
            PlayerPrefs.SetString(prefsKey, stringList.ToString(StringDelimiter));
        }

        public static byte[] Encrypt(this List<string> stringList, int keyIndex = 0)
        {
            return QEncryption.EncryptString(stringList.ToString(StringDelimiter), KeyHolder.GetKey(keyIndex), KeyHolder.GetIV(keyIndex));
        }

        public static List<string> ToStringList(this string value)
        {
            if(string.IsNullOrEmpty(value)) { throw new ArgumentNullException(); }
            return value.Split(StringDelimiter).ToList();
        }

        public static List<string> DecryptStringList(this string value, int keyIndex = 0)
        {
            return QEncryption.DecryptBytes(Array.ConvertAll(value.Split(StringDelimiter), byte.Parse), KeyHolder.GetKey(keyIndex), KeyHolder.GetIV(keyIndex)).Split(StringDelimiter).ToList();
        }

        public static List<string> DecryptStringList(this byte[] byteArray, int keyIndex = 0)
        {
            return QEncryption.DecryptBytes(byteArray, KeyHolder.GetKey(keyIndex), KeyHolder.GetIV(keyIndex)).Split(StringDelimiter).ToList();
        }

        public static List<string> Load(this List<String> defaultList, string prefsKey, bool encrypted = false, int keyIndex = 0)
        {
            try
            {
                if(encrypted) { return PlayerPrefs.GetString(prefsKey).DecryptStringList(keyIndex); }
                else { return PlayerPrefs.GetString(prefsKey, null).ToStringList(); }
            }
            catch(Exception) { return defaultList; }
        }
        #endregion
    }
}