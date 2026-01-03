using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System;

[System.Serializable]
public class PieceData
{
    public string image;
    public int row;
    public int col;
    public float scale_x;
    public float scale_y;
}

[System.Serializable]
public class PieceListWrapper
{
    public List<PieceData> pieces;
}

public class PythonJigsawGenerator : MonoBehaviour
{
    [Header("Server Settings")]
    [SerializeField] private string serverUrl = "http://127.0.0.1:5000/cut_puzzle";

    public float FinalPieceWidth { get; private set; }
    public float FinalPieceHeight { get; private set; }

    public void RequestPieces(Texture2D originalImage, int rows, int cols, Transform parent, GameObject prefab, Action<List<Transform>> onComplete)
    {
        StartCoroutine(UploadAndGenerate(originalImage, rows, cols, parent, prefab, onComplete));
    }

    IEnumerator UploadAndGenerate(Texture2D texture, int rows, int cols, Transform parent, GameObject prefab, Action<List<Transform>> onComplete)
    {
        byte[] imageBytes = texture.EncodeToJPG();

        WWWForm form = new WWWForm();
        form.AddBinaryData("image", imageBytes, "puzzle.jpg", "image/jpeg");
        form.AddField("rows", rows);
        form.AddField("cols", cols);

        using (UnityWebRequest www = UnityWebRequest.Post(serverUrl, form))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Python Error: " + www.error + "\nResponse: " + www.downloadHandler.text);
            }
            else
            {
                string jsonResponse = www.downloadHandler.text;
                PieceListWrapper wrapper = JsonUtility.FromJson<PieceListWrapper>(jsonResponse);
                
                if (wrapper == null || wrapper.pieces == null)
                {
                    Debug.LogError("JSON Error. Response: " + jsonResponse);
                    yield break;
                }

                List<Transform> createdPieces = new List<Transform>();

                float orthoHeight = Camera.main.orthographicSize * 2f;
                float targetTotalHeight = orthoHeight * 0.7f;
                
                float baseHeight = targetTotalHeight / rows;
                float aspect = (float)texture.width / texture.height;
                float baseWidth = baseHeight * aspect;

                FinalPieceWidth = baseWidth;
                FinalPieceHeight = baseHeight;

                foreach (PieceData data in wrapper.pieces)
                {
                    GameObject pieceObj = Instantiate(prefab, parent);
                    pieceObj.name = $"Piece_{data.row}_{data.col}";

                    // --- תיקון נראות 1: איפוס מיקום Z ---
                    pieceObj.transform.localPosition = new Vector3(pieceObj.transform.localPosition.x, pieceObj.transform.localPosition.y, 0f);

                    byte[] decodedBytes = Convert.FromBase64String(data.image);
                    Texture2D pieceTex = new Texture2D(2, 2);
                    pieceTex.LoadImage(decodedBytes);

                    Sprite sprite = Sprite.Create(pieceTex, new Rect(0, 0, pieceTex.width, pieceTex.height), new Vector2(0.5f, 0.5f));
                    SpriteRenderer sr = pieceObj.GetComponent<SpriteRenderer>();
                    sr.sprite = sprite;

                    // --- תיקון נראות 2: העלאת סדר השכבה ---
                    // זה מבטיח שהחתיכה תצויר מעל הרקע
                    sr.sortingOrder = 10; 

                    float scaleX = baseWidth * data.scale_x;
                    float scaleY = baseHeight * data.scale_y;

                    float ppu = 100f; 
                    float originalWorldWidth = pieceTex.width / ppu;
                    float originalWorldHeight = pieceTex.height / ppu;

                    pieceObj.transform.localScale = new Vector3(
                        scaleX / originalWorldWidth,
                        scaleY / originalWorldHeight,
                        1f
                    );

                    if (pieceObj.GetComponent<BoxCollider2D>() == null)
                         pieceObj.AddComponent<BoxCollider2D>();

                    createdPieces.Add(pieceObj.transform);
                }

                onComplete?.Invoke(createdPieces);
            }
        }
    }
}