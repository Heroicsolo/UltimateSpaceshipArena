using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MathHelper
{
    private static System.Random rng = new System.Random();

    public static string ToRoundedString(this float _value, int _digits = 1)
    {
        string zeros = new string('0', _digits);
        return Mathf.Approximately(_value, Mathf.Floor(_value)) ? Mathf.FloorToInt(_value).ToString() : _value.ToString("0." + zeros);
    }

    public static Vector3 CubeBezier3(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float r = 1f - t;
        float f0 = r * r * r;
        float f1 = r * r * t * 3;
        float f2 = r * t * t * 3;
        float f3 = t * t * t;
        return f0*p0 + f1*p1 + f2*p2 + f3*p3;
    }

    public static void AddUnique<T>( this IList<T> self, IEnumerable<T> items )
    {
        foreach( var item in items )
            if(!self.Contains(item))
                self.Add(item);
    }

    public static void Shuffle<T>(this IList<T> list)  
    {  
        int n = list.Count;  

        while (n > 1) {  
            n--;  
            int k = rng.Next(n + 1);  
            T value = list[k];  
            list[k] = list[n];  
            list[n] = value;  
        }  
    }

    public static void Shuffle<T>(this List<T> list)  
    {  
        int n = list.Count;  

        while (n > 1) {  
            n--;  
            int k = rng.Next(n + 1);  
            T value = list[k];  
            list[k] = list[n];  
            list[n] = value;  
        }  
    }

    public static float Distance(this Transform from, Transform to)
    {
        return (from.position - to.position).magnitude;
    }

    public static float Distance(this Vector3 from, Vector3 to)
    {
        return (from - to).magnitude;
    }

    public static T GetRandomElement<T>(this List<T> list)
    {
        return list[rng.Next(list.Count)];
    }
}
