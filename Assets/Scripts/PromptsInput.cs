using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections;
using UnityEngine.Networking;
using Newtonsoft.Json;
using System.Text;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

public class PromptsInput : MonoBehaviour
{
    [SerializeField] private TMP_InputField messageInput;
    [SerializeField] private Button sendButton;
    [SerializeField] private Button returnButton;
    [SerializeField] private ScreenManager screenManager;
    [SerializeField] private PythonJigsawGenerator pythonGenerator;
    [SerializeField] private Transform gameHolder;
    [SerializeField] private GameObject piecePrefab; 
    [SerializeField] private GameObject playAgainButton;
    [SerializeField] private GameObject emoji;
    [Header("Timer UI")]
    [SerializeField] private TextMeshProUGUI timerText;
    [Header("Audio & Effects")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip snapSound;
    [SerializeField] private AudioClip winSound;
    private float currentElapsedTime;
    private bool isTimerRunning = false;


    private List<Transform> pieces;
    private Vector2Int dimensions;
    private float width;
    private float height;
    private Transform draggingPiece = null;
    private Vector3 offset;
    private int piecesCorrect;

    int ModePanelIndex = 1;
    int difficulty = 4; 
    bool ingame = false;
    private bool isGameActive = false;
    private float startTime;
    private double timeTaken;

    void Start()
    {
        sendButton.onClick.AddListener(OnSendClicked);
        if(timerText != null) timerText.text = "00:00";
        timerText.gameObject.SetActive(false);
        if (Settings.Instance != null)
        {
            difficulty = Settings.Instance.pieces;
        }
        bool musicOn = Settings.Instance.isMusicOn;
    }

    // public void OnSendClicked()
    // {
    //     string prompt = messageInput.text;
    //     if (string.IsNullOrEmpty(prompt))
    //     {
    //         Debug.Log("Empty input");
    //         return;
    //     }

        
    //     sendButton.interactable = false;
    //     sendButton.gameObject.SetActive(false);
    //     messageInput.gameObject.SetActive(false);
        
    //     StartCoroutine(GenerateImagePollinations(prompt));
    // }
    public void OnSendClicked()
    {
        string prompt = messageInput.text;
        if (string.IsNullOrEmpty(prompt))
        {
            Debug.Log("Empty input");
            return;
        }

        sendButton.interactable = false;
        sendButton.gameObject.SetActive(false);
        messageInput.gameObject.SetActive(false);
        timerText.gameObject.SetActive(true);
        
        // CHANGE THIS LINE to match your new function name:
        StartCoroutine(GenerateImageFromLocalSD(prompt)); 
    }
    // IEnumerator GenerateImagePollinations(string prompt)
    // {
    //     string safePrompt = UnityWebRequest.EscapeURL(prompt);

    //     int randomSeed = Random.Range(1, 999);

    //     string url = "https://image.pollinations.ai/prompt/" + safePrompt + 
    //                  "?model=turbo" + 
    //                  "&seed=" + randomSeed + 
    //                  "&width=1024&height=1024&nologo=true";

    //     Debug.Log("Generated URL: " + url);

    //     using (UnityWebRequest uwr = UnityWebRequestTexture.GetTexture(url))
    //     {
    //         yield return uwr.SendWebRequest();

    //         if (uwr.result == UnityWebRequest.Result.Success)
    //         {
    //             Texture2D tex = DownloadHandlerTexture.GetContent(uwr);
                
    //             if (tex != null)
    //             {
    //                 tex.name = "AI_" + prompt;
    //                 StartGame(tex);
    //                 messageInput.text = "";
    //             }
    //         }
    //         else
    //         {
    //             Debug.LogError("Error: " + uwr.error + " | Server: " + uwr.downloadHandler.text);
    //         }
    //     }
        
    //     sendButton.interactable = true;
    // }
    // IEnumerator GenerateImagePollinations(string prompt)
    // {
    //     string myApiKey = "sk_4qNn1XYTX4dFNXxSNy2pEnFAs52ffmI2"; // Replace with your actual key from enter.pollinations.ai
    //     string safePrompt = UnityWebRequest.EscapeURL(prompt);
    //     int randomSeed = Random.Range(1, 99999);

    //     // Using the validated 'flux' model we found in the previous error
    //     string url = $"https://gen.pollinations.ai/image/{safePrompt}?model=nanobanana&seed={randomSeed}&width=256&height=256&nologo=true";
    //     Debug.Log("Generated URL: " + url);
    //     using (UnityWebRequest uwr = UnityWebRequestTexture.GetTexture(url))
    //     {
    //         // ADD THIS LINE for the 401 error:
    //         uwr.SetRequestHeader("Authorization", "Bearer " + myApiKey);

    //         yield return uwr.SendWebRequest();

    //         if (uwr.result == UnityWebRequest.Result.Success)
    //         {
    //             Texture2D tex = DownloadHandlerTexture.GetContent(uwr);
    //             if (tex != null)
    //             {
    //                 tex.name = "AI_" + prompt;
    //                 StartGame(tex);
    //                 messageInput.text = "";
    //             }
    //         }
    //         else
    //         {
    //             Debug.LogError($"Error {uwr.responseCode}: {uwr.error}");
    //             // If you get a 403 here, it means your 'Pollen' balance is empty!
    //         }
    //     }
        
    //     sendButton.interactable = true;
    //     sendButton.gameObject.SetActive(true);
    // }

    IEnumerator GenerateImageFromLocalSD(string prompt)
    {
        string url = "http://132.68.60.38:7860/sdapi/v1/txt2img";

        // Create the payload object (matches your Python example)
        var payload = new
        {
            prompt = prompt,
            negative_prompt = "lowres, blurry, jpeg artifacts",
            steps = 25,
            cfg_scale = 7,
            width = 512,
            height = 512,
            sampler_name = "DPM++ 2M Karras",
            seed = -1
        };

        string jsonPayload = JsonConvert.SerializeObject(payload);
        
        using (UnityWebRequest uwr = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
            uwr.uploadHandler = new UploadHandlerRaw(bodyRaw);
            uwr.downloadHandler = new DownloadHandlerBuffer();
            uwr.SetRequestHeader("Content-Type", "application/json");

            yield return uwr.SendWebRequest();

            if (uwr.result == UnityWebRequest.Result.Success)
            {
                // Parse the JSON response
                var responseData = JObject.Parse(uwr.downloadHandler.text);
                // SD API returns an array of images in base64
                string base64String = responseData["images"][0].ToString();

                // Handle potential data:image/png;base64, prefix
                if (base64String.Contains(","))
                {
                    base64String = base64String.Split(',')[1];
                }

                byte[] imageBytes = System.Convert.FromBase64String(base64String);
                
                // Create Texture
                Texture2D tex = new Texture2D(2, 2);
                if (tex.LoadImage(imageBytes))
                {
                    tex.name = "SD_" + prompt;
                    StartGame(tex);
                    messageInput.text = "";
                }
            }
            else
            {
                Debug.LogError($"SD Error: {uwr.error} - {uwr.downloadHandler.text}");
            }
        }

    }

    public void StartGame(Texture2D jigsawTexture)
    {
        Debug.Log("StartGame Running...");
        if (Settings.Instance != null)
        {
            difficulty = Settings.Instance.pieces;
            Debug.Log($"Difficulty loaded from Settings: {difficulty}");
        }
        isGameActive = true;
        gameHolder.position = Vector3.zero;
        gameHolder.rotation = Quaternion.identity;
        gameHolder.localScale = Vector3.one;
        
        pieces = new List<Transform>(); 
        piecesCorrect = 0;

        foreach(Transform child in gameHolder) Destroy(child.gameObject);

        int calculated = (int)Mathf.Sqrt(difficulty);
        int rows = Mathf.Max(2, calculated); 
        int cols = rows;
        
        dimensions = new Vector2Int(cols, rows);
        ingame = true;
        pythonGenerator.RequestPieces(jigsawTexture, rows, cols, gameHolder, piecePrefab, (generatedPieces) => {
            if (isGameActive == false) 
            {
                // We are back in the menu! Destroy these new pieces immediately.
                foreach(var p in generatedPieces) 
                {
                    Destroy(p.gameObject);
                }
                return; // Stop the function here
            }
            this.pieces = generatedPieces;
            
            width = pythonGenerator.FinalPieceWidth;
            height = pythonGenerator.FinalPieceHeight;
            
            float totalPuzzleWidth = width * cols;
            float totalPuzzleHeight = height * rows;

            UpdateBorder(totalPuzzleWidth, totalPuzzleHeight);  
            Scatter();       
        });
        currentElapsedTime = 0f;
        isTimerRunning = true; 
        if(timerText != null) timerText.gameObject.SetActive(true);
        startTime = Time.time;
    }


    public Vector2Int GetDimensions(Texture2D jigsawTexture, int difficulty)
    {
        Debug.Log("GetDimensions function running.");
        Vector2Int dimensions = Vector2Int.zero;
        // Difficulty is the number of pieces on the smallest texture dimension.
        // This helps ensure the pieces are as square as possible.
        if (jigsawTexture.width < jigsawTexture.height)
        {
        dimensions.x = difficulty;
        dimensions.y = (difficulty * jigsawTexture.height) / jigsawTexture.width;
        }
        else
        {
        dimensions.x = (difficulty * jigsawTexture.width) / jigsawTexture.height;
        dimensions.y = difficulty;
        }
        return dimensions;
    }

    void Scatter()
    {
        // חישוב גבולות המסך כדי שהחלקים לא יצאו החוצה
        float screenHeight = Camera.main.orthographicSize * 2f;
        float screenWidth = screenHeight * Camera.main.aspect;

        float margin = 1.0f; // רווח מהקצה
        float safeX = (screenWidth / 2) - margin;
        float safeY = (screenHeight / 2) - margin;

        foreach (Transform piece in pieces)
        {
            float randomX = UnityEngine.Random.Range(-safeX, safeX);
            float randomY = UnityEngine.Random.Range(-safeY, safeY);

            piece.localPosition = new Vector3(randomX, randomY, -5.0f);
            
            Collider2D col = piece.GetComponent<Collider2D>();
            if(col != null) col.enabled = true;
        }
    }

    public void UpdateBorder(float totalWidth, float totalHeight)
    {
        LineRenderer lineRenderer = gameHolder.GetComponent<LineRenderer>();
        if (lineRenderer == null) return;

        float halfWidth = totalWidth / 2f;
        float halfHeight = totalHeight / 2f;
        float borderZ = 0.0f; // שמים את המסגרת ברקע (0)

        lineRenderer.positionCount = 4;
        lineRenderer.loop = true;

        // סדר הנקודות: שמאל-למעלה, ימין-למעלה, ימין-למטה, שמאל-למטה
        lineRenderer.SetPosition(0, new Vector3(-halfWidth, halfHeight, borderZ));
        lineRenderer.SetPosition(1, new Vector3(halfWidth, halfHeight, borderZ));
        lineRenderer.SetPosition(2, new Vector3(halfWidth, -halfHeight, borderZ));
        lineRenderer.SetPosition(3, new Vector3(-halfWidth, -halfHeight, borderZ));

        lineRenderer.startWidth = 0.1f;
        lineRenderer.endWidth = 0.1f;
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
        // Safety check
        if (draggingPiece == null || pieces.Count == 0) return;

        int pieceIndex = pieces.IndexOf(draggingPiece);

        // This math works now because 'dimensions' is set in StartGame
        int col = pieceIndex % dimensions.x;
        int row = pieceIndex / dimensions.x;

        // This math works now because 'width' and 'height' are set in StartGame
        // Note: We use the same grid logic as the Python generator (roughly centered on 0,0)
        float targetX = (-(dimensions.x - 1) * width) / 2 + (col * width);
        float puzzleTopBorder = (dimensions.y * height) / 2;
        float firstRowCenter = puzzleTopBorder - (height / 2);
        float targetY = firstRowCenter - (row * height);
        Vector2 targetPosition = new Vector2(targetX, targetY);

        // Check distance
        if (Vector2.Distance(draggingPiece.localPosition, targetPosition) < (width / 2))
        {
            // Snap
            draggingPiece.localPosition = targetPosition;

            // --- FIX 3: Use generic Collider2D (Works for Box OR Polygon) ---
            Collider2D col2D = draggingPiece.GetComponent<Collider2D>();
            if(col2D != null) col2D.enabled = false;
            // ---------------------------------------------------------------
            if (Settings.Instance.isSfxOn && sfxSource != null && snapSound != null)
            {
                sfxSource.PlayOneShot(snapSound);
            }

            piecesCorrect++;
            if (piecesCorrect == pieces.Count)
            {
                isTimerRunning = false;
                timeTaken = Time.time - startTime;
                if (Settings.Instance.isSfxOn && sfxSource != null && winSound != null)
                {
                    sfxSource.PlayOneShot(winSound);
                }
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
        timerText.gameObject.SetActive(false);
        sendButton.interactable = true;
        sendButton.gameObject.SetActive(true);
        messageInput.gameObject.SetActive(true);
        ingame = false;
    }
    public void OnClickReturn()
    {
        ResetTimerUI();
        isGameActive = false;
        if (ingame)
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
            timerText.gameObject.SetActive(false);
            ingame = false;
            sendButton.interactable = true;
            sendButton.gameObject.SetActive(true);
            messageInput.gameObject.SetActive(true);
        }
        else
        {
            Debug.LogWarning("see levelSelectPanel as true.");
            ingame = false;
            screenManager.ShowScreen(ModePanelIndex);
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
