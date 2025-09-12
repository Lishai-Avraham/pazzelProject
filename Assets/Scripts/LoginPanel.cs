using System;
using System.Threading.Tasks;
using TMPro;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;

public class LoginPanel : MonoBehaviour
{
  [SerializeField] private TMP_InputField usernameInput;
  [SerializeField] private TMP_InputField passwordInput;
  [SerializeField] private GameObject loginButton;
  [SerializeField] private GameObject SignupButton;
  [SerializeField] private GameObject ResetPasswordButton;
  [SerializeField] private ScreenManager screenManager;

  // [SerializeField] private TextMeshProUGUI errMsgText = default;
  int ModePanelIndex = 1;
  async void Start()
  {
    await UnityServices.InitializeAsync();
    if (AuthenticationService.Instance.IsSignedIn)
    {
      Debug.Log("Already signed in.");
      screenManager.ShowScreen(ModePanelIndex);
    }
    else
    {
      Debug.Log("Not signed in, ready for login.");
    }
  }

  public async void OnClickSignUp()
  {
    bool signupSuccess = await SignUpWithUsernamePasswordAsync(usernameInput.text, passwordInput.text);
    if (signupSuccess)
    {
      screenManager.ShowScreen(ModePanelIndex);
    }
    else
    {
      Debug.Log("Invalid username or password.");
    }
  }

  public async void OnClickSignIn()
  {
    Debug.Log("Singing in with username and password.");
    bool loginSuccess = await SignInWithUsernamePasswordAsync(usernameInput.text, passwordInput.text);
    if (loginSuccess)
    {
      screenManager.ShowScreen(ModePanelIndex);
    }
    else
    {
      Debug.Log("Invalid username or password.");
    }
  }

  public async void OnClickResetrPassword()
  {
    Debug.Log("Singing in with username and password.");
    // await UpdatePasswordAsync(currentPassword.text, newPassword.text);
  }

  private async Task<bool> SignUpWithUsernamePasswordAsync(string username, string password)
  {
    try
    {
      await AuthenticationService.Instance.SignUpWithUsernamePasswordAsync(username, password);
      Debug.Log("SignUp is successful.");
      return true;
    }
    catch (AuthenticationException ex)
    {
      // Compare error code to AuthenticationErrorCodes
      // Notify the player with the proper error message
      Debug.Log(ex.Message);
    }
    catch (RequestFailedException ex)
    {
      // Compare error code to CommonErrorCodes
      // Notify the player with the proper error message
      Debug.Log(ex.Message);
    }
    catch (Exception ex)
    {
      // Handle any other exceptions that may occur
      Debug.Log(ex.Message);
    }
    return false;
  }

  private async Task<bool> SignInWithUsernamePasswordAsync(string username, string password)
  {
    try
    {
      await AuthenticationService.Instance.SignInWithUsernamePasswordAsync(username, password);
      Debug.Log("SignIn is successful.");
      return true;
    }
    catch (AuthenticationException ex)
    {
      // Compare error code to AuthenticationErrorCodes
      // Notify the player with the proper error message
      Debug.Log(ex.Message);
      return false;
    }
    catch (RequestFailedException ex)
    {
      // Compare error code to CommonErrorCodes
      // Notify the player with the proper error message
      Debug.Log(ex.Message);
      return false;
    }
    catch (Exception ex)
    {
      // Handle any other exceptions that may occur
      Debug.Log(ex.Message);
      return false;
    }
  }

  private async Task UpdatePasswordAsync(string currentPassword, string newPassword)
  {
    try
    {
      await AuthenticationService.Instance.UpdatePasswordAsync(currentPassword, newPassword);
      Debug.Log("Password updated.");
    }
    catch (AuthenticationException ex)
    {
      // Compare error code to AuthenticationErrorCodes
      // Notify the player with the proper error message
      Debug.LogException(ex);
    }
    catch (RequestFailedException ex)
    {
      // Compare error code to CommonErrorCodes
      // Notify the player with the proper error message
      Debug.LogException(ex);
    }
  }
}
  