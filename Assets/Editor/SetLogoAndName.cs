using UnityEngine;
using UnityEditor;
using System.IO;

public class SetLogoAndName
{
    [MenuItem("Tools/Set Logo And Name")]
    public static void ApplySettings()
    {
        // Set Product Name
        PlayerSettings.productName = "Who";

        // Load the logo texture
        string logoPath = "Assets/Images/logo.png";
        Texture2D logoTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(logoPath);

        if (logoTexture == null)
        {
            Debug.LogError("Logo texture not found at " + logoPath + ". Please check if it was imported correctly.");
            return;
        }

        // Set Default Icon (Unknown group usually maps to Default)
        Texture2D[] defaultIcons = new Texture2D[] { logoTexture };
        PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Unknown, defaultIcons);

        // Set iOS Icons
        PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.iOS, defaultIcons);

        // Set Android Icons
        PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Android, defaultIcons);

        AssetDatabase.SaveAssets();
        Debug.Log("Successfully set product name to 'Who' and applied logo.png as app icon for iOS and Android!");
    }
}
