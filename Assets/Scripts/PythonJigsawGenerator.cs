using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json; 

public class PythonJigsawGenerator : MonoBehaviour
{
    private string serverUrl = "http://127.0.0.1:5000/cut_puzzle"; 

    // --- אין יותר PieceScaleFactor ידני! הכל אוטומטי ---

    public float FinalPieceWidth { get; private set; }
    public float FinalPieceHeight { get; private set; }

    [System.Serializable]
    public class PieceData
    {
        public int row;
        public int col;
        public string image_data; 
        public int width;
        public int height;
        // נתונים חדשים מהשרת
        public float scale_x; 
        public float scale_y;
    }

    [System.Serializable]
    public class ServerResponse
    {
        public string status;
        public List<PieceData> pieces;
    }

    public void RequestPieces(Texture2D sourceTexture, int rows, int cols, Transform gameHolder, GameObject piecePrefab, System.Action<List<Transform>> onComplete)
    {
        StartCoroutine(UploadAndGenerate(sourceTexture, rows, cols, gameHolder, piecePrefab, onComplete));
    }

    IEnumerator UploadAndGenerate(Texture2D texture, int rows, int cols, Transform gameHolder, GameObject piecePrefab, System.Action<List<Transform>> onComplete)
    {
        byte[] imageBytes = texture.EncodeToPNG();
        WWWForm form = new WWWForm();
        form.AddBinaryData("image", imageBytes, "puzzle.png", "image/png");
        form.AddField("rows", rows);
        form.AddField("cols", cols);

        using (UnityWebRequest www = UnityWebRequest.Post(serverUrl, form))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Python Error: " + www.error);
            }
            else
            {
                ProcessResponse(www.downloadHandler.text, texture, gameHolder, piecePrefab, rows, cols, onComplete);
            }
        }
    }

    void ProcessResponse(string json, Texture2D originalTexture, Transform gameHolder, GameObject piecePrefab, int totalRows, int totalCols, System.Action<List<Transform>> onComplete)
    {
        if (gameHolder == null) return;

        ServerResponse response = JsonConvert.DeserializeObject<ServerResponse>(json);
        List<Transform> newPieces = new List<Transform>(); 

        // חישוב גודל גריד (80% מהמסך)
        float screenHeight = Camera.main.orthographicSize * 2f;
        float screenWidth = screenHeight * Camera.main.aspect;

        float maxAllowedHeight = screenHeight * 0.8f;
        float maxAllowedWidth = screenWidth * 0.8f;

        float imageAspect = (float)originalTexture.width / originalTexture.height;
        float screenAspect = maxAllowedWidth / maxAllowedHeight;

        float targetTotalWidth, targetTotalHeight;

        if (imageAspect > screenAspect)
        {
            targetTotalWidth = maxAllowedWidth;
            targetTotalHeight = targetTotalWidth / imageAspect;
        }
        else
        {
            targetTotalHeight = maxAllowedHeight;
            targetTotalWidth = targetTotalHeight * imageAspect;
        }

        FinalPieceHeight = targetTotalHeight / totalRows;
        FinalPieceWidth = targetTotalWidth / totalCols;

        foreach (PieceData data in response.pieces)
        {
            byte[] imageBytes = System.Convert.FromBase64String(data.image_data);
            Texture2D pieceTex = new Texture2D(data.width, data.height);
            pieceTex.LoadImage(imageBytes);

            // --- תיקון PPU אוטומטי ---
            // משתמשים בנתון שהגיע מהפייתון (scale_x) כדי לדעת בדיוק כמה לנפח
            // אם פייתון אומר 1.7, נשתמש ב-1.7. אם 2.5, אז 2.5.
            float ppu = data.width / (FinalPieceWidth * data.scale_x);
            
            Sprite pieceSprite = Sprite.Create(pieceTex, new Rect(0, 0, pieceTex.width, pieceTex.height), new Vector2(0.5f, 0.5f), ppu);

            GameObject pieceObj = Instantiate(piecePrefab, gameHolder);
            pieceObj.name = $"Piece_{data.row}_{data.col}";

            if(pieceObj.GetComponent<MeshFilter>()) Destroy(pieceObj.GetComponent<MeshFilter>());
            if(pieceObj.GetComponent<MeshRenderer>()) Destroy(pieceObj.GetComponent<MeshRenderer>());
            
            SpriteRenderer sr = pieceObj.GetComponent<SpriteRenderer>();
            if (sr == null) sr = pieceObj.AddComponent<SpriteRenderer>();
            
            sr.sprite = pieceSprite;
            sr.sortingOrder = 10; 
            pieceObj.transform.localScale = Vector3.one; 

            if(pieceObj.GetComponent<PolygonCollider2D>()) Destroy(pieceObj.GetComponent<PolygonCollider2D>());
            pieceObj.AddComponent<PolygonCollider2D>();
            pieceObj.tag = "PuzzlePiece";

            float startX = -((totalCols-1) * FinalPieceWidth) / 2;
            float startY = ((totalRows-1) * FinalPieceHeight) / 2; 

            pieceObj.transform.localPosition = new Vector3(
                startX + (data.col * FinalPieceWidth), 
                startY - (data.row * FinalPieceHeight), 
                -5.0f 
            );
            
            newPieces.Add(pieceObj.transform);
        }

        if (onComplete != null) onComplete(newPieces);
    }
}