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

    void Start()
    {
        sendButton.onClick.AddListener(OnSendClicked);
        if (Settings.Instance != null)
        {
            difficulty = Settings.Instance.pieces;
        }
        bool musicOn = Settings.Instance.isMusicOn;
    }

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
        
        StartCoroutine(GenerateImagePollinations(prompt));
    }

    IEnumerator GenerateImagePollinations(string prompt)
    {
        string safePrompt = UnityWebRequest.EscapeURL(prompt);

        int randomSeed = Random.Range(1, 999);

        string url = "https://image.pollinations.ai/prompt/" + safePrompt + 
                     "?model=turbo" + 
                     "&seed=" + randomSeed + 
                     "&width=1024&height=1024&nologo=true";

        Debug.Log("Generated URL: " + url);

        using (UnityWebRequest uwr = UnityWebRequestTexture.GetTexture(url))
        {
            yield return uwr.SendWebRequest();

            if (uwr.result == UnityWebRequest.Result.Success)
            {
                Texture2D tex = DownloadHandlerTexture.GetContent(uwr);
                
                if (tex != null)
                {
                    tex.name = "AI_" + prompt;
                    StartGame(tex);
                    messageInput.text = "";
                }
            }
            else
            {
                Debug.LogError("Error: " + uwr.error + " | Server: " + uwr.downloadHandler.text);
            }
        }
        
        sendButton.interactable = true;
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

            piecesCorrect++;
            if (piecesCorrect == pieces.Count)
            {
                playAgainButton.SetActive(true);
                emoji.SetActive(true);
            }
        }
    }
    public void RestartGame()
    {
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
        sendButton.gameObject.SetActive(true);
        messageInput.gameObject.SetActive(true);
        ingame = false;
    }
    public void OnClickReturn()
    {
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
            ingame = false;
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
}
