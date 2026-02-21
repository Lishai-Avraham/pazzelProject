using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using TMPro;


public class ImageUploader : MonoBehaviour
{
    public RawImage previewImage; // optional, to preview the image before puzzle
    [SerializeField] private int difficulty = 2;
    [SerializeField] private Transform gameHolder;
    // [SerializeField] private Transform piecePrefab;
    [SerializeField] private GameObject piecePrefab; 
    [SerializeField] private Button uploadButton;
    [SerializeField] private Button returnButton;
    [SerializeField] private ScreenManager screenManager;
    [SerializeField] private GameObject playAgainButton;
    [SerializeField] private GameObject emoji;
    [SerializeField] private PythonJigsawGenerator pythonGenerator;
    [Header("Timer UI")]
    [SerializeField] private TextMeshProUGUI timerText;
    private float currentElapsedTime;
    private bool isTimerRunning = false;

    private Texture2D selectedTexture;
    private List<Transform> pieces;
    private Vector2Int dimensions;
    private float width;
    private float height;
    private Transform draggingPiece = null;
    private Vector3 offset;
    private int piecesCorrect;
    int ModePanelIndex = 1;
    private bool inlevels = true;
    private bool isGameActive = false;
    private float startTime;
    private double timeTaken;

    private void Start()
    {
        if(timerText != null) timerText.text = "00:00";
        timerText.gameObject.SetActive(false);
        if (Settings.Instance != null)
        {
            difficulty = Settings.Instance.pieces;
        }
    }

    public void PickImage()
    {
        // Opens Android's gallery
        NativeGallery.GetImageFromGallery((path) =>
        {
            if (path != null)
            {
                // Load the selected image
                Texture2D texture = NativeGallery.LoadImageAtPath(path, 1024, false);                if (texture == null)
                if (texture == null)
                {
                    Debug.LogError("Couldn't load texture from " + path);
                    return;
                }

                // Preview it (optional)
                if (previewImage != null)
                    previewImage.texture = texture;
                timerText.gameObject.SetActive(true);
                // Send to your jigsaw function
                StartGame(texture);
            }
        }, "Select an image", "image/*");
    }

    public void StartGame(Texture2D jigsawTexture)
    {
        Debug.Log("StartGame function running.");
        if (Settings.Instance != null)
        {
            difficulty = Settings.Instance.pieces;
            Debug.Log($"Difficulty loaded from Settings: {difficulty}");
        }
        isGameActive = true;
        // Hide the UI
        uploadButton.gameObject.SetActive(false);
        inlevels = false;
        
        // Reset the holder position so pieces don't spawn off-screen
        gameHolder.position = Vector3.zero;
        gameHolder.rotation = Quaternion.identity;
        gameHolder.localScale = Vector3.one;

        pieces = new List<Transform>();
        piecesCorrect = 0;
        selectedTexture = jigsawTexture;

        foreach(Transform child in gameHolder) Destroy(child.gameObject);

        int calculated = (int)Mathf.Sqrt(difficulty);
        int rows = Mathf.Max(2, calculated); 
        int cols = rows;
        dimensions = new Vector2Int(cols, rows);

        pythonGenerator.RequestPieces(jigsawTexture, rows, cols, gameHolder, piecePrefab, (generatedPieces) => {
            
            // If the user went back to the menu (inlevels is true) while we were waiting,
            // destroy the new pieces immediately and stop.
            if (isGameActive == false) 
            {
                foreach(var p in generatedPieces) Destroy(p.gameObject);
                return; 
            }
            this.pieces = generatedPieces;
            
            width = pythonGenerator.FinalPieceWidth;
            height = pythonGenerator.FinalPieceHeight;
            
            float totalPuzzleWidth = width * cols;
            float totalPuzzleHeight = height * rows;

            UpdateBorder(totalPuzzleWidth, totalPuzzleHeight);  
            
            Scatter(); 
            isTimerRunning = true; 
            if(timerText != null) timerText.gameObject.SetActive(true);
            startTime = Time.time;      
        });
        
    }

