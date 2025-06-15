using UnityEditor;
using UnityEngine;
public class ResetProgressTool
{
    [MenuItem("Tools/SeroBoom/Reset Player Progress")]
    public static void ResetPlayerProgress()
    {
        PlayerPrefs.DeleteKey("CurrentLevel");

        Debug.LogWarning("!!! Player progress ('CurrentLevel') has been reset to 0. The game will start from Level 1 on next play. !!!");
    }
}