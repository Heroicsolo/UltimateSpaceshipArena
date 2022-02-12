using Firebase.Auth;
using Google;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public static class AuthController
{
    private static FirebaseAuth auth;

    private static string m_userId = "";
    private static string m_loginError = "";
    private static string m_deviceId = "";
    private static string m_email = "";
    private static string m_password = "";
    private static string authCode = "";

    private static bool m_signedUp = false;
    private static bool m_signingIn = false;
    private static bool m_signedIn = false;
    private static bool m_signInFailed = false;
    private static bool m_GP_initialized = false;
    private static bool m_playGamesSignInEnded = false;
    private static bool m_playGamesSignInSuccess = false;
    private static bool m_credentialsSaved = false;
    private static bool m_newProfile = false;

    public static Action OnLoginFailed;

    public static bool IsAuthorized = false;

    public static string UserID => m_userId;
    public static string DeviceID => m_deviceId;

    public static string Email => m_email;

    public static bool GooglePlayConnected => m_GP_initialized;
    public static bool CredentialsSaved => m_credentialsSaved;

    public static bool IsSigningIn => m_signingIn;

    public static void InitGP()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        Social.localUser.Authenticate((bool success) => {
            if( success )
            {
                authCode = PlayGamesPlatform.Instance.GetServerAuthCode();
                m_GP_initialized = true;
                (Social.Active as PlayGamesPlatform).LoadAchievements(InitAchievements);
            }
            else
            {
                m_GP_initialized = false;
            }
        });
#else
        m_GP_initialized = true;
#endif
    }

    private static void SignInViaPlayGames()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        SignInWithPlayGamesOnFirebase(authCode);
#else
        m_playGamesSignInEnded = true;
        m_playGamesSignInSuccess = false;
