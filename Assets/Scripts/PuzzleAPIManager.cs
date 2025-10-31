using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;


[System.Serializable]
public class JigsawAPIResponse
{
    public int piecesHeight;
    public int piecesWidth;
    public int totalPieces;
    public List<JigsawPieceData> pieces;
}

public class PuzzleAPIManager : MonoBehaviour
{
    public string apiURL = "http://127.0.0.1:8000/create_jigsaw";

    public IEnumerator UploadImage(Texture2D imageTexture, int rows, int cols, System.Action<JigsawAPIResponse> onComplete)
    {
        byte[] imageBytes = imageTexture.EncodeToPNG();
        WWWForm form = new WWWForm();
        form.AddBinaryData("file", imageBytes, "upload.png", "image/png");
        form.AddField("piecesHeight", rows);
        form.AddField("piecesWidth", cols);

        using (UnityWebRequest www = UnityWebRequest.Post(apiURL, form))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                var json = www.downloadHandler.text;
                var response = JsonConvert.DeserializeObject<JigsawAPIResponse>(json);
                onComplete?.Invoke(response);
            }
            else
            {
                Debug.LogError($"Upload failed: {www.error}");
            }
        }
    }
}
