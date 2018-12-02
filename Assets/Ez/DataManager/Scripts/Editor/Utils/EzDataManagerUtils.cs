// Copyright (c) 2016 - 2018 Ez Entertainment SRL. All Rights Reserved.
// This code can only be used under the standard Unity Asset Store End User License Agreement
// A Copy of the EULA APPENDIX 1 is available at http://unity3d.com/company/legal/as_terms

using Ez.DataManager.Internal;
using QuickEditor;
using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Ez.DataManager
{
    public class EzDataManagerUtils
    {
        /// <summary>
        /// Exports the key/iv pairs in the EzEncryptionKeys ScriptableObject to a CSV file.
        /// </summary>
        /// <param name="ezEncryptionKeys">Reference to the EzEncryptionKeys ScriptableObject</param>
        public static void BackupEncryptionKeysToCSV(EzEncryptionKeys ezEncryptionKeys)
        {
            string[] tempStringArray = new string[ezEncryptionKeys.GetKeyCount()];
            for(int i = 0; i < tempStringArray.Length; i++)
            {
                tempStringArray[i] = Convert.ToBase64String(ASCIIEncoding.UTF8.GetBytes(
                                Convert.ToBase64String(ezEncryptionKeys.GetKey(i)) +
                                "," +
                                Convert.ToBase64String(ezEncryptionKeys.GetIV(i))));
            }
            if(!AssetDatabase.IsValidFolder("Assets/Ez/DataManager/Editor/KeysBackup")) { AssetDatabase.CreateFolder(EZT.PATH + "/DataManager/Editor", "KeysBackup"); }
            string path = EditorUtility.SaveFilePanelInProject("Backup to CSV", Application.productName + "_EzEncryptionKeysBackup", "csv", "Backup AES Encryption Data", EZT.PATH + "/DataManager/Editor/KeysBackup/");
            File.WriteAllLines(path, tempStringArray, Encoding.UTF8);
            AssetDatabase.Refresh();
            EditorUtility.RevealInFinder(path);
        }

        /// <summary>
        /// Imports a new AES key/iv list from the selected CSV file. Validates the import and returns true if successful, false otherwise.
        /// </summary>
        /// <param name="ezEncryptionKeys">Reference to the EzEncryptionKeys ScriptableObject</param>
        /// <returns>Returns true if successful, false otherwise.</returns>
        public static bool RestoreEncryptionKeysFromCSV(EzEncryptionKeys ezEncryptionKeys)
        {
            try
            {
                string path = EditorUtility.OpenFilePanel("Restore from CSV", EZT.PATH + "/DataManager/Editor/KeysBackup/", "csv");
                string[] tempStringArray = File.ReadAllLines(path, Encoding.UTF8);
                string[] tempStrSplit;
                if(tempStringArray.Length == 0) { return false; }
                // Parse the import data once to validate it
                for(int i = 0; i < tempStringArray.Length; i++)
                {
                    tempStrSplit = ASCIIEncoding.UTF8.GetString(Convert.FromBase64String(tempStringArray[i])).Split(',');
                    if(!ezEncryptionKeys.ValidateKeyAndIV(Convert.FromBase64String(tempStrSplit[0]), Convert.FromBase64String(tempStrSplit[1]))) { return false; }
                }
                // Import validated, overwrite existing keys
                ezEncryptionKeys.ClearEncryptionKeysAndIVs();
                for(int i = 0; i < tempStringArray.Length; i++)
                {
                    tempStrSplit = ASCIIEncoding.UTF8.GetString(Convert.FromBase64String(tempStringArray[i])).Split(',');
                    ezEncryptionKeys.AddNewKeyAndIV(Convert.FromBase64String(tempStrSplit[0]), Convert.FromBase64String(tempStrSplit[1]));
                }
                QUI.SetDirty(ezEncryptionKeys);
                AssetDatabase.SaveAssets();
            }
            catch(Exception)
            {
                return false;
            }
            return true;
        }
    }
}
