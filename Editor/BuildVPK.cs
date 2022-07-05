using UnityEditor;
using UnityEngine;
using System.IO;
using System.IO.Compression;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using PSVitaUtilities.Settings;
using System.Security.Cryptography;

namespace PSVitaUtilities.Building
{
  public class BuildVPK : EditorWindow
  {
    static string buildType = "";
    static string buildCache = Application.dataPath.TrimEnd("Assets".ToCharArray()) + "/Build/BuildCache";

    #region Build Functions
    [MenuItem("PSVita/Build/Build VPK", false, -1)]
    public static void BuildGameNormal() => BuildGame(BuildMode.Normal);

    [MenuItem("PSVita/Build/FTP/Build and Send VPK")]
    public static void BuildGameFTP() => BuildGame(BuildMode.FTP);

    [MenuItem("PSVita/Build/FTP/Transfer Build")]
    public static void BuildFTPTransfer() => BuildGame(BuildMode.FTPTransfer);

    [MenuItem("PSVita/Build/FTP/Build and Run (WIP)")]
    public static void BuildGameRun() => BuildGame(BuildMode.Run);

    [MenuItem("PSVita/Build/USB/Build and Send VPK (WIP)")]
    public static void BuildGameUSB() => BuildGame(BuildMode.USB);

    //[MenuItem("PSVita/Build/USB/Build and Run (WIP)")]
    public static void BuildGameUSBRun() => BuildGame(BuildMode.USBRun);

    //[MenuItem("PSVita/Build/Emulator/Build and Transfer")]
    public static void BuildEmuTransfer() => BuildGame(BuildMode.EmuTransfer);

