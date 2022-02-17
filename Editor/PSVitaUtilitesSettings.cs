using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Networking;
using Unity.EditorCoroutines.Editor;

namespace PSVitaUtilities.Settings
{
    public class PSVitaUtilitiesSettings : EditorWindow
    {
        static bool Loaded = false;
        static string vitaDBJSON;

        #region Settings
        public static string PSVitaIP;
        public static string FTPLocation;
        public static bool FastBuild;
        public static bool VitaBDCheck;
        public static string DeveloperName
        {
            get { return PlayerSettings.companyName; }
            set { PlayerSettings.companyName = value; }
        }
        public static string ProductName
        {
            get { return PlayerSettings.productName; }
            set { PlayerSettings.productName = value; }
        }
        public static string ShortTitle
        {
            get { return PlayerSettings.PSVita.shortTitle; }
            set { PlayerSettings.PSVita.shortTitle = value; }
        }
        public static string TitleID
        {
            get
            {
                string[] temp = PlayerSettings.PSVita.contentID.Split('-', '_');
                return temp[1];
            }

            set
            {
                string[] temp = PlayerSettings.PSVita.contentID.Split('-', '_');
                PlayerSettings.PSVita.contentID = temp[0] + "-" + value + "_" + temp[2] + "-" + temp[3];
            }
        }
        #endregion

        [MenuItem("PSVita/Settings")]
        public static void StartWindow()
        {
            PSVitaUtilitiesSettings window = (PSVitaUtilitiesSettings)GetWindow(typeof(PSVitaUtilitiesSettings));
            GUIContent windowContent = new GUIContent();
            windowContent.text = "PSVita Settings";
            window.titleContent = windowContent;
            window.minSize = new Vector2(450, 300);
            window.Show();
        }
        
        void OnGUI()
        {
            if (!Loaded)
            {
                LoadSettings();
                LoadVitaDB();
                Loaded = true;
            }

            GUILayout.Label("Utilty Settings", EditorStyles.boldLabel);
            GUILayout.Label("Please keep in mind that this package makes use of the PSVita Companion Plugin and is Required for Vita Control Functionality. It can be downloaded through AutoPlugin 2 on the vita.", EditorStyles.wordWrappedLabel);
            if (GUILayout.Button("Link to PSVita Utilites on GitHub"))
            {
                Application.OpenURL("https://github.com/GlitcherOG/PSVita-Unity-Utilities");
            }

            GUILayout.Space(16);

            PSVitaIP = EditorGUILayout.TextField("PSVita IP", PSVitaIP);
            FTPLocation = EditorGUILayout.TextField("FTP Build Location", FTPLocation);
            FastBuild = EditorGUILayout.ToggleLeft("Fast Development Building (trial version will show in the bottom corner)", FastBuild, EditorStyles.wordWrappedLabel);
            VitaBDCheck = EditorGUILayout.ToggleLeft("Check TitleID against the VitaDB", VitaBDCheck, EditorStyles.wordWrappedLabel);
            if (GUILayout.Button("Refresh VitaDB"))
            {
                LoadVitaDB();
            }

            GUILayout.Space(16);

            GUILayout.Label("Project Settings", EditorStyles.boldLabel);
            DeveloperName = EditorGUILayout.TextField("Developed by", DeveloperName);
            ProductName = EditorGUILayout.TextField("Title", ProductName);
            ShortTitle = EditorGUILayout.TextField("Short Title", ShortTitle);
            TitleID = EditorGUILayout.TextField("TitleID", TitleID);
            if (GUILayout.Button("Save"))
            {
                SaveSettings();
            }
        }
        #region Saving and Loading
        public static void LoadSettings()
        {
            PSVitaIP = EditorPrefs.GetString("PSVitaIP", "0.0.0.0");
            FastBuild = EditorPrefs.GetBool("FastBuild", false);
            FTPLocation = EditorPrefs.GetString("FTPLocation", "ux0:/");
            VitaBDCheck = EditorPrefs.GetBool("VitaDBCheck", true);
        }
        public static void SaveSettings()
        {
            EditorPrefs.SetString("PSVitaIP", PSVitaIP);
            EditorPrefs.SetBool("FastBuild", FastBuild);
            if (FTPLocation == "")
            {
                FTPLocation = "ux0:/";
            }
            EditorPrefs.SetString("FTPLocation", FTPLocation);
            EditorPrefs.SetBool("VitaDBCheck",VitaBDCheck);
            if (TitleID.Length != 9)
            {
                Debug.LogError("Title ID is recomended to be 9 characters long with 4 letters and 5 numbers (E.g. ABCD12345)");
                EditorApplication.Beep();
            }
            Debug.Log("Settings Saved");
        }
        #endregion

        public static bool CheckValidTitleID()
        {
            if (TitleID == "" || TitleID.Length != 9) return false;

            if (TitleID == "ABCD12345")
            {
                if (!EditorUtility.DisplayDialog("TitleID", "This project is using the default Title ID and may override any other application using that Title ID.\nDo you want to continue?", "Yes", "No"))
                    return false;
            }
            if (VitaBDCheck)
            {
                bool idFound = false; // Needed in case multiple of the same ID exists on VitaDB. This is actually the case for "ABCD12345", but we have our own check for that above.
                VitaDBItem[] vitaDBItems = VitaDBItem.LoadVitaDBTitleIDs(vitaDBJSON);

                foreach (VitaDBItem item in vitaDBItems)
                {
                    if (item.titleid == TitleID && idFound == false)
                    {
                        idFound = true;
                        if (!EditorUtility.DisplayDialog("Title ID", "The Title ID you have entered appears to already exist on VitaDB. Using this ID may cause issues in the future in case you install those applications.\nDo you want to continue with this Title ID?", "Yes", "No"))
                            return false;
                    }
                }
            }
            return true;
        }
        #region Loading TitleIDs from VitaDB
        public void LoadVitaDB()
        {
            EditorCoroutineUtility.StartCoroutine(GetJSON("https://rinnegatamante.it/vitadb/list_hbs_json.php"), this);
        }

        static protected IEnumerator GetJSON(string url)
        {
            using (UnityWebRequest www = UnityWebRequest.Get(url))
            {
                yield return www.SendWebRequest();

                if (www.isNetworkError || www.isHttpError)
                    Debug.LogError(www.error);
                else
                    vitaDBJSON = www.downloadHandler.text;
            }
        }
        #endregion
    }



    [System.Serializable]
    public class VitaDBItem
    {
        public string titleid;

        public static VitaDBItem[] LoadVitaDBTitleIDs(string jsonString) => JsonHelper.FromJson<VitaDBItem>(jsonString);
    }

    /// <summary>
    /// Json Helper tools, taken from https://stackoverflow.com/a/36244111
    /// </summary>
    public static class JsonHelper
    {
        public static T[] FromJson<T>(string json)
        {
            Wrapper<T> wrapper = JsonUtility.FromJson<Wrapper<T>>(FixJson(json));
            return wrapper.Items;
        }
#pragma warning disable 0649
        [System.Serializable]
        private class Wrapper<T>
        {
            public T[] Items;
        }
#pragma warning restore 0649
        /// <summary>
        /// Formats JSON in a way that makes the FromJson thing above work
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        static string FixJson(string value) => "{\"Items\":" + value + "}";
    }
}
