using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class PuzzleAPIManager : MonoBehaviour
{
    public GameManager gameManager; // Assign this in the inspector

    public void CreateJigsawFromAPI(Texture2D image)
    {
        StartCoroutine(SendImageToAPI(image));
    }

    private IEnumerator SendImageToAPI(Texture2D image)
    {
        byte[] imageBytes = image.EncodeToPNG();

        WWWForm form = new WWWForm();
        form.AddBinaryData("file", imageBytes, "puzzle.png", "image/png");

        using UnityWebRequest www = UnityWebRequest.Post("http://127.0.0.1:8000/create_jigsaw", form);
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError(www.error);
            yield break;
        }

        string json = www.downloadHandler.text;
        Debug.Log("API raw response: " + json);
        
        var puzzleData = JsonUtility.FromJson<JigsawAPIResponse>(json);

        // Send data to GameManager
        gameManager.CreateJigsawPiecesFromAPI(puzzleData);
    }
}