    public static void BuildGame(BuildMode buildMode)
    {
      DateTime startTime = DateTime.Now;

      string Error = "Project Failed To Build";
      string filePath = "";

      #region Set Build Type for Error Messages
      switch (buildMode)
      {
        case BuildMode.Normal:
          buildType = "VPK Build";
          break;
        case BuildMode.FTP:
          buildType = "FTP VPK Build";
          break;
        case BuildMode.Run:
          buildType = "Build and Run";
          break;
        case BuildMode.USB:
          buildType = "USB VPK Build";
          break;
        case BuildMode.USBRun:
          buildType = "USB Build and Run Build";
          break;
        case BuildMode.EmuTransfer:
          buildType = "Emulator Transfer";
          break;
        case BuildMode.FTPTransfer:
          buildType = "FTP Build Transfer";
          break;
      }
      #endregion

      try
      {
        #region Load Settings
        Error = $"{buildType} Failed (Failed to Load Settings)";
        PSVitaUtilitiesSettings.LoadSettings();
        #endregion

        #region Build and Run check
        if (buildMode == BuildMode.Run)
        {
          if (!EditorUtility.DisplayDialog("Are you sure?", "This is a work in progress function that could potentally crash your vita and crash in Unity 2017. It requires the game being preinstalled on the system at least once", "Continue", "Stop"))
            return;

          EditorUtility.DisplayProgressBar("Building", "Killing All Apps On Vita...", 0f / 6f);
          Error = $"{buildType} Failed (Network Error: Failed to kill all apps)";
          VitaDestroy();
        }
        if (buildMode == BuildMode.USBRun)
        {
          if (!EditorUtility.DisplayDialog("Are you sure?", "This is a work in progress function that could potentally crash your vita and crash in Unity 2017. It requires the game being preinstalled on the system at least once", "Continue", "Stop"))
            return;

          ScreenOn();
          //EditorUtility.DisplayProgressBar("Building", "Killing All Apps On Vita...", 0f / 6f);
          //Error = $"{buildType} Failed (Network Error: Failed to kill all apps)";
          //VitaDestroy();
          //VitaShellLaunch();
          //if (!EditorUtility.DisplayDialog("Have you enabled USB transfer on VitaShell?", "This function requires that the VitaShell USB transfer is enabled and active. Please press continue if you have done so.", "Continue", "Stop"))
          //  return;
        }
        #endregion

        #region TitleID Check
        Error = $"{buildType} Failed (TitleID Check Error: Try turning off the TitleID Check in settings or Refreshing the VitaDB in settings)";
        if (!PSVitaUtilitiesSettings.CheckValidTitleID())
        {
          Debug.LogError("TitleID Error. TitleID can be changed in PlayerSettings or in the PSVita Settings");
          return;
        }
        #endregion

        #region Prepare Filepaths
        EditorUtility.DisplayProgressBar("Building", "Starting process...", 1f / 5f);
        Error = $"{buildType} Failed (Failed to set path)";
        string BuildPath = Application.dataPath.TrimEnd("Assets".ToCharArray()) + "/Build/TempBuild";
        if (buildMode == BuildMode.Normal)
        {
          filePath = EditorUtility.SaveFilePanel("Save VPK file", "", "", "vpk");
          if (filePath == "")
          {
            EditorUtility.ClearProgressBar();
            return;
          }
        }
        else
        {
          filePath = Application.dataPath.TrimEnd("Assets".ToCharArray()) + "/Build/" + PlayerSettings.productName + ".vpk";
        }
        #endregion

        #region Create Build Directory
        Error = $"{buildType} Failed (Failed to Create Directory)";
        Directory.CreateDirectory(BuildPath);
        #endregion

        #region Build Project
        Error = $"{buildType} Failed (Failed to Build)";
        if (buildMode != BuildMode.Run && buildMode != BuildMode.FTPTransfer)
        {
          if (Directory.Exists(buildCache))
            Directory.Delete(buildCache, true);
        }
        EditorUtility.DisplayProgressBar("Building", "Building project...", 2f / 5f);
        BuildPipeline.BuildPlayer(EditorBuildSettings.scenes, BuildPath, BuildTarget.PSP2, BuildOptions.None);
        #endregion

        #region Delete Junk
        EditorUtility.DisplayProgressBar("Building", "Deleting junk...", 3f / 5f);
        Error = $"{buildType} Failed (Failed to Delete Junk)";
        DeleteJunk(BuildPath);
        #endregion

        #region Remove Trial
        Error = $"{buildType} Failed (Failed to edit TempBuild.self)";
        EditorUtility.DisplayProgressBar("Building", "Removing trial...", 4f / 5f);
        RemoveTrial(BuildPath);
        #endregion

        #region Make VPK
        if (buildMode != BuildMode.Run && buildMode != BuildMode.FTPTransfer && buildMode != BuildMode.EmuTransfer)
        {
          Error = $"{buildType} Failed (Failed to zip To VPK)";
          EditorUtility.DisplayProgressBar("Building", "Finalising build...", 5f / 6f);

          MakeZip(BuildPath, filePath);
        }
        #endregion

        DateTime startTransferTime = DateTime.Now;

        #region FTP - Send to Vita
        if (buildMode == BuildMode.FTP)
        {
          Error = $"{buildType} Failed (Network Error: Failed to transfer Build)";
          string productName = PlayerSettings.productName;
          EditorUtility.DisplayProgressBar("Building", "Transfering " + productName + ".vpk", 6f / 7f);
          TransferFTPVPK(filePath, "ftp://" + PSVitaUtilitiesSettings.PSVitaIP + ":1337/" + PSVitaUtilitiesSettings.FTPLocation + "/" + productName + ".vpk");
        }
        #endregion

        #region USB - Send to Vita
        if (buildMode == BuildMode.USB)
        {
          Error = $"{buildType} Failed (Transfer Error: Failed to transfer Build)";
          string productName = PlayerSettings.productName;
          EditorUtility.DisplayProgressBar("Building", "Transfering " + productName + ".vpk", 6f / 7f);
          TransferUSBVPK(filePath, $"{PSVitaUtilitiesSettings.USBDriveLetter}/{productName}.vpk");
        }
        #endregion

        #region FTP - Build and Run - Send to Vita and Run
        if (buildMode == BuildMode.Run || buildMode == BuildMode.FTPTransfer)
        {
          #region Send to Vita
          Error = $"{buildType} Failed (Network Error: Failed to Transfer Build)";
          EditorUtility.DisplayProgressBar("Building", "Transfering build...", 5f / 6f);
          string temp = PSVitaUtilitiesSettings.TitleID;
          TransferFolder(BuildPath, "ftp://" + PSVitaUtilitiesSettings.PSVitaIP + ":1337/ux0:/app/" + temp);
          #endregion
          if (buildMode == BuildMode.Run)
          {
            #region Reboot Vita
            EditorUtility.DisplayProgressBar("Building", "Booting Game...", 6f / 7f);
            Error = $"{buildType} Failed (Network Error: Failed to reboot Vita)";
            #endregion
          }
        }
        #endregion

        #region USB - Build and Run - Send to Vita and Run
        if (buildMode == BuildMode.USBRun)
        {
          #region Send to Vita
          Error = $"{buildType} Failed (Network Error: Failed to Transfer Build)";
          EditorUtility.DisplayProgressBar("Building", "Transfering build...", 5f / 6f);
          string temp = PSVitaUtilitiesSettings.TitleID;
          TransferFolderUSB(BuildPath, $"{PSVitaUtilitiesSettings.USBDriveLetter}app/{temp}");
          #endregion
        }
        #endregion

        #region Emu - Transfer to Emu
        if (buildMode == BuildMode.EmuTransfer)
        {
          Error = $"{buildType} Failed (Failed to transfer Build)";
          EditorUtility.DisplayProgressBar("Building", "Transfering build...", 6f / 6f);
          CopyDirectory(BuildPath, PSVitaUtilitiesSettings.EmuStoragePath + "\\ux0/app\\" + PSVitaUtilitiesSettings.TitleID, true);
        }
        #endregion

        TimeSpan transferTime = DateTime.Now - startTransferTime;
        Debug.Log($"{buildType} Transfer complete in {transferTime:mm':'ss}!");
        TimeSpan buildTime = DateTime.Now - startTime;
        Debug.Log($"{buildType} Complete in {buildTime:mm':'ss}!");

        #region Run Game
        if (buildMode == BuildMode.Run || buildMode == BuildMode.USBRun)
        {
          if (PSVitaUtilitiesSettings.BuildRunReboot)
          {
            VitaReboot();
            if (EditorUtility.DisplayDialog("Start Game?", "Is the Vita ready to start the game?", "Yes", "No"))
              Error = $"{buildType} Failed (Network Error: Failed To Launch Game. Was the Vita fully rebooted?)";
          }

          ScreenOn();
          LaunchGame();

          EditorUtility.ClearProgressBar();
        }
        #endregion
      }
      catch
      {
        Debug.LogError(Error);
      }
      EditorUtility.ClearProgressBar();
    }
    #endregion

