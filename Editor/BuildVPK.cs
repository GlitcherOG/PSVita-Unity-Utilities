using UnityEditor;
using UnityEngine;
using Unity.EditorCoroutines.Editor;
using System.Threading;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using PSVitaUtilities.Settings;
using UnityEngine.Networking;

namespace PSVitaUtilities.Building
{
    public class BuildVPK : EditorWindow
    {
        #region Build Functions
        [MenuItem("PSVita/Build/Build VPK")]
        public static void BuildGameNormal() => BuildGame(BuildMode.Normal);

        [MenuItem("PSVita/Build/Build FTP VPK")]
        public static void BuildGameFTP() => BuildGame(BuildMode.FTP);

        [MenuItem("PSVita/Build/Build and Run (WIP)")]
        public static void BuildGameRun() => BuildGame(BuildMode.Run);

        public static void BuildGame(BuildMode buildMode)
        {
            string Error = "Project Failed To Build";
            string buildType = "";
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
                    if (!EditorUtility.DisplayDialog("Are you sure?", "This is a work in progress function meant only to quickly test out code changes, it requires the game being preinstalled on the system", "Continue", "Stop"))
                        return;

          EditorUtility.DisplayProgressBar("Building", "Killing All Apps On Vita...", 0f / 6f);
          Error = $"{buildType} Failed (Network Error: Failed to kill all apps)";
          VitaDestroy();

          #region Initialise Vita
          Error = $"{buildType} Failed (Network Error: Failed to initially reboot Vita)";
          VitaReboot();
          #endregion
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
                EditorUtility.DisplayProgressBar("Building", "Building project...", 2f / 5f);
                BuildPipeline.BuildPlayer(EditorBuildSettings.scenes, BuildPath, BuildTarget.PSP2, BuildOptions.None);
                #endregion

                #region Delete Junk
                EditorUtility.DisplayProgressBar("Building", "Deleting junk...", 3f / 5f);
                Error = $"{buildType} Failed (Failed to Delete Junk)";
                var t = Task.Run(() => DeleteJunk(BuildPath));
                t.Wait();
                #endregion

                #region Remove Trial
                Error = $"{buildType} Failed (Failed to edit TempBuild.self)";
                EditorUtility.DisplayProgressBar("Building", "Removing trial...", 4f / 5f);
                t = Task.Run(() => RemoveTrial(BuildPath));
                t.Wait();

                #endregion

                #region Make VPK
                if (buildMode != BuildMode.Run)
                {
                    Error = $"{buildType} Failed (Failed to zip To VPK)";
                    EditorUtility.DisplayProgressBar("Building", "Finalising build...", 5f / 5f);

                    t = Task.Run(() => MakeZip(BuildPath, filePath));
                    t.Wait();
                }
                #endregion

                #region FTP - Send to Vita
                if (buildMode == BuildMode.FTP)
                {
                    Error = $"{buildType} Failed (Network Error: Failed to transfer Build)";
                    EditorUtility.DisplayProgressBar("Building", "Transfering build...", 6f / 6f);
                    string productName = PlayerSettings.productName;
                    t = Task.Run(() => TransferFTPVPK(filePath, "ftp://" + PSVitaUtilitiesSettings.PSVitaIP + ":1337/" + PSVitaUtilitiesSettings.FTPLocation + "/" + productName + ".vpk"));
                    t.Wait();
                }
                #endregion

                #region Build and Run - Send to Vita and Run
                if (buildMode == BuildMode.Run)
                {
                    #region Send to Vita
                    Error = $"{buildType} Failed (Network Error: Failed to Transfer Build)";
                    EditorUtility.DisplayProgressBar("Building", "Transfering build...", 5f / 6f);
                    string temp = PSVitaUtilitiesSettings.TitleID;
                    t = Task.Run(() => TransferFolder(BuildPath, "ftp://" + PSVitaUtilitiesSettings.PSVitaIP + ":1337/ux0:/app/" + temp));
                    t.Wait();
                    #endregion

                    #region Reboot Vita
                    EditorUtility.DisplayProgressBar("Building", "Booting Game...", 6f / 6f);
                    Error = $"{buildType} Failed (Network Error: Failed to reboot Vita)";
                    VitaReboot();
                    #endregion

                    #region Run Game
                    //Task.Delay(4000);
                    //try
                    //{
                    //  Error = $"{buildType} Failed (Network Error: Failed To Launch Game Automatically. Was the Vita fully rebooted?)";
                    //  if (PingVita())
                    //  {
                    //    ScreenOn();
                    //    LaunchGame();
                    //  }
                    //}
                    //catch
                    //{
                    if (EditorUtility.DisplayDialog("Start Game?", "Is the Vita ready to start the game?", "Yes", "No"))
                    {
                        Error = $"{buildType} Failed (Network Error: Failed To Launch Game. Was the Vita fully rebooted?)";
                        ScreenOn();
                        LaunchGame();
                    }
                    //}
                    EditorUtility.ClearProgressBar();
                    #endregion
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
            if (byteSequence.Length > stream.Length)
                return -1;

            byte[] buffer = new byte[byteSequence.Length];

            using (BufferedStream bufStream = new BufferedStream(stream, byteSequence.Length))
            {
                int i = 0;
                if (i != 0)
                {
                    i = 0;
                }
                while ((i = bufStream.Read(buffer, 0, byteSequence.Length)) == byteSequence.Length)
                {
                    if (byteSequence.SequenceEqual(buffer))
                        return bufStream.Position - byteSequence.Length;
                    else
                        bufStream.Position -= byteSequence.Length - PadLeftSequence(buffer, byteSequence);
                }
            }

            return -1;
        }
        private static int PadLeftSequence(byte[] bytes, byte[] seqBytes)
        {
            int i = 1;
            while (i < bytes.Length)
            {
                int n = bytes.Length - i;
                byte[] aux1 = new byte[n];
                byte[] aux2 = new byte[n];
                Array.Copy(bytes, i, aux1, 0, n);
                Array.Copy(seqBytes, aux2, n);
                if (aux1.SequenceEqual(aux2))
                    return i;
                i++;
            }
            return i;
        }
        static bool PingVita()
        {
            Debug.Log("Pinging Vita...");
            TcpClient client = new TcpClient(PSVitaUtilitiesSettings.PSVitaIP, (Int32)1338);
            if (client.Connected)
            {
                client.Close();
                Debug.Log("Vita Pinged!");
                return true;
            }
            return false;
        }
        #endregion

        #region Build Stages
        private static async Task DeleteJunk(string _buildPath)
        {
            Directory.Delete(_buildPath + "/SymbolFiles", true);
            File.Delete(_buildPath + "/configuration.psp2path");
            File.Delete(_buildPath + "/TempBuild.bat");
            await Task.Delay(1000);
        }
        private static async Task RemoveTrial(string _buildPath)
        {
            long pos = 0;
            if (!PSVitaUtilitiesSettings.FastBuild)
            {
                pos = FindPosition(File.Open(_buildPath + "/TempBuild.self", FileMode.Open), new byte[] { 0x74, 0x72, 0x69, 0x61, 0x6C, 0x2E, 0x70, 0x6E, 0x67 });
            }
            using (Stream stream = File.Open(_buildPath + "/TempBuild.self", FileMode.Open))
            {
                stream.Position = 0x80;
                stream.WriteByte(0x00);
                stream.Position = 0x00;
                if (!PSVitaUtilitiesSettings.FastBuild)
                {
                    stream.Position = pos;
                    stream.WriteByte(0x00);
                }
            }
            System.IO.File.Move(_buildPath + "/TempBuild.self", _buildPath + "/eboot.bin");
            await Task.Delay(5000);
        }
        private static async Task MakeZip(string _buildPath, string _filePath)
        {
            if (File.Exists(_filePath))
            {
                File.Delete(_filePath);
            }
            ZipFile.CreateFromDirectory(_buildPath, _filePath);
            //File.Delete(_buildPath);
            await Task.Delay(1000);
        }
        private static async Task TransferFTPVPK(string _filePath, string _ip)
        {
            WebClient client = new WebClient();
            client.UploadFile(new Uri(_ip), _filePath);
            await Task.Delay(1000);
        }
        private async static Task TransferFolder(string _filePath, string _ip)
        {
            WebClient client = new WebClient();
            //client.Timeout = 30000;
            var temp = Directory.GetFiles(_filePath, "*.*", SearchOption.AllDirectories);
            List<int> error = new List<int>();
            int retry = 0;
            for (int i = 0; i < temp.Length; i++)
            {
                if (!temp[i].Contains("sce_sys"))
                {
                    try
                    {
                        if (retry == 3)
                        {
                            break;
                        }
                        client.UploadFile(new Uri(_ip + temp[i].Substring(_filePath.Length)), temp[i]);
                        retry = 0;
                    }
                    catch
                    {
                        retry += 1;
                        Debug.LogError("Network Error on " + temp[i].Substring(_filePath.Length));
                        error.Add(i);
                    }
                }
            }
            if (error.Count != 0)
            {
                while (error.Count != 0)
                {
                    for (int i = 0; i < error.Count; i++)
                    {
                        Debug.Log("Retrying Upload");
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
                            retry += 1;
                            if (retry == 3)
                            {
                                error.RemoveRange(0, error.Count);
                            }
                        }
                    }
                }
            }
            await Task.Delay(3000);
        }
        #endregion
    }

    public enum BuildMode
    {
        Normal = 0,
        FTP = 1,
        Run = 2
    }
}