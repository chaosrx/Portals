using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
[InitializeOnLoad]
public class StartupScanner{

	// Use this for initialization
	static StartupScanner () {
        TextAsset fileContents = Resources.Load<TextAsset>("VersionInfo");
        StartupPrompt.fileContents = fileContents.ToString().Split(new string[] { System.Environment.NewLine}, System.StringSplitOptions.None);
        string newestVersion = "Connection Error";
        using (var wc = new System.Net.WebClient()) {
            newestVersion = wc.DownloadString("http://pastebin.com/raw/0bzKCecZ");
        }
        StartupPrompt.currentVersion = newestVersion;
        if (StartupPrompt.fileContents[0] == "0" || (float.Parse(StartupPrompt.fileContents[1]) < float.Parse(StartupPrompt.currentVersion) && StartupPrompt.fileContents[2] != "0")) {
            var window = EditorWindow.GetWindow(typeof(StartupPrompt), true, "Portal Kit Pro Startup", true);
        }   
    }
}