    #region Vita Control Functions
    [MenuItem("PSVita/Vita Control/Launch Game")]
    public static void LaunchGame()
    {
      PSVitaUtilitiesSettings.LoadSettings();
      if (PSVitaUtilitiesSettings.KillAllAppsBeforeLaunch)
        VitaDestroy();

      VitaControl($"launch { PSVitaUtilitiesSettings.TitleID}");
    }

    [MenuItem("PSVita/Vita Control/Launch Shell")]
    public static void VitaShellLaunch()
    {
      PSVitaUtilitiesSettings.LoadSettings();
      VitaControl("launch VITASHELL");
    }

    [MenuItem("PSVita/Vita Control/Screen On")]
    public static void ScreenOn() => VitaControl("screen on");

    [MenuItem("PSVita/Vita Control/Screen Off")]
    public static void ScreenOff() => VitaControl("screen off");

    [MenuItem("PSVita/Vita Control/Reboot")]
    public static void VitaReboot() => VitaControl("reboot");

    [MenuItem("PSVita/Vita Control/Kill All")]
    public static void VitaDestroy() => VitaControl("destroy");

    static void VitaControl(string action)
    {
      PSVitaUtilitiesSettings.LoadSettings();
      TcpClient client = new TcpClient(PSVitaUtilitiesSettings.PSVitaIP, (Int32)1338);

      Byte[] data = System.Text.Encoding.ASCII.GetBytes($"{action}\n");
      NetworkStream stream = client.GetStream();
      stream.Write(data, 0, data.Length);
      stream.Close();
      client.Close();
    }
    #endregion

    #region Helper Functions
    public static long FindPosition(Stream stream, byte[] byteSequence)
    {
      int b;
      long i = 0;
      while ((b = stream.ReadByte()) != -1)
      {
        if (b == byteSequence[i++])
        {
          if (i == byteSequence.Length)
            return stream.Position - byteSequence.Length;
        }
        else
          i = b == byteSequence[0] ? 1 : 0;
      }

      return -1;
    }
    private static bool Skippable(string file)
    {
      string[] skippableEntries = new string[] { "sce_sys", "sce_module", "eboot.bin" };
      for (int i = 0; i < skippableEntries.Length; i++)
      {
        if (file.Contains(skippableEntries[i]))
          return true;
      }
      return false;
    }
    private static string[] BuildRunCache(string _filePath)
    {
      var New = Directory.GetFiles(_filePath, "*.*", SearchOption.AllDirectories);

      if (!Directory.Exists(buildCache) || !PSVitaUtilitiesSettings.SizeCheck)
        return New;

      var Old = Directory.GetFiles(buildCache, "*.*", SearchOption.AllDirectories);
      List<string> files = new List<string>();
      for (int i = 0; i < New.Length; i++)
      {
        bool samePathLength = false;
        for (int a = 0; a < Old.Length; a++)
        {
          if (New[i].Substring(_filePath.Length) == Old[a].Substring(buildCache.Length))
          {
            samePathLength = true;
            FileInfo newFile = new FileInfo(New[i]);
            FileInfo oldFile = new FileInfo(Old[a]);
            if (newFile.Length != oldFile.Length)
              files.Add(New[i]);
            else
            {
              if (!FilesAreEqual_Hash(newFile, oldFile))
                files.Add(New[i]);
            }
          }
        }
        if (!samePathLength)
          files.Add(New[i]);
      }
      bool oldFileMissing = false;
      for (int a = 0; a < Old.Length; a++)
      {
        for (int i = 0; i < New.Length; i++)
        {
          oldFileMissing = true;
          if (New[i].Substring(_filePath.Length) == Old[a].Substring(buildCache.Length))
          {
            oldFileMissing = false;
            break;
          }
        }
        if (oldFileMissing)
        {
          files = new List<string>();
          Debug.LogError("Failed file found in cache that isnt on vita");
          break;
        }
      }
      if (files.Count == 0)
      {
        if (!EditorUtility.DisplayDialog("Continue?", "All files appear to be exactly the same do you want to continue? (File difference check can be disabled in settings)", "Yes", "No"))
          Debug.LogError("Files the same size, not transfering anything");
        else
          return New;
      }
      return files.ToArray();
    }
    static bool FilesAreEqual_Hash(FileInfo first, FileInfo second)
    {
      Stream stream = first.OpenRead();
      Stream stream1 = second.OpenRead();
      byte[] firstHash = MD5.Create().ComputeHash(stream);
      byte[] secondHash = MD5.Create().ComputeHash(stream1);
      stream.Close();
      stream1.Close();
      for (int i = 0; i < firstHash.Length; i++)
      {
        if (firstHash[i] != secondHash[i])
          return false;
      }
      return true;
    }
    static void CopyDirectory(string sourceDir, string destinationDir, bool recursive)
    {
      // Get information about the source directory
      var dir = new DirectoryInfo(sourceDir);

      // Check if the source directory exists
      if (!dir.Exists)
        throw new DirectoryNotFoundException($"Source directory not found: {dir.FullName}");

      // Cache directories before we start copying
      DirectoryInfo[] dirs = dir.GetDirectories();

      // Create the destination directory
      Directory.CreateDirectory(destinationDir);

      // Get the files in the source directory and copy to the destination directory
      foreach (FileInfo file in dir.GetFiles())
      {
        string targetFilePath = Path.Combine(destinationDir, file.Name);
        file.CopyTo(targetFilePath);
      }

      // If recursive and copying subdirectories, recursively call this method
      if (recursive)
      {
        foreach (DirectoryInfo subDir in dirs)
        {
          string newDestinationDir = Path.Combine(destinationDir, subDir.Name);
          CopyDirectory(subDir.FullName, newDestinationDir, true);
        }
      }
    }
    #endregion

    #region Build Stages
    private static void DeleteJunk(string _buildPath)
    {
      Directory.Delete(_buildPath + "/SymbolFiles", true);
      File.Delete(_buildPath + "/configuration.psp2path");
      File.Delete(_buildPath + "/TempBuild.bat");
    }
    private static void RemoveTrial(string _buildPath)
    {
      using (Stream stream = File.Open(_buildPath + "/TempBuild.self", FileMode.Open))
      {
        stream.Position = 0x80;
        stream.WriteByte(0x00);
        stream.Position = 0x00;
        if (!PSVitaUtilitiesSettings.ShowTrialWatermark)
        {
          long pos = FindPosition(stream, new byte[] { 0x74, 0x72, 0x69, 0x61, 0x6C, 0x2E, 0x70, 0x6E, 0x67 });
          stream.Position = pos;
          stream.WriteByte(0x00);
        }
      }
      System.IO.File.Move(_buildPath + "/TempBuild.self", _buildPath + "/eboot.bin");
    }
    private static void MakeZip(string _buildPath, string _filePath)
    {
      if (File.Exists(_filePath))
        File.Delete(_filePath);

      ZipFile.CreateFromDirectory(_buildPath, _filePath);
    }