    // Place the pieces randomly in the visible area.
     void Scatter()
    {
        // חישוב גבולות המסך כדי שהחלקים לא יצאו החוצה
        float screenHeight = Camera.main.orthographicSize * 2f;
        float screenWidth = screenHeight * Camera.main.aspect;

        float margin = width; // משאירים מקום של חתיכה אחת מהקצה
        float safeX = (screenWidth / 2) - margin;
        float safeY = (screenHeight / 2) - margin;

        foreach (Transform piece in pieces)
        {
            float randomX = UnityEngine.Random.Range(-safeX, safeX);
            float randomY = UnityEngine.Random.Range(-safeY, safeY);

            // Z=-5 מבטיח שהחלקים יהיו מעל הרקע (שהוא בדרך כלל ב-0)
            piece.localPosition = new Vector3(randomX, randomY, -5.0f);
            
            Collider2D col = piece.GetComponent<Collider2D>();
            if(col != null) col.enabled = true;
        }
    }
    // Update the border to fit the chosen puzzle.
    public void UpdateBorder(float totalWidth, float totalHeight)
    {
        LineRenderer lineRenderer = gameHolder.GetComponent<LineRenderer>();
        if (lineRenderer == null) {
            lineRenderer = gameHolder.gameObject.AddComponent<LineRenderer>();
      }

      // הגדרת חומר בסיסי כדי שהקו ייראה
      if (lineRenderer.material == null) {
          lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
          lineRenderer.startColor = Color.white;
          lineRenderer.endColor = Color.white;
      }

      float halfWidth = totalWidth / 2f;
      float halfHeight = totalHeight / 2f;
      float borderZ = -1.0f; 

      lineRenderer.positionCount = 4;
      lineRenderer.loop = true;
      lineRenderer.useWorldSpace = false;
      
      // --- התיקון: וידוא שהקו מצויר מעל הכל ---
      lineRenderer.sortingOrder = 20; // מספר גבוה כדי להיות מעל הרקע והחלקים

      lineRenderer.SetPosition(0, new Vector3(-halfWidth, halfHeight, borderZ));
      lineRenderer.SetPosition(1, new Vector3(halfWidth, halfHeight, borderZ));
      lineRenderer.SetPosition(2, new Vector3(halfWidth, -halfHeight, borderZ));
      lineRenderer.SetPosition(3, new Vector3(-halfWidth, -halfHeight, borderZ));

      lineRenderer.startWidth = 0.15f;
      lineRenderer.endWidth = 0.15f;
      lineRenderer.enabled = true;
    }

    // Update is called once per frame
    void Update()
    {
        if (isTimerRunning)
        {
            currentElapsedTime += Time.deltaTime;
            UpdateTimerDisplay();
        }
        // Debug.Log("Update function running.");
        if (Input.GetMouseButtonDown(0))
        {
        RaycastHit2D hit = Physics2D.Raycast(Camera.main.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);
        if (hit)
        {
            // Everything is moveable, so we don't need to check it's a Piece.
            draggingPiece = hit.transform;
            offset = draggingPiece.position - Camera.main.ScreenToWorldPoint(Input.mousePosition);
            offset += Vector3.back;
        }
        }

        // When we release the mouse button stop dragging.
        if (draggingPiece && Input.GetMouseButtonUp(0))
        {
        SnapAndDisableIfCorrect();
        draggingPiece.position += Vector3.forward;
        draggingPiece = null;
        }

        // Set the dragged piece position to the position of the mouse.
        if (draggingPiece)
        {
        Vector3 newPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        // newPosition.z = draggingPiece.position.z;
        newPosition += offset;
        draggingPiece.position = newPosition;
        }
    }

