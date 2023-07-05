using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CloudData : MonoBehaviour
{
    string cloudSaveData = string.Empty;
    
    GPGSHelper gPGSHelper;

    // Start is called before the first frame update
    void Start()
    {
#if UNITY_ANDROID
        gPGSHelper = gameObject.AddComponent<GPGSHelper>();
        gPGSHelper.cloudSaveDataAction = OnCloudDataLoaded;
#endif
    }

    void OnCloudDataLoaded(string cloudSaveString)
    {
        cloudSaveData = cloudSaveString;

        JSONNode json = JSON.Parse(cloudSaveData);

        int CloudLastCompletedLevelNumber = json["last_completed_level_number"].AsInt;

        //campare level so that you save data only if level is greater
        if(CloudLastCompletedLevelNumber > GameController.Instance.LastCompletedLevelNumber)
        {
            //cloud data is higher so save the data and restart the game.
            GameController.Instance.SaveCloudData(cloudSaveString);
            PopupManager.Instance.ShowToast("Saving cloud data");
            //reload scene
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
        else
        {
            PopupManager.Instance.ShowToast("Discarding cloud data and uploading new data");
            //cloud data is lower level so leave that and save the current data to cloud
            gPGSHelper.WriteSavedGame(GameController.Instance.GetSaveData());
        }
    }

    public void SaveDataToCloud()
    {
        if (gPGSHelper.isConnected)
        {
            PopupManager.Instance.ShowToast("uploading new data");
            gPGSHelper.WriteSavedGame(GameController.Instance.GetSaveData());
        }
    }
}
