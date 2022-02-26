using UnityEngine;
using UnityEditor;
using System.IO;
using System.Net;
using System;

namespace PSVitaUtilities.Settings
{
    public class PSVitaUtilitiesSettings : EditorWindow
    {
        static bool Loaded = false;
        static string vitaDBJSON;
        static PSVitaUtilitiesSettings instance;
        static string buildCache = "";

        bool showVitaSettings = true;
        bool showProjectSettings = true;

        #region Settings
        public static string PSVitaIP;
        public static string FTPLocation;
        public static string USBDriveLetter;
        public static string EmuStoragePath;
        public static bool ShowTrialWatermark;
        public static bool VitaBDCheck;
        public static bool KillAllAppsBeforeLaunch;
        public static bool BuildRunReboot;
        public static int RetryCount;
        public static bool SizeCheck;

        Vector2 scrollPos;
        //public PlayerSettings.PSVita.PSVitaPowerMode PowerMode = PlayerSettings.PSVita.PSVitaPowerMode.ModeA;

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
        //public static PlayerSettings.PSVita.PSVitaPowerMode PowerMode
        //{
        //  get { return PlayerSettings.PSVita.powerMode; }
        //  set { PlayerSettings.PSVita.powerMode = value; }
        //}
        #endregion

        #region Creating Editor Window
        [MenuItem("PSVita/Settings")]
        public static void StartWindow()
        {
            buildCache = Application.dataPath.TrimEnd("Assets".ToCharArray()) + "/Build/BuildCache";

            PSVitaUtilitiesSettings window = (PSVitaUtilitiesSettings)GetWindow(typeof(PSVitaUtilitiesSettings));
            GUIContent windowContent = new GUIContent();
            windowContent.text = "PSVita Settings";
            window.titleContent = windowContent;
            window.minSize = new Vector2(450, 350);
            window.Show();

            instance = window;
        }
        #endregion

        void OnGUI()
        {
            #region Initialisation
            if (!Loaded)
            {
                LoadSettings();
                Loaded = true;
            }
            #endregion

            #region Allowing for Bold Foldouts
            GUIStyle boldFoldout = EditorStyles.foldout;
            boldFoldout.fontStyle = FontStyle.Bold;
            #endregion

            #region Drawing Editor Window
            GUILayout.BeginVertical();
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            #region Shilling
            GUILayout.Space(16);
            GUILayout.Label("Please keep in mind that this package makes use of the PSVita Companion Plugin for Vita Control and Build and Run functionality. \nIt can be downloaded through AutoPlugin 2 on the Vita.", EditorStyles.wordWrappedLabel);

            EditorLine();

            if (FlexiButton("Link to PSVita Utilites on GitHub"))
                Application.OpenURL("https://github.com/GlitcherOG/PSVita-Unity-Utilities");
            #endregion

            EditorLine();

            #region Utility Settings
            showVitaSettings = EditorGUILayout.Foldout(showVitaSettings, "Utility Settings", boldFoldout);
            if (showVitaSettings)
            {
                #region Input Text Fields
                EditorGUILayout.LabelField("FTP Settings");
                PSVitaIP = EditorGUILayout.TextField("PSVita IP", PSVitaIP);
                FTPLocation = EditorGUILayout.TextField("FTP Build Location", FTPLocation);
                RetryCount = EditorGUILayout.IntField("FTP Retries", RetryCount);

                //GUILayout.Space(8);
                //EditorGUILayout.LabelField("Emulator Settings");
                //EmuStoragePath = EditorGUILayout.TextField("Emulator Storage Path", EmuStoragePath);

                GUILayout.Space(8);
                EditorGUILayout.LabelField("USB Settings");
                USBDriveLetter = EditorGUILayout.TextField("USB Drive Letter", USBDriveLetter);
                #endregion

                EditorLine();

                #region Setting Bools
                ShowTrialWatermark = EditorGUILayout.ToggleLeft("Enable \"Trial Version\" watermark", ShowTrialWatermark, EditorStyles.wordWrappedLabel);
                VitaBDCheck = EditorGUILayout.ToggleLeft("Check TitleID against the VitaDB", VitaBDCheck, EditorStyles.wordWrappedLabel);
                BuildRunReboot = EditorGUILayout.ToggleLeft("Reboot Vita before starting game", BuildRunReboot, EditorStyles.wordWrappedLabel);
                EditorIndentBlock("This will automatically reboot the vita after Build and Run finishes transferring files. May reduce errors.");
                KillAllAppsBeforeLaunch = EditorGUILayout.ToggleLeft("Kill all apps on Vita before launching game remotely", KillAllAppsBeforeLaunch, EditorStyles.wordWrappedLabel);
                SizeCheck = EditorGUILayout.ToggleLeft("Check Files Before Transfering", SizeCheck, EditorStyles.wordWrappedLabel);
                EditorIndentBlock("This will check if the file being transferring already exists in the Build and Run Cache, and skip them if the match.");
                #endregion

                EditorLine();

                #region ButtonBois
                EditorGUILayout.BeginHorizontal();

                if (FlexiButton("Refresh VitaDB"))
                    DownloadVitaDBCache();
                if (FlexiButton("Delete Build and Run Cache"))
                    Directory.Delete(buildCache, true);

                EditorGUILayout.EndHorizontal();
                #endregion
            }
            #endregion

            EditorLine();

            #region Project Settings
            showProjectSettings = EditorGUILayout.Foldout(showProjectSettings, "Project Settings", boldFoldout);
            if (showProjectSettings)
            {
                DeveloperName = EditorGUILayout.TextField("Developed by", DeveloperName);
                ProductName = EditorGUILayout.TextField("Title", ProductName);
                ShortTitle = EditorGUILayout.TextField("Short Title", ShortTitle);
                TitleID = EditorGUILayout.TextField("TitleID", TitleID);
                //PowerMode = (PlayerSettings.PSVita.PSVitaPowerMode)EditorGUILayout.EnumPopup("Power Mode", PowerMode);
            }
            #endregion

            EditorLine();

            if (FlexiButton("Save"))
                SaveSettings();

            GUILayout.Space(16); // This last space is to just have the Save button not be touching the bottom of the window

            GUILayout.EndVertical();
            EditorGUILayout.EndScrollView();
            #endregion
        }

        #region Saving and Loading
        public static void LoadSettings()
        {
            PSVitaIP = EditorPrefs.GetString("PSVitaIP", "0.0.0.0");
            ShowTrialWatermark = EditorPrefs.GetBool("ShowTrialWatermark", false);
            FTPLocation = EditorPrefs.GetString("FTPLocation", "ux0:/");
            USBDriveLetter = EditorPrefs.GetString("USBDriveLetter", "H:/");
            VitaBDCheck = EditorPrefs.GetBool("VitaDBCheck", true);
            RetryCount = EditorPrefs.GetInt("Retying", 3);
            KillAllAppsBeforeLaunch = EditorPrefs.GetBool("KillAllAppsBeforeLaunch", true);
            BuildRunReboot = EditorPrefs.GetBool("BuildRunReboot", false);
            SizeCheck = EditorPrefs.GetBool("SizeCheck", true);
            EmuStoragePath = EditorPrefs.GetString("EmuStoragePath", Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)+"\\Vita3K\\Vita3K");
            //PowerMode = (PlayerSettings.PSVita.PSVitaPowerMode)EditorPrefs.GetInt("PowerMode", 1);
        }
        public static void SaveSettings()
        {
            EditorPrefs.SetString("PSVitaIP", PSVitaIP);
            EditorPrefs.SetBool("ShowTrialWatermark", ShowTrialWatermark);
            if (FTPLocation == "")
                FTPLocation = "ux0:/";
            EditorPrefs.SetString("FTPLocation", FTPLocation);
            if (USBDriveLetter == "")
                USBDriveLetter = "H:/";
            EditorPrefs.SetString("USBDriveLetter", USBDriveLetter);
            EditorPrefs.SetBool("VitaDBCheck", VitaBDCheck);
            EditorPrefs.SetInt("Retying", RetryCount);
            if (TitleID.Length != 9)
            {
                Debug.LogError("Title ID is recomended to be 9 characters long with 4 letters and 5 numbers (E.g. ABCD12345)");
                EditorApplication.Beep();
            }
            EditorPrefs.SetBool("KillAllAppsBeforeLaunch", KillAllAppsBeforeLaunch);
            EditorPrefs.SetBool("BuildRunReboot", BuildRunReboot);
            EditorPrefs.SetBool("SizeCheck", SizeCheck);
            //EditorPrefs.SetInt("PowerMode", (int)PowerMode);
            if (EmuStoragePath == "")
                EmuStoragePath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\Vita3K\\Vita3K";
            EditorPrefs.SetString("EmuStoragePath", EmuStoragePath);
            Debug.Log("Settings Saved");

            CheckValidTitleID();
            if (instance != null)
                instance.ShowNotification(new GUIContent("Saved"));
        }
        #endregion

        public static bool CheckValidTitleID(bool skipVitaDBCheck = false)
        {
            if (TitleID == "" || TitleID.Length != 9) return false;

            if (TitleID == "ABCD12345")
            {
                if (!EditorUtility.DisplayDialog("TitleID", "This project is using the default Title ID and may override any other application using that Title ID.\nDo you want to continue?", "Yes", "No"))
                    return false;
            }
            if (!skipVitaDBCheck && VitaBDCheck)
            {
                bool idFound = false; // Needed in case multiple of the same ID exists on VitaDB. This is actually the case for "ABCD12345", but we have our own check for that above.

                string json = File.ReadAllText(Application.dataPath + "/Editor/vitadb.txt");

                VitaDBItem[] vitaDBItems = VitaDBItem.LoadVitaDBTitleIDs(json);

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
        public void DownloadVitaDBCache()
        {
            Debug.Log("Downloading VitaDB Cache...");
            WebClient client = new WebClient();
            client.Headers.Add("user-agent", "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705;)");
            client.DownloadFile("https://rinnegatamante.it/vitadb/list_hbs_json.php", Application.dataPath + "/Editor/vitadb.txt");
            Debug.Log("Downloaded");
        }
        #endregion

        #region Bane's Handy Editor Functions
        void EditorIndentBlock(string text, int indentLevel = 1)
        {
            EditorGUI.indentLevel = indentLevel;
            EditorGUILayout.LabelField(text, EditorStyles.wordWrappedLabel);
            EditorGUI.indentLevel = 0;
        }
        void EditorLine() => EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        bool FlexiButton(string label, GUILayoutOption[] options = null)
        {
            #region Default Options
            GUILayoutOption[] defaultOptions = { GUILayout.MinWidth(2), GUILayout.MaxWidth(260), GUILayout.MinHeight(16), GUILayout.ExpandWidth(true) };
            if (options == null)
                options = defaultOptions;
            #endregion

            bool clicked = false;
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(label, options))
                clicked = true;
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            return clicked;
        }
        #endregion
    }

    #region Helper Classes
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
    #endregion
}