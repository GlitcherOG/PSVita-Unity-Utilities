using UnityEngine;
using UnityEditor;
using System.IO;
using System.Net;

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
      #region Drawing Editor Window
      GUILayout.BeginVertical();
      scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

      #region Shilling
      GUILayout.Label("Please keep in mind that this package makes use of the PSVita Companion Plugin and is Required for Vita Control Functionality. It can be downloaded through AutoPlugin 2 on the Vita.", EditorStyles.wordWrappedLabel);

      GUILayout.Space(16);

      if (GUILayout.Button("Link to PSVita Utilites on GitHub"))
        Application.OpenURL("https://github.com/GlitcherOG/PSVita-Unity-Utilities");

      GUILayout.Space(16);
      #endregion

      #region Allowing for Bold Foldouts
      GUIStyle boldFoldout = EditorStyles.foldout;
      FontStyle previousStyle = boldFoldout.fontStyle;
      boldFoldout.fontStyle = FontStyle.Bold;
      #endregion

      showVitaSettings = EditorGUILayout.Foldout(showVitaSettings, "Utility Settings", boldFoldout);

      if (showVitaSettings)
      {
        PSVitaIP = EditorGUILayout.TextField("PSVita IP", PSVitaIP);
        FTPLocation = EditorGUILayout.TextField("FTP Build Location", FTPLocation);
        RetryCount = EditorGUILayout.IntField("FTP Retries", RetryCount);

        GUILayout.Space(16);

        ShowTrialWatermark = EditorGUILayout.ToggleLeft("Enable \"Trial Version\" watermark", ShowTrialWatermark, EditorStyles.wordWrappedLabel);
        VitaBDCheck = EditorGUILayout.ToggleLeft("Check TitleID against the VitaDB", VitaBDCheck, EditorStyles.wordWrappedLabel);
        BuildRunReboot = EditorGUILayout.ToggleLeft("Reboot Vita before starting game", BuildRunReboot, EditorStyles.wordWrappedLabel);
        EditorIndentBlock("This will automatically reboot the vita after Build and Run finishes transferring files. May reduce errors.");
        KillAllAppsBeforeLaunch = EditorGUILayout.ToggleLeft("Kill all apps on Vita before launching game remotely", KillAllAppsBeforeLaunch, EditorStyles.wordWrappedLabel);
        SizeCheck = EditorGUILayout.ToggleLeft("Check Files Before Transfering", SizeCheck, EditorStyles.wordWrappedLabel);
        EditorIndentBlock("This will check if the file being transferring already exists in the Build and Run Cache, and skip them if the match.");

        GUILayout.Space(16);

        if (GUILayout.Button("Refresh VitaDB"))
          DownloadVitaDBCache();

        if (GUILayout.Button("Delete Build and Run Cache"))
        {
          if (Directory.Exists(buildCache))
            Directory.Delete(buildCache, true);
        }

        GUILayout.Space(16);
      }

      showProjectSettings = EditorGUILayout.Foldout(showProjectSettings, "Project Settings", boldFoldout);

      if (showProjectSettings)
      {
        DeveloperName = EditorGUILayout.TextField("Developed by", DeveloperName);
        ProductName = EditorGUILayout.TextField("Title", ProductName);
        ShortTitle = EditorGUILayout.TextField("Short Title", ShortTitle);
        TitleID = EditorGUILayout.TextField("TitleID", TitleID);
        //PowerMode = (PlayerSettings.PSVita.PSVitaPowerMode)EditorGUILayout.EnumPopup("Power Mode", PowerMode);
      }

      GUILayout.Space(16);

      if (GUILayout.Button("Save"))
        SaveSettings();

      GUILayout.Space(16);

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
      VitaBDCheck = EditorPrefs.GetBool("VitaDBCheck", true);
      RetryCount = EditorPrefs.GetInt("Retying", 3);
      KillAllAppsBeforeLaunch = EditorPrefs.GetBool("KillAllAppsBeforeLaunch", true);
      BuildRunReboot = EditorPrefs.GetBool("BuildRunReboot", false);
      SizeCheck = EditorPrefs.GetBool("SizeCheck", true);
      //PowerMode = (PlayerSettings.PSVita.PSVitaPowerMode)EditorPrefs.GetInt("PowerMode", 1);
    }
    public static void SaveSettings()
    {
      EditorPrefs.SetString("PSVitaIP", PSVitaIP);
      EditorPrefs.SetBool("ShowTrialWatermark", ShowTrialWatermark);
      if (FTPLocation == "")
      {
        FTPLocation = "ux0:/";
      }
      EditorPrefs.SetString("FTPLocation", FTPLocation);
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

      Debug.Log("Settings Saved");

      CheckValidTitleID();
      if (instance != null)
      {
        instance.ShowNotification(new GUIContent("Saved"));
      }
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

    void EditorIndentBlock(string text, int indentLevel = 1)
    {
      EditorGUI.indentLevel = indentLevel;
      EditorGUILayout.LabelField(text, EditorStyles.wordWrappedLabel);
      EditorGUI.indentLevel = 0;
    }

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