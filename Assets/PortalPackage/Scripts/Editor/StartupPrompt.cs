using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;

public class StartupPrompt : EditorWindow{
    public static string[] fileContents;
    public static string currentVersion;
    //[MenuItem("Window/Portal Kit Pro Startup")]
    // Use this for initialization

    public static void ShowWindow() {
        EditorWindow.GetWindow(typeof(StartupPrompt));
    }

    void OnGUI() {
        //if (fileContents == null)
        //    return; 
        //Name
        GUILayout.Label(string.Format(
            "Version: {0} (Newest Version: {1})" + System.Environment.NewLine + "Read the documentation to get started!", fileContents[1], currentVersion), EditorStyles.boldLabel);

        if (GUILayout.Button("Documentation")) {
            Application.OpenURL(Application.dataPath + "/PortalPackage/Manual.pdf"); 
        }
        try {

            if (fileContents[0] == "0") {
                GUILayout.Label("You need to initialize your import." + System.Environment.NewLine +
                "What kind of project is this?");
                if (GUILayout.Button("VR")) {
                    Directory.Delete("Assets/PortalPackage/Scripts/NonVR/");
                    Directory.Delete("Assets/PortalPackage/Resources/Prefabs/NonVR/");
                    fileContents[0] = "1";
                    File.WriteAllText(Application.dataPath + "/PortalPackage/Resources/VersionInfo.txt", string.Join(System.Environment.NewLine, fileContents));
                    AssetDatabase.SaveAssets();
                }

                if (GUILayout.Button("Non VR")) {
                    Directory.Delete("Assets/PortalPackage/Scripts/VR/");
                    Directory.Delete("Assets/PortalPackage/Resources/Prefabs/VR/");
                    fileContents[0] = "1";
                    File.WriteAllText(Application.dataPath + "/PortalPackage/Resources/VersionInfo.txt", string.Join(System.Environment.NewLine, fileContents));
                    AssetDatabase.SaveAssets();
                }
            }
            //GUILayout.Label(string.Format(
            if (float.Parse(fileContents[1]) < float.Parse(currentVersion) && fileContents[2] != "0") {
                GUILayout.Label(string.Format("You are {0} important versions behind, and an update is highly recommended." + System.Environment.NewLine + "Please update to avoid bugs, as this is typically what updates are fixing.", float.Parse(currentVersion) - float.Parse(fileContents[1])));
                if (GUILayout.Button("Update")) {
                    Application.OpenURL("http://u3d.as/JeF");
                }
                if (GUILayout.Button("Never remind me again")) {
                    fileContents[2] = "0";
                    File.WriteAllText(Application.dataPath + "/PortalPackage/Resources/VersionInfo.txt", string.Join(System.Environment.NewLine, fileContents));
                    this.Close();
                }
            }
        } catch { }
    }
}
