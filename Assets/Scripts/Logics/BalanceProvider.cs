using Firebase.Database;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class BalanceInfo
{
    public int techWorks;
    public int techWorksDelay;
    public int version;
    public float fightLength;
    public int initArenaRating;
    public float joinStageLength;
    public float lobbyLength;
    public float lossRatingMod;
    public int matchmakingGap;
    public int maxPlayersPerRoom;
    public float nexusCaptureTime;
    public float victoryRatingMod;
    public float respawnTimeBase;
    public float respawnTimeMax;
    public float spectacleTime;
    public int winnersCount;
    public float shieldRegenDelay;
    public int initCurrency;
    public int currencyPerFightMin;
    public int currencyPerWin;
    public int currencyPlaceBonus;
    public int currencyPerMissionMin;
    public float missionTimeRewardModifier;
    public int nameChangeCost;
}

public static class BalanceProvider
{
    private static DatabaseReference mBalanceDatabaseRef;
    public static BalanceInfo Balance;
    public static bool IsLoaded = false;
    public static Action OnValueChanged;
    const int clientVersion = 3;

    public static void Init()
    {
        mBalanceDatabaseRef = FirebaseDatabase.DefaultInstance.RootReference.Child("balance");

        mBalanceDatabaseRef.GetValueAsync().ContinueWith(task =>
                        {
                            if (task.IsFaulted)
                            {

                            }
                            else if (task.IsCompleted)
                            {
                                RefreshBalance(task.Result);
                                IsLoaded = true;
                            }
                        });
    }

    private static void RefreshBalance(DataSnapshot snapshot)
    {
        string restoredData = snapshot.GetRawJsonValue();

        if (restoredData.Length < 2)
            Balance = new BalanceInfo();
        else
            Balance = JsonUtility.FromJson<BalanceInfo>(restoredData);
    }

    private static void HandleBalanceValueChanged(object sender, ValueChangedEventArgs args)
    {
        if (args.DatabaseError != null)
        {
            Debug.LogError(args.DatabaseError.Message);
            return;
        }

        RefreshBalance(args.Snapshot);

        OnValueChanged?.Invoke();

        if (Balance.version > clientVersion)
        {
            MessageBox.instance.Show("Your game client version is old. Please, update it on Google Play.");
            Launcher.instance.CloseGameDelayed();
        }
        else if (Balance.techWorks != 0)
        {
            if (Balance.techWorksDelay < 1)
            {
                MessageBox.instance.Show("The game is currently under maintenance. We are sorry for the inconvenience.");
            }
            else
            {
                MessageBox.instance.Show(string.Format("The game will be under maintenance in {0} minutes. We are sorry for the inconvenience.", Balance.techWorksDelay));
                Launcher.instance.CloseGameDelayed(Balance.techWorksDelay * 60f);
            }
        }
    }
}