    private static void TransferFTPVPK(string _filePath, string _ip)
    {
      WebClient client = new WebClient();
      int error = 0;
      bool done = false;
      while (!done)
      {
        try
        {
          client.UploadFile(new Uri(_ip), _filePath);
          done = true;
        }
        catch
        {
          error++;
          if (error >= PSVitaUtilitiesSettings.RetryCount)
          {
            done = true;
            Debug.LogError($"{buildType} Failed (Network Error: Failed to transfer Build)");
          }
        }
      }
    }
    private static void TransferFolder(string _filePath, string _ip)
    {
      WebClient client = new WebClient();

      var temp = BuildRunCache(_filePath);
      if (temp == null) return;

      List<int> error = new List<int>();
      int retry = 0;
      for (int i = 0; i < temp.Length; i++)
      {
        if (!Skippable(temp[i]))
        {
          string fileBeingTransferred = temp[i].Substring(_filePath.Length);
          try
          {
            if (retry >= PSVitaUtilitiesSettings.RetryCount) break;

            EditorUtility.DisplayProgressBar("Building", $"Transfering {fileBeingTransferred}...", (float)i / (float)temp.Length);
            Task.Delay(1000);
            client.UploadFile(new Uri(_ip + fileBeingTransferred), temp[i]);
            retry = 0;
          }
          catch
          {
            retry++;
            Debug.LogError("Network Error on " + fileBeingTransferred);
            error.Add(i);
          }
        }
      }
      // Retry transfer on failed files
      if (error.Count != 0 && retry < 3)
      {
        while (error.Count != 0)
        {
          for (int i = 0; i < error.Count; i++)
          {
            try
            {
              if (error.Count != 0)
              {
                client.UploadFile(new Uri(_ip + temp[error[i]].Substring(_filePath.Length)), temp[error[i]]);
                error.RemoveAt(i);
                i--;
                retry = 0;
              }
            }
            catch
            {
              retry++;
              if (retry >= PSVitaUtilitiesSettings.RetryCount)
              {
                Debug.LogError($"{buildType} Failed (Network Error: Failed to Transfer Build, Retry Count exceeded.)");
                error.RemoveRange(0, error.Count);
              }
            }
          }
        }
      }
      else if (retry >= PSVitaUtilitiesSettings.RetryCount)
      {
        Debug.LogError($"{buildType} Failed (Network Error: Failed to Transfer Build, Retry Count exceeded.)");
      }

      if (Directory.Exists(buildCache))
        Directory.Delete(buildCache, true);

      CopyDirectory(_filePath, buildCache, true);
    }

    private static void TransferUSBVPK(string _source, string _dest)
    {
      try { File.Copy(_source, _dest, true); }
      catch { Debug.LogError($"{buildType} Failed (Transfer Error: Failed to transfer Build)"); }
    }

    private static void TransferFolderUSB(string _source, string _dest)
    {
      try
      {
        DirectoryInfo target = new DirectoryInfo(_dest);
        DirectoryInfo source = new DirectoryInfo(_source);

        if (!Directory.Exists(target.FullName))
        {
          Debug.LogError("Please ensure you have installed the project already before using Build and Run");
          throw new Exception();
        }

        // Copy each file into it’s new directory.
        foreach (FileInfo fi in source.GetFiles())
        {
          if (!Skippable(fi.Name))
          {
            //Debug.Log($"Copying {target.FullName}\\{fi.Name}");
            fi.CopyTo(Path.Combine(target.ToString(), fi.Name), true);
          }
        }

        // Copy each subdirectory using recursion.
        foreach (DirectoryInfo diSourceSubDir in source.GetDirectories())
        {
          if (!Skippable(diSourceSubDir.Name))
          {
            DirectoryInfo nextTargetSubDir =
              target.CreateSubdirectory(diSourceSubDir.Name);
            TransferFolderUSB(diSourceSubDir.ToString(), nextTargetSubDir.ToString());
          }
        }
      }
      catch
      {
        Debug.LogError($"{buildType} Failed (Transfer Error: Failed to transfer Build)");
      }

    }
    #endregion
  }

  struct FileFTPData
  {
    public string Name;
    public string Size;
  }

  public enum BuildMode
  {
    Normal = 0,
    FTP = 1,
    FTPTransfer = 2,
    Run = 3,
    USB = 4,
    USBRun = 5,
    EmuTransfer = 6
  }

  [System.Serializable]
  enum FileState
  {
    Missing,
    Same,
    Size,
  }

}