using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ApplicationQuitHandler
{
    public static bool AppIsQuitting { get; private set; }

    [RuntimeInitializeOnLoadMethod]
    private static void OnAppStart()
    {
        Application.quitting += () => AppIsQuitting = true;
    }
}
