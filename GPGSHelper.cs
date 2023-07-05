using UnityEngine;
using GooglePlayGames;
using GooglePlayGames.BasicApi;
using GooglePlayGames.BasicApi.SavedGame;
using System.Linq;
using System;
public class GPGSHelper : MonoBehaviour
{
    private string _mStatus = "Ready";
    private string mSavedGameFilename = "saved_data";
    private ISavedGameMetadata mCurrentSavedGame = null;
    private string mSavedGameFileContent = string.Empty;
    private string statsMessage = string.Empty;

    public Action<string> cloudSaveDataAction;
    public bool isConnected;
    public void Start()
    {
        PlayGamesPlatform.DebugLogEnabled = true;
        PlayGamesPlatform.Activate();
        PlayGamesPlatform.Instance.Authenticate(OnSignInResult);
    }

    //Saving data to cloud
    internal void WriteSavedGame(string gameData)
    {
        Status = "ShowWriteSavedGame";
        if (mCurrentSavedGame == null || !mCurrentSavedGame.IsOpen)
        {
            Status = "No opened saved game selected.";
            return;
        }

        var update = new SavedGameMetadataUpdate.Builder()
            .WithUpdatedDescription("Saved at " + DateTime.Now.ToString())
            .WithUpdatedPlayedTime(mCurrentSavedGame.TotalTimePlayed.Add(LocalAnalytics.Instance.PlayTime()))
            .Build();

        mSavedGameFileContent = gameData;

        Debug.Log("saved " + mSavedGameFileContent);
        PlayGamesPlatform.Instance.SavedGame.CommitUpdate(
               mCurrentSavedGame,
               update,
               System.Text.ASCIIEncoding.Default.GetBytes(mSavedGameFileContent),
               (status, updated) =>
               {
                   Status = "Write status was: " + status;
                   LogStatus();
               });
        LogStatus();
    }
    
    //For reading game data to cloud
    internal void OpenSavedGame()
    {
        Status = "OpenSavedGame";
        ConflictResolutionStrategy strategy = ConflictResolutionStrategy.UseOriginal;
        PlayGamesPlatform.Instance.SavedGame.OpenWithAutomaticConflictResolution(
            mSavedGameFilename,
            DataSource.ReadNetworkOnly,
            strategy,
            (status, openedFile) =>
            {
                Status = "Open status for file " + mSavedGameFilename + ": " + status + "\n";
                if (openedFile != null)
                {
                    Status += "Successfully opened file: " + openedFile;
                    GooglePlayGames.OurUtils.Logger.d("Opened file: " + openedFile);
                    mCurrentSavedGame = openedFile;
                    LogStatus();
                    ReadSavedGame();
                }
                LogStatus();
            });
        LogStatus();
    }

    //Canot delete
    internal void DoDeleteSavedGame()
    {
        if (mCurrentSavedGame == null)
        {
            Status = "No save game selected";
            return;
        }

        PlayGamesPlatform.Instance.SavedGame.Delete(mCurrentSavedGame);
        Status = mCurrentSavedGame.Filename + " deleted.";
        mCurrentSavedGame = null;
    }

    internal void ReadSavedGame()
    {
       var openedFile = mSavedGameFilename;
        PlayGamesPlatform.Instance.SavedGame.ReadBinaryData(
            mCurrentSavedGame,
            (status, binaryData) =>
            {
                Status = "Reading file " + openedFile + ", status: " + status + "\n";
                LogStatus();
                if (binaryData != null)
                {
                    var stringContent = System.Text.ASCIIEncoding.Default.GetString(binaryData);
                    Status += "File content: " + stringContent;
                    mSavedGameFileContent = stringContent;
                    cloudSaveDataAction?.Invoke(mSavedGameFileContent);
                }
                else
                {
                    mSavedGameFileContent = string.Empty;
                }
                LogStatus();
            });
    }

  
    internal void DoFetchAll()
    {
        PlayGamesPlatform.Instance.SavedGame.FetchAllSavedGames(
            DataSource.ReadNetworkOnly,
            (status, savedGames) =>
            {
                Status = "Fetch All Status: " + status + "\n";
                Status += "Saved Games: [" +
                          string.Join(",", savedGames.Select(g => g.Filename).ToArray()) + "]";
                savedGames.ForEach(g =>
                    GooglePlayGames.OurUtils.Logger.d("Retrieved save game: " + g));
                LogStatus();
            });
    }

   
    internal void ShowUserInfoUi()
    {
        Debug.Log("User info for " + Social.localUser.userName);
        if (statsMessage == string.Empty && Social.localUser.authenticated)
        {
            statsMessage = "loading stats....";
            ((PlayGamesLocalUser)Social.localUser).GetStats(
                (result, stats) =>
                {
                    statsMessage = result + " number of sessions: " +
                                   stats.NumberOfSessions;

                    Debug.Log("User info for " + statsMessage);
                });
        }
        
    }

    internal string Status
    {
        get { return _mStatus; }
        set { _mStatus = value; }
    }

    //Manual authentication Call from button
    internal void DoAuthenticate()
    {
        Debug.Log("Authenticating...");
        PlayGamesPlatform.Activate();
        PlayGamesPlatform.Instance.ManuallyAuthenticate(OnSignInResult);
    }

    private void OnSignInResult(SignInStatus signInStatus)
    {
        if (signInStatus == SignInStatus.Success)
        {
            Status = "Authenticated. Hello, " + Social.localUser.userName + " (" + Social.localUser.id + ")";
            isConnected = true;
            OpenSavedGame();
        }
        else
        {
            Status = "*** Failed to authenticate with " + signInStatus;
        }
        LogStatus();
    }
    void LogStatus()
    {
        Debug.Log(Status);
    }
}