    private void UpdateTimerDisplay()
    {
        if (timerText == null) return;

        int minutes = Mathf.FloorToInt(currentElapsedTime / 60);
        int seconds = Mathf.FloorToInt(currentElapsedTime % 60);
        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    private void SnapAndDisableIfCorrect()
    {
      if (draggingPiece == null || pieces.Count == 0) return;

      int pieceIndex = pieces.IndexOf(draggingPiece);
      int col = pieceIndex % dimensions.x;
      int row = pieceIndex / dimensions.x;

      // חישוב המיקום הנכון - מבוסס על גודל הגריד ומרכוז סביב ה-0,0
      // הנוסחה הזו מניחה שה-GameHolder נמצא ב-0,0,0
      float startX = -((dimensions.x * width) / 2) + (width / 2);
      float startY = ((dimensions.y * height) / 2) - (height / 2);

      float targetX = startX + (col * width);
      float targetY = startY - (row * height);

      Vector2 targetPosition = new Vector2(targetX, targetY);

      if (Vector2.Distance(draggingPiece.localPosition, targetPosition) < (width / 2))
      {
          // --- תיקון: Z=0 כשהחלק במקום, כדי שיהיה מתחת לחלקים שעדיין גוררים ---
          draggingPiece.localPosition = new Vector3(targetX, targetY, 0f); 
          
          Collider2D col2D = draggingPiece.GetComponent<Collider2D>();
          if(col2D != null) col2D.enabled = false;

          piecesCorrect++;
          if (piecesCorrect == pieces.Count)
          {
            isTimerRunning = false;
            timeTaken = Time.time - startTime;
            string difficultyName = "easy";
            if (Settings.Instance != null && !string.IsNullOrEmpty(Settings.Instance.difficulty))
            {
                difficultyName = Settings.Instance.difficulty;
                difficultyName = difficultyName.ToLower();
                Debug.Log($"Difficulty from Settings: {difficultyName}");
            }
            else
            {
                Debug.LogWarning("Settings.Instance is missing! Defaulting difficulty to 'easy'.");
            }

            // 2. Submit Score Safely
            if (LeaderboardManager.Instance != null)
            {
                LeaderboardManager.Instance.SubmitScore(difficultyName, timeTaken);
            }
            else
            {
                Debug.LogWarning("LeaderboardManager.Instance is missing! Score could not be submitted.");
            }
            playAgainButton.SetActive(true);
            emoji.SetActive(true);
          }
      }
    }

    public void RestartGame()
    {
        ResetTimerUI();
        Debug.Log("RestartGame function running.");
        // Destroy all the puzzle pieces.
        foreach (Transform piece in pieces)
        {
        Destroy(piece.gameObject);
        }
        pieces.Clear();
        // Hide the outline
        gameHolder.GetComponent<LineRenderer>().enabled = false;
        // Show the level select UI.
        playAgainButton.SetActive(false);
        emoji.SetActive(false);
        uploadButton.gameObject.SetActive(true);
        inlevels = true;
    }
    public void OnClickReturn()
    {
        ResetTimerUI();
        Debug.Log("OnClickReturn function running.");
        isGameActive = false;
        if (inlevels == true)
        {
        Debug.LogWarning("see uploadButton as true.");
        uploadButton.gameObject.SetActive(false);
        inlevels = false;
        screenManager.ShowScreen(ModePanelIndex);
        }
        else
        {
        foreach (Transform piece in pieces)
        {
            Destroy(piece.gameObject);
        }
        pieces.Clear();
        // Hide the outline
        gameHolder.GetComponent<LineRenderer>().enabled = false;
        // Show the level select UI.
        playAgainButton.SetActive(false);
        emoji.SetActive(false);
        uploadButton.gameObject.SetActive(true);
        inlevels = true;
        } 
    }

    private void ResetTimerUI()
    {
        isTimerRunning = false;
        currentElapsedTime = 0f;
        if(timerText != null) 
        {
            timerText.text = "00:00";
            timerText.gameObject.SetActive(false); // Optional: hide when not in game
        }
    }
}


