using Renci.SshNet;
using System;
using System.Diagnostics;
using System.Linq;
using System.IO;
using System.Threading;
using UnityEditor;
using UnityEngine;

public class BuildScript
{   
    private static short maxUploadAttempts = 5;
    private static short maxFileChecks = 50;

    [MenuItem("Build/Build All")]
    public static void BuildAll()
    {
        BuildWindowsServer();
        BuildLinuxServer();
        BuildWindowsClient();
    }

    [MenuItem("Build/Build Server (Windows)")]
    public static void BuildWindowsServer()
    {
        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
        buildPlayerOptions.scenes = new[] { "Assets/Scenes/SampleScene.unity" };
        buildPlayerOptions.locationPathName = "Builds/Windows/Server/Server.exe";
        buildPlayerOptions.target = BuildTarget.StandaloneWindows64;
        buildPlayerOptions.options = BuildOptions.CompressWithLz4HC | BuildOptions.EnableHeadlessMode;

        Console.WriteLine("Building Server (Windows)...");
        BuildPipeline.BuildPlayer(buildPlayerOptions);
        Console.WriteLine("Built Server (Windows).");
    }

    [MenuItem("Build/Build Server (Linux)")]
    public static void BuildLinuxServer()
    {
        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
        buildPlayerOptions.scenes = new[] { "Assets/Scenes/SampleScene.unity" };
        buildPlayerOptions.locationPathName = "Builds/Linux/Server/Server.x86_64";
        buildPlayerOptions.target = BuildTarget.StandaloneLinux64;
        buildPlayerOptions.options = BuildOptions.CompressWithLz4HC | BuildOptions.EnableHeadlessMode;

        Console.WriteLine("Building Server (Linux)...");
        BuildPipeline.BuildPlayer(buildPlayerOptions);
        Console.WriteLine("Built Server (Linux).");
    }

    [MenuItem("Build/Build Server + Upload (Linux)")]
    public static void BuildLinuxServerWithUpload()
    {
        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
        buildPlayerOptions.scenes = new[] { "Assets/Scenes/Scene.unity" };
        buildPlayerOptions.locationPathName = "Builds/Linux/Server/Server.x86_64";
        buildPlayerOptions.target = BuildTarget.StandaloneLinux64;
        buildPlayerOptions.options = BuildOptions.CompressWithLz4HC | BuildOptions.EnableHeadlessMode;

        BuildPipeline.BuildPlayer(buildPlayerOptions);

        ArchiveFile();
        UploadFile();
    }

    private static void ArchiveFile()
    {
        Directory.CreateDirectory("BuildsForRemote");
        Process archiveScript = new Process();
        try {
            string exe = "winrar.exe";
            string result = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Machine)
                .Split(';')
                .Where(s => File.Exists(Path.Combine(s, exe)))
                .FirstOrDefault();
                
            result = "\"" + result + "\"\\";
            string path = Directory.GetCurrentDirectory() + "/archiver.bat";
            archiveScript.StartInfo.FileName = path;
            archiveScript.StartInfo.Arguments = result;
            archiveScript.Start();
            archiveScript.WaitForExit();
        } catch (Exception ex) {
            UnityEngine.Debug.LogError(ex.StackTrace + "");
        }
    }

    private static void UploadFile(int uploadAttempts = 0, int fileChecks = 0)
    {
        if (!File.Exists(Directory.GetCurrentDirectory() + "/BuildsForRemote/Server.zip")) {
            if (fileChecks >= maxFileChecks) {
                UnityEngine.Debug.Log(string.Format("Could not upload file after {0} attempts. Please try again by clicking \"Retry Upload\"", maxUploadAttempts));
                return;
            }
            Thread.Sleep(1000);
            UploadFile(uploadAttempts, fileChecks + 1);
            return;
        }

        try {
            String ip = Environment.GetEnvironmentVariable("FB_SERVER_DEV_IP", EnvironmentVariableTarget.User);
            int port = Int32.Parse(Environment.GetEnvironmentVariable("FB_SERVER_DEV_PORT", EnvironmentVariableTarget.User));
            String privateKeyStr = Environment.GetEnvironmentVariable("FB_SERVER_DEV_PRIVATE_KEY", EnvironmentVariableTarget.User);
            String user = Environment.GetEnvironmentVariable("FB_SERVER_DEV_USER", EnvironmentVariableTarget.User);

            PrivateKeyFile privateKey = new PrivateKeyFile(privateKeyStr);
            string path = Directory.GetCurrentDirectory() + @"\BuildsForRemote\Server.zip";
            using (var client = new SftpClient(ip, 22, user, privateKey)) {
                client.Connect();

                if (client.IsConnected) {
                    byte[] byteData = File.ReadAllBytes(path);
                    using (var ms = new MemoryStream(byteData))
                    {
                        ms.Write(byteData, 0, byteData.Length);
                        ms.Position = 0;
                        client.UploadFile(ms, string.Format("/home/{0}/Server.zip", user), true);
                    }
                    Directory.Delete("BuildsForRemote");
                } else {    
                    UnityEngine.Debug.LogError("Could not connect to server with the given environment credentials.");
                }
            }

            File.Delete(path);

            UnityEngine.Debug.Log("File successfully uploaded to server.");
            
        } catch (ArgumentException argEx) {
            UnityEngine.Debug.LogError(argEx);
            return;
        } catch (Exception ex) {
            if (uploadAttempts >= maxUploadAttempts) {
                UnityEngine.Debug.LogError(ex);
                return;
            }
            UnityEngine.Debug.LogWarning(string.Format("Upload attempt failed on attempt #{0}. Attempting to upload again. ({0} of {1} max attempts)", uploadAttempts, maxUploadAttempts));
            UnityEngine.Debug.LogError(ex);
            UploadFile(uploadAttempts + 1, fileChecks);
        }
    }

    [MenuItem("Build/Retry Upload")]
    public static void RetryUpload()
    {
        if (!File.Exists(Directory.GetCurrentDirectory() + "/BuildsForRemote/Server.zip")) {
            UnityEngine.Debug.LogError("File does not exist in /BuildsForRemote/, cannot upload. Please build, archive, and upload instead.");
            return;
        }
        UploadFile();
    }

    [MenuItem("Build/Build Client (Windows)")]
    public static void BuildWindowsClient()
    {
        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
        buildPlayerOptions.scenes = new[] { "Assets/Scenes/SampleScene.unity" };
        buildPlayerOptions.locationPathName = "Builds/Windows/Client/Client.exe";
        buildPlayerOptions.target = BuildTarget.StandaloneWindows64;
        buildPlayerOptions.options = BuildOptions.CompressWithLz4HC;

        Console.WriteLine("Building Client (Windows)...");
        BuildPipeline.BuildPlayer(buildPlayerOptions);
        Console.WriteLine("Built Client (Windows).");
    }
}