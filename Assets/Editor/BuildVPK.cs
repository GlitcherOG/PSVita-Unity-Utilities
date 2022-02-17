using UnityEditor;
using UnityEngine;
using System.Threading;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System;
using System.Net;
using System.Threading.Tasks;
using System.Net.Sockets;
using PSVitaUtilities.Settings;

namespace PSVitaUtilities.Building
{
    public class BuildVPK : EditorWindow
    {

        [MenuItem("PSVita/Build/Build VPK")]
        public static void BuildGame()
        {
            string Error="Project Failed To Build";
            try
            {
                Error = "Build FTP VPK Failed (Failed to Load Settings)";
                PSVitaUtilitiesSettings.LoadSettings();

                if(!PSVitaUtilitiesSettings.CheckValidTitleID())
                {
                    Debug.LogError("TitleID Error can be changed in project settings or in the psvita settings");
                    return;
                }

                EditorUtility.DisplayProgressBar("Building", "Starting process...", 1f / 5f);
                string BuildPath = Application.dataPath.TrimEnd("Assets".ToCharArray()) + "/Build/TempBuild";
                string filePath = EditorUtility.SaveFilePanel("Save VPK file", "", "", "vpk");
                if(filePath=="")
                {
                    EditorUtility.ClearProgressBar();
                    return;
                }

                Error = "VPK Build Failed (Failed to Create Directory)";
                Directory.CreateDirectory(BuildPath);

                Error = "VPK Build Failed (Failed to Build)";
                EditorUtility.DisplayProgressBar("Building", "Building project...", 2f / 5f);
                BuildPipeline.BuildPlayer(EditorBuildSettings.scenes, BuildPath, BuildTarget.PSP2, BuildOptions.None);

                Thread.Sleep(1000);
                EditorUtility.DisplayProgressBar("Building", "Deleting junk...", 3f / 5f);
                Error = "VPK Build Failed (Failed to Delete Junk)";
                var t = Task.Run(() => DeleteJunk(BuildPath));
                t.Wait();

                Error = "VPK Build Failed (Failed to edit TempBuild.self)";
                EditorUtility.DisplayProgressBar("Building", "Removing trial...", 4f / 5f);
                t = Task.Run(() => RemoveTrial(BuildPath));
                t.Wait();

                Error = "VPK Build Failed (Failed to Zip To VPK)";
                EditorUtility.DisplayProgressBar("Building", "Finalising build...", 5f / 5f);

                t = Task.Run(() => MakeZip(BuildPath, filePath));
                t.Wait();
            }
            catch
            {
                Debug.LogError(Error);
            }
            EditorUtility.ClearProgressBar();
        }

        [MenuItem("PSVita/Build/Build FTP VPK")]
        public static void BuildGameFTP()
        {
            string Error = "Project Failed To Build";
            try
            {
                Error = "Build FTP VPK Failed (Failed to Load Settings)";
                PSVitaUtilitiesSettings.LoadSettings();
                if (!PSVitaUtilitiesSettings.CheckValidTitleID())
                {
                    Debug.LogError("TitleID Error can be changed in project settings or in the psvita settings");
                    return;
                }

                EditorUtility.DisplayProgressBar("Building", "Starting process...", 1f / 6f);

                string BuildPath = Application.dataPath.TrimEnd("Assets".ToCharArray()) + "/Build/TempBuild";
                string filePath = Application.dataPath.TrimEnd("Assets".ToCharArray()) + "/Build/" + PlayerSettings.productName + ".vpk";

                Error = "Build FTP VPK Failed (Failed to Create Directory)";
                Directory.CreateDirectory(BuildPath);

                Error = "Build FTP VPK Failed (Failed to Build Project)";
                EditorUtility.DisplayProgressBar("Building", "Building project...", 2f / 6f);
                BuildPipeline.BuildPlayer(EditorBuildSettings.scenes, BuildPath, BuildTarget.PSP2, BuildOptions.None);

                Error = "Build FTP VPK Failed (Failed to Delete Junk)";
                EditorUtility.DisplayProgressBar("Building", "Deleting junk...", 3f / 6f);
                var t = Task.Run(() => DeleteJunk(BuildPath));
                t.Wait();

                Error = "Build FTP VPK Failed (Failed to edit TempBuild.self)";
                EditorUtility.DisplayProgressBar("Building", "Removing trial...", 4f / 6f);
                t = Task.Run(() => RemoveTrial(BuildPath));
                t.Wait();

                Error = "Build FTP VPK Failed (Failed to Zip to VPK)";
                EditorUtility.DisplayProgressBar("Building", "Finalising build...", 5f / 6f);
                t = Task.Run(() => MakeZip(BuildPath, filePath));
                t.Wait();

                Error = "Build FTP VPK Failed (Network Error, Failed to Transfer Build)";
                EditorUtility.DisplayProgressBar("Building", "Transfering build...", 6f / 6f);
                string productName = PlayerSettings.productName;
                t = Task.Run(() => TransferFTPVPK(filePath, "ftp://"+PSVitaUtilitiesSettings.PSVitaIP+":1337/"+ PSVitaUtilitiesSettings.FTPLocation + "/" + productName + ".vpk"));
                t.Wait();
            }
            catch
            {
                Debug.LogError(Error);
            }
            EditorUtility.ClearProgressBar();
        }

        [MenuItem("PSVita/Build/Build and Run (WIP)")]
        public static void BuildGameRun()
        {
            string Error = "Project Failed To Build";
            try
            {
                Error = "Build and Run Failed (Failed to Load Settings)";
                PSVitaUtilitiesSettings.LoadSettings();

                if (!EditorUtility.DisplayDialog("Are you sure?", "This is a work in progress function meant only to quickly test out code changes, it requires the game being preinstalled on the system", "Continue", "Stop"))
                {
                    return;
                }
                if (!PSVitaUtilitiesSettings.CheckValidTitleID())
                {
                    Debug.LogError("TitleID Error can be changed in project settings or in the psvita settings");
                    return;
                }

                EditorUtility.DisplayProgressBar("Building", "Killing All Apps On Vita...", 0f / 6f);
                Error = "Build and Run Failed (Network Error, Failed to kill all apps)";
                VitaDestroy();
                EditorUtility.DisplayProgressBar("Building", "Starting process...", 1f / 6f);

                string BuildPath = Application.dataPath.TrimEnd("Assets".ToCharArray()) + "/Build/TempBuild";

                Error = "Build and Run Failed (Failed to Create Directory)";
                Directory.CreateDirectory(BuildPath);

                Error = "Build and Run Failed (Failed to Build Project)";
                EditorUtility.DisplayProgressBar("Building", "Building project...", 2f / 6f);
                BuildPipeline.BuildPlayer(EditorBuildSettings.scenes, BuildPath, BuildTarget.PSP2, BuildOptions.None);

                Error = "Build and Run Failed (Failed to Delete Junk)";
                EditorUtility.DisplayProgressBar("Building", "Deleting junk...", 3f / 6f);
                var t = Task.Run(() => DeleteJunk(BuildPath));
                t.Wait();

                Error = "Build and Run Failed (Failed to edit TempBuild.self)";
                EditorUtility.DisplayProgressBar("Building", "Removing trial...", 4f / 6f);
                t = Task.Run(() => RemoveTrial(BuildPath));
                t.Wait();

                Error = "Build and Run Failed (Network Error, Failed to Tranfer Build)";
                EditorUtility.DisplayProgressBar("Building", "Transfering build...", 5f / 6f);
                t = Task.Run(() => TransferFolder(BuildPath, "ftp://" + PSVitaUtilitiesSettings.PSVitaIP + ":1337/ux0:/app/" + PSVitaUtilitiesSettings.TitleID));
                t.Wait();
                EditorUtility.DisplayProgressBar("Building", "Booting Game...", 6f / 6f);
                Error = "Build and Run Failed (Network Error, Failed to Reboot Vita)";
                VitaReboot();
                if (EditorUtility.DisplayDialog("Start Game?","Is the vita ready to start the game?", "Yes", "No"))
                {
                    Error = "Build and Run Failed (Network Error, Failed To Launch Game was the vita fully rebooted?)";
                    Screenon();
                    LaunchGame();
                }
                EditorUtility.ClearProgressBar();
            }
            catch
            {
                Debug.LogError(Error);
                EditorUtility.ClearProgressBar();
            }
        }

        [MenuItem("PSVita/Vita Control/Launch Game")]
        public static void LaunchGame()
        {
            PSVitaUtilitiesSettings.LoadSettings();
            Int32 port = 1338;
            TcpClient client = new TcpClient(PSVitaUtilitiesSettings.PSVitaIP, port);
            client.SendTimeout = 60000;
            Byte[] data = System.Text.Encoding.ASCII.GetBytes("launch " + PSVitaUtilitiesSettings.TitleID + "\n");
            NetworkStream stream = client.GetStream();
            stream.Write(data, 0, data.Length);
            stream.Close();
            client.Close();
        }

        [MenuItem("PSVita/Vita Control/Launch Shell")]
        public static void VitaShellLaunch()
        {
            PSVitaUtilitiesSettings.LoadSettings();
            Int32 port = 1338;
            TcpClient client = new TcpClient(PSVitaUtilitiesSettings.PSVitaIP, port);
            Byte[] data = System.Text.Encoding.ASCII.GetBytes("launch VITASHELL\n");
            NetworkStream stream = client.GetStream();
            stream.Write(data, 0, data.Length);
            stream.Close();
            client.Close();
        }

        [MenuItem("PSVita/Vita Control/Screen On")]
        public static void Screenon()
        {
            PSVitaUtilitiesSettings.LoadSettings();
            Int32 port = 1338;
            TcpClient client = new TcpClient(PSVitaUtilitiesSettings.PSVitaIP, port);
            Byte[] data = System.Text.Encoding.ASCII.GetBytes("screen on\n");
            NetworkStream stream = client.GetStream();
            stream.Write(data, 0, data.Length);
            stream.Close();
            client.Close();
        }

        [MenuItem("PSVita/Vita Control/Screen Off")]
        public static void Screenoff()
        {
            PSVitaUtilitiesSettings.LoadSettings();
            Int32 port = 1338;
            TcpClient client = new TcpClient(PSVitaUtilitiesSettings.PSVitaIP, port);
            Byte[] data = System.Text.Encoding.ASCII.GetBytes("screen off\n");
            NetworkStream stream = client.GetStream();
            stream.Write(data, 0, data.Length);
            stream.Close();
            client.Close();
        }

        [MenuItem("PSVita/Vita Control/Reboot")]
        public static void VitaReboot()
        {
            PSVitaUtilitiesSettings.LoadSettings();
            Int32 port = 1338;
            TcpClient client = new TcpClient(PSVitaUtilitiesSettings.PSVitaIP, port);
            Byte[] data = System.Text.Encoding.ASCII.GetBytes("reboot\n");
            NetworkStream stream = client.GetStream();
            stream.Write(data, 0, data.Length);
            stream.Close();
            client.Close();
        }

        [MenuItem("PSVita/Vita Control/Kill All")]
        public static void VitaDestroy()
        {
            PSVitaUtilitiesSettings.LoadSettings();
            Int32 port = 1338;
            TcpClient client = new TcpClient(PSVitaUtilitiesSettings.PSVitaIP, port);
            Byte[] data = System.Text.Encoding.ASCII.GetBytes("destroy\n");
            NetworkStream stream = client.GetStream();
            stream.Write(data, 0, data.Length);
            stream.Close();
            client.Close();
        }


        public static long FindPosition(Stream stream, byte[] byteSequence)
        {
            if (byteSequence.Length > stream.Length)
                return -1;

            byte[] buffer = new byte[byteSequence.Length];

            using (BufferedStream bufStream = new BufferedStream(stream, byteSequence.Length))
            {
                int i = 0;
                if(i!=0)
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
            foreach (var item in Directory.GetFiles(_filePath, "*.*", SearchOption.AllDirectories))
            {
                if (!item.Contains("sce_sys"))
                {
                    client.UploadFile(new Uri(_ip + item.Substring(_filePath.Length)), item);
                }
            }
            await Task.Delay(3000);
        }
    }
}