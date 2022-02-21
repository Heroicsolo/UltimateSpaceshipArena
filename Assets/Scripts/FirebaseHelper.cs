using Firebase.Database;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class FirebaseHelper
{
    public static void GetValueFromSnapshot(this DataSnapshot snapshot, string childName, bool defaultValue, out bool savedValue)
    {
        bool notEmptyProfile = snapshot != null && snapshot.ChildrenCount > 1;
        savedValue = notEmptyProfile && snapshot.HasChild(childName) ? bool.Parse(snapshot.Child(childName).Value.ToString()) : defaultValue;
    }

    public static void GetValueFromSnapshot(this DataSnapshot snapshot, string childName, int defaultValue, out int savedValue)
    {
        bool notEmptyProfile = snapshot != null && snapshot.ChildrenCount > 1;
        savedValue = notEmptyProfile && snapshot.HasChild(childName) ? int.Parse(snapshot.Child(childName).Value.ToString()) : defaultValue;
    }

    public static void GetValueFromSnapshot(this DataSnapshot snapshot, string childName, string defaultValue, out string savedValue)
    {
        bool notEmptyProfile = snapshot != null && snapshot.ChildrenCount > 1;
        savedValue = notEmptyProfile && snapshot.HasChild(childName) ? snapshot.Child(childName).Value.ToString() : defaultValue;
    }

    public static void GetRawValueFromSnapshot<T>(this DataSnapshot snapshot, string childName, out T savedValue) where T : new()
    {
        bool notEmptyProfile = snapshot != null && snapshot.ChildrenCount > 1;

        if (notEmptyProfile)
        {
            string restoredData = snapshot.Child(childName).GetRawJsonValue();

            if (restoredData == null || restoredData.Length < 2)
                savedValue = new T();
            else
                savedValue = JsonUtility.FromJson<T>(restoredData);
        }
        else
        {
            savedValue = new T();
        }
    }

    public static void GetRawValueFromSnapshot<T>(this DataSnapshot snapshot, out T savedValue) where T : new()
    {
        bool notEmptyProfile = snapshot != null && snapshot.ChildrenCount > 1;

        if (notEmptyProfile)
        {
            string restoredData = snapshot.GetRawJsonValue();

            if (restoredData == null || restoredData.Length < 2)
                savedValue = new T();
            else
                savedValue = JsonUtility.FromJson<T>(restoredData);
        }
        else
        {
            savedValue = new T();
        }
    }
}