#endif
    }

    private static void OnCredentialsSaved()
    {
        m_credentialsSaved = true;
    }

    public static void SaveCredentials(string userName, bool onlyLocal = false, bool isNewAccount = false, bool isPlayGames = false)
    {
        if (!isPlayGames)
        {
            PlayerPrefs.SetString("email", m_email);
            PlayerPrefs.SetString("pass", m_password);
        }

        if (isNewAccount)
            AccountManager.SetUserName(userName);

        if (onlyLocal) { m_credentialsSaved = true; return; }

        AccountManager.SaveCredentials(m_email, isNewAccount, OnCredentialsSaved);
    }

    private static void SignInWithPlayGamesOnFirebase(string idToken)
    {
        Firebase.Auth.FirebaseAuth auth = Firebase.Auth.FirebaseAuth.DefaultInstance;
        Firebase.Auth.Credential credential =
            Firebase.Auth.PlayGamesAuthProvider.GetCredential(idToken);

        auth.SignInWithCredentialAsync(credential).ContinueWith(task =>
        {
            if (task.IsCanceled)
            {
                Debug.LogError("SignInWithCredentialAsync was canceled.");
                m_loginError = "Logging in was canceled.";
                m_signingIn = false;
                m_playGamesSignInSuccess = false;
                m_playGamesSignInEnded = true;
                return;
            }
            if (task.IsFaulted)
            {
                Debug.LogError("SignInWithCredentialAsync encountered an error: " + task.Exception);
                m_loginError = task.Exception.Message;
                m_signingIn = false;
                m_playGamesSignInSuccess = false;
                m_playGamesSignInEnded = true;
                return;
            }

            Firebase.Auth.FirebaseUser newUser = task.Result;
            Debug.LogFormat("User signed in successfully: {0} ({1})",
                newUser.DisplayName, newUser.UserId);

            m_email = newUser.Email;
            m_userId = newUser.UserId;
            AccountManager.GenerateUserName();
            m_password = "";

            m_signInFailed = false;
            m_signedIn = true;
            m_signingIn = false;
            m_playGamesSignInSuccess = true;
            m_playGamesSignInEnded = true;
        });
    }

    public static async void WaitForLoginFail()
    {
        while (!m_signInFailed)
        {
            await Task.Yield();
        }

        MessageBox.instance.Show(m_loginError);

        OnLoginFailed?.Invoke();
    }

    private static void SignInWithGoogleOnFirebase(string idToken)
    {
        Credential credential = GoogleAuthProvider.GetCredential(idToken, null);

        auth.SignInWithCredentialAsync(credential).ContinueWith(task =>
        {
            if (task.IsCanceled)
            {
                Debug.LogError("SignInWithGoogleOnFirebase was canceled.");
                m_loginError = "Logging in was canceled.";
                m_signInFailed = true;
                m_signingIn = false;
                return;
            }
            if (task.IsFaulted)
            {
                Debug.LogError("SignInWithGoogleOnFirebase encountered an error: " + task.Exception);
                m_loginError = task.Exception.Message;
                m_signInFailed = true;
                m_signingIn = false;
                return;
            }

            FirebaseUser newUser = task.Result;
            Debug.LogFormat("User signed in successfully: {0} ({1})", newUser.DisplayName, newUser.UserId);

            m_email = newUser.Email;
            m_userId = newUser.UserId;
            m_password = "";

            m_signInFailed = false;
            m_signedIn = true;
            m_signingIn = false;

            PlayerPrefs.SetString("googleId", idToken);
        });
    }

    public static void SignInWithGoogle()
    {
        m_signingIn = true;
        GoogleSignIn.DefaultInstance?.SignIn().ContinueWith(OnGoogleAuthFinished);
    }

    private static void OnGoogleAuthFinished(Task<GoogleSignInUser> task)
    {
        if (task.IsCompleted)
            SignInWithGoogleOnFirebase(task.Result.IdToken);
    }

    public static void SignIn(string email, string password)
    {
        m_signingIn = true;

        auth.SignInWithEmailAndPasswordAsync(email, password).ContinueWith(task =>
        {
            if (task.IsCanceled)
            {
                Debug.LogError("SignInWithEmailAndPasswordAsync was canceled.");
                m_loginError = "Logging in was canceled.";
                m_signInFailed = true;
                m_signingIn = false;
                return;
            }
            if (task.IsFaulted)
            {
                Debug.LogError("SignInWithEmailAndPasswordAsync encountered an error: " + task.Exception);
                m_loginError = task.Exception.Message;
                m_signInFailed = true;
                m_signingIn = false;
                return;
            }

            FirebaseUser newUser = task.Result;
            Debug.LogFormat("User signed in successfully: {0} ({1})", newUser.DisplayName, newUser.UserId);

            m_email = email;
            m_userId = newUser.UserId;
            m_password = password;

            m_signInFailed = false;
            m_signedIn = true;
            m_signingIn = false;
        });
    }

    public static void OnSecondLoginRegistered()
    {
        m_loginError = "Someone is already logged in your account. Game will be closed.";
        Launcher.instance.CloseGameOnError = true;
        m_signInFailed = true;
    }

    public static void SetEmailIfEmpty(string email)
    {
        if (m_email.Length < 2)
            m_email = email;
    }

    public static void SignOut()
    {
        auth.SignOut();
    }

    public static void Init()
    {
        auth = FirebaseAuth.DefaultInstance;

        m_deviceId = SystemInfo.deviceUniqueIdentifier;
    }

    public static bool TrySignIn()
    {
        m_email = PlayerPrefs.GetString("email", "");
        m_password = PlayerPrefs.GetString("pass", "");

        string googleToken = PlayerPrefs.GetString("googleId", "");

        if (googleToken.Length > 2)
        {
            m_signingIn = true;
            SignInWithGoogleOnFirebase(googleToken);
            return true;
        }
        else if (m_email.Length > 2)
        {
            WaitForLoginFail();
            m_signingIn = true;
            SignIn(m_email, m_password);
            return true;
        }

        return false;
    }

    public static void CreateAccount(string email, string password)
    {
        m_signingIn = true;

        auth.CreateUserWithEmailAndPasswordAsync(email, password).ContinueWith(task =>
        {
            if (task.IsCanceled)
            {
                Debug.LogError("CreateUserWithEmailAndPasswordAsync was canceled.");
                m_loginError = "Account creation was canceled.";
                m_signInFailed = true;
                m_signingIn = false;
                return;
            }
            if (task.IsFaulted)
            {
                Debug.LogError("CreateUserWithEmailAndPasswordAsync encountered an error: " + task.Exception);
                m_loginError = "Account with this email already exists";
                m_signInFailed = true;
                m_signingIn = false;
                return;
            }

            // Firebase user has been created.
            FirebaseUser newUser = task.Result;
            Debug.LogFormat("Firebase user created successfully: {0} ({1})", newUser.DisplayName, newUser.UserId);

            m_email = email;
            m_password = password;
            m_userId = newUser.UserId;

            m_newProfile = true;
            m_signInFailed = false;
            m_signedIn = true;
            m_signingIn = false;
        });
    }

    public static async void SignIn()
    {
        m_signedIn = false;

        bool emailSigningIn = TrySignIn();

        if (!emailSigningIn)
        {
            SignInViaPlayGames();

            while (!m_playGamesSignInEnded)
            {
                await Task.Yield();
            }

            if (!m_playGamesSignInSuccess)
            {
                m_signingIn = false;
                Launcher.instance.OpenLoginScreen();
            }
        }

        while (!m_signedIn)
        {
            await Task.Yield();
        }

        IsAuthorized = true;

        SaveCredentials(AccountManager.UserName, false, m_signedUp, m_playGamesSignInSuccess);
    }
}
