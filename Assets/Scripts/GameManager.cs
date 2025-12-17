using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Networking;
using System.Collections;

[System.Serializable]
public class JigsawPieceData
{
  public int row;
  public int col;
  public string image; // base64 string from API
}

public class Triangulator
{
    private List<Vector2> m_points = new List<Vector2>();

    public Triangulator(Vector2[] points) {
        m_points = new List<Vector2>(points);
    }
    public Triangulator(Vector3[] points) { // Overload for Vector3
        for(int i=0; i<points.Length; i++) m_points.Add(new Vector2(points[i].x, points[i].y));
    }

    public int[] Triangulate() {
        List<int> indices = new List<int>();
        int n = m_points.Count;
        if (n < 3) return indices.ToArray();

        int[] V = new int[n];
        if (Area() > 0) {
            for (int v = 0; v < n; v++) V[v] = v;
        } else {
            for (int v = 0; v < n; v++) V[v] = (n - 1) - v;
        }

        int nv = n;
        int count = 2 * nv;
        for (int m = 0, v = nv - 1; nv > 2; ) {
            if ((count--) <= 0) return indices.ToArray();

            int u = v;
            if (nv <= u) u = 0;
            v = u + 1;
            if (nv <= v) v = 0;
            int w = v + 1;
            if (nv <= w) w = 0;

            if (Snip(u, v, w, nv, V)) {
                int a, b, c, s, t;
                a = V[u]; b = V[v]; c = V[w];
                indices.Add(a); indices.Add(b); indices.Add(c);
                m++;
                for (s = v, t = v + 1; t < nv; s++, t++) V[s] = V[t];
                nv--;
                count = 2 * nv;
            }
        }
        indices.Reverse();
        return indices.ToArray();
    }

    private float Area() {
        int n = m_points.Count;
        float A = 0.0f;
        for (int p = n - 1, q = 0; q < n; p = q++) {
            A += m_points[p].x * m_points[q].y - m_points[q].x * m_points[p].y;
        }
        return (A * 0.5f);
    }

    private bool Snip(int u, int v, int w, int n, int[] V) {
        int p;
        float Ax, Ay, Bx, By, Cx, Cy, Px, Py;
        Ax = m_points[V[u]].x; Ay = m_points[V[u]].y;
        Bx = m_points[V[v]].x; By = m_points[V[v]].y;
        Cx = m_points[V[w]].x; Cy = m_points[V[w]].y;
        if (Mathf.Epsilon > (((Bx - Ax) * (Cy - Ay)) - ((By - Ay) * (Cx - Ax)))) return false;
        for (p = 0; p < n; p++) {
            if ((p == u) || (p == v) || (p == w)) continue;
            Px = m_points[V[p]].x; Py = m_points[V[p]].y;
            if (InsideTriangle(Ax, Ay, Bx, By, Cx, Cy, Px, Py)) return false;
        }
        return true;
    }

    private bool InsideTriangle(float Ax, float Ay, float Bx, float By, float Cx, float Cy, float Px, float Py) {
        float ax, ay, bx, by, cx, cy, apx, apy, bpx, bpy, cpx, cpy;
        float cCROSSap, bCROSScp, aCROSSbp;
        ax = Cx - Bx; ay = Cy - By; bx = Ax - Cx; by = Ay - Cy; cx = Bx - Ax; cy = By - Ay;
        apx = Px - Ax; apy = Py - Ay; bpx = Px - Bx; bpy = Py - By; cpx = Px - Cx; cpy = Py - Cy;
        aCROSSbp = ax * bpy - ay * bpx;
        cCROSSap = cx * apy - cy * apx;
        bCROSScp = bx * cpy - by * cpx;
        return ((aCROSSbp >= 0.0f) && (bCROSScp >= 0.0f) && (cCROSSap >= 0.0f));
    }
}

public static class JigsawUtils
{
    // Define the shape of a "Tab" relative to a straight line of length 1
    // You can tweak these Vector2 points to change the style of your puzzle piece
    private static readonly Vector2[] BeizerPath = new Vector2[] {
        new Vector2(0, 0),             // Start
        new Vector2(0.35f, 0),         // Shoulder start
        new Vector2(0.35f, 0.15f),     // Neck start
        new Vector2(0.35f, 0.3f),      // Head Left
        new Vector2(0.5f, 0.3f),       // Head Top (Middle)
        new Vector2(0.65f, 0.3f),      // Head Right
        new Vector2(0.65f, 0.15f),     // Neck end
        new Vector2(0.65f, 0),         // Shoulder end
        new Vector2(1, 0)              // End
    };

    // 0 = Flat, 1 = Tab, -1 = Hole
    public static Vector3[] GetBezierCurve(Vector3 start, Vector3 end, int type)
    {
        // If it's a flat edge, just return start and end
        if (type == 0) return new Vector3[] { start, end };

        List<Vector3> points = new List<Vector3>();
        float scale = Vector3.Distance(start, end);
        
        // Calculate direction and perpendicular for rotation
        Vector3 dir = (end - start).normalized;
        Vector3 normal = new Vector3(-dir.y, dir.x, 0); // 2D Perpendicular

        // If it's a hole (-1), we flip the normal direction
        if (type == -1) normal = -normal;

        // Iterate through our predefined Bezier shape
        // Note: For a smoother curve, you would use actual Bezier interpolation here.
        // This is a simplified point-mapping for performance.
        for (int i = 0; i < BeizerPath.Length; i++)
        {
            Vector2 p = BeizerPath[i];
            
            // Lerp along the line (X) + Add Normal Offset (Y)
            Vector3 point = start + (dir * p.x * scale) + (normal * p.y * scale * 0.8f); // 0.8f affects tab height
            points.Add(point);
        }
        return points.ToArray();
    }
}

public class GameManager : MonoBehaviour
{
  [Header("Game Elements")]
  [Range(2, 20)]
  [SerializeField] private int difficulty = 20;
  [SerializeField] private Transform gameHolder;
  //[SerializeField] private Transform piecePrefab;
  [SerializeField] private GameObject piecePrefab; 

  [Header("UI Elements")]
  [SerializeField] private List<Texture2D> imageTextures;
  [SerializeField] private Button returnButton;
  [SerializeField] private ScreenManager screenManager;
  [SerializeField] private Transform levelSelectPanel;
  [SerializeField] private Image levelSelectPrefab;
  [SerializeField] private GameObject playAgainButton;
  [SerializeField] private GameObject emoji;
  [SerializeField] private GameObject difficultyPanel;
  [SerializeField] private TMP_InputField difficultyInput;
  [SerializeField] private PuzzleAPIManager apiManager;
  [SerializeField] private PythonJigsawGenerator pythonGenerator;

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

  // public void StartGame(Texture2D jigsawTexture)
  // {
  //   Debug.Log("StartGame function running.");
  //   Debug.Log($"DIFFICULTY = {difficulty}");
  //   // Hide the UI
  //   levelSelectPanel.gameObject.SetActive(false);
  //   inlevels = false;
  //   Debug.Log($"startGame in levels: {inlevels}");


  //   // We store a list of the transform for each jigsaw piece so we can track them later.
  //   pieces = new List<Transform>();

  //   // Calculate the size of each jigsaw piece, based on a difficulty setting.
  //   dimensions = GetDimensions(jigsawTexture, difficulty);

  //   // Create the pieces of the correct size with the correct texture.
  //   CreateJigsawPieces(jigsawTexture);

  //   // Place the pieces randomly into the visible area.
  //   Scatter();

  //   // Update the border to fit the chosen puzzle.
  //   UpdateBorder();

  //   // As we're starting the puzzle there will be no correct pieces.
  //   piecesCorrect = 0;
  // }
  public void StartGame(Texture2D jigsawTexture)
  {
      Debug.Log("StartGame Running...");
      
      // כיבוי הפאנל
      if (levelSelectPanel != null) levelSelectPanel.gameObject.SetActive(false);
      inlevels = false;
      
      pieces = new List<Transform>(); 
      piecesCorrect = 0;

      foreach(Transform child in gameHolder) Destroy(child.gameObject);

      // חישוב שורות ועמודות (מינימום 2)
      int calculated = (int)Mathf.Sqrt(difficulty);
      int rows = Mathf.Max(2, calculated); 
      int cols = rows;
      
      dimensions = new Vector2Int(cols, rows);

      pythonGenerator.RequestPieces(jigsawTexture, rows, cols, gameHolder, piecePrefab, (generatedPieces) => {
          this.pieces = generatedPieces;
          
          // --- התיקון הקריטי למסגרת ולרווחים ---
          // לוקחים את הגודל המדויק שחושב לפי צורת התמונה
          width = pythonGenerator.FinalPieceWidth;
          height = pythonGenerator.FinalPieceHeight;
          
          float totalPuzzleWidth = width * cols;
          float totalPuzzleHeight = height * rows;

          // שולחים את הגודל הנכון למסגרת
          UpdateBorder(totalPuzzleWidth, totalPuzzleHeight);  
          Scatter();       
      });
  }

  public void ShowLevelSelect()
  {
    Debug.Log("ShowLevelSelect running.");
    // Make sure puzzle pieces are cleared
    levelSelectPanel.gameObject.SetActive(true);
    inlevels = true;
    Debug.Log($"showlevelsrlect in levels: {inlevels}");    
  }


  async void Start()
  {
    Debug.Log("start function running.");
    int piecesToCut = Settings.Instance.pieces;
    bool musicOn = Settings.Instance.isMusicOn;
    string difficultyName = Settings.Instance.difficulty;
    // levelSelectPanel.gameObject.SetActive(true);
    inlevels = true;
    Debug.Log($"start in levels: {inlevels}");
    // returnButton.onClick.AddListener(OnClickReturn);

    // Create the UI
    foreach (Texture2D texture in imageTextures)
    {
      Image image = Instantiate(levelSelectPrefab, levelSelectPanel);
      image.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero);
      //inlevels = false;
      // Assign button action
      image.GetComponent<Button>().onClick.AddListener(delegate { StartGame(texture); });
      //image.GetComponent<Button>().onClick.AddListener(() => OnImageSelected(texture));
    }
  }


  public void OnConfirmInputDifficulty()
  {
    if (int.TryParse(difficultyInput.text, out int input))
    {
      if (input >= 2 && input <= 20)
      {
        difficulty = input;
        difficultyPanel.SetActive(false);
        StartGame(selectedTexture);
      }
      else
      {
        Debug.LogWarning("Please enter a number between 2 and 20.");
      }
    }
    else
    {
      Debug.LogWarning("Invalid input. Please enter a number.");
    }
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

  // Create all the jigsaw pieces
  // public void CreateJigsawPieces(Texture2D jigsawTexture)
  // {
  //   Debug.Log("CreateJigsawPieces function running.");
  //   inlevels = false;
  //   Debug.Log($"createjigsawpieces in levels: {inlevels}");
  //   // Calculate piece sizes based on the dimensions.
  //   height = 1f / dimensions.y;
  //   float aspect = (float)jigsawTexture.width / jigsawTexture.height;
  //   width = aspect / dimensions.x;

  //   for (int row = 0; row < dimensions.y; row++)
  //   {
  //     for (int col = 0; col < dimensions.x; col++)
  //     {
  //       // Create the piece in the right location of the right size.
  //       Transform piece = Instantiate(piecePrefab, gameHolder);
  //       piece.localPosition = new Vector3(
  //         (-width * dimensions.x / 2) + (width * col) + (width / 2),
  //         (-height * dimensions.y / 2) + (height * row) + (height / 2),
  //         -1);
  //       piece.localScale = new Vector3(width, height, 1f);

  //       // We don't have to name them, but always useful for debugging.
  //       piece.name = $"Piece {(row * dimensions.x) + col}";
  //       pieces.Add(piece);
  //       if (piece.GetComponent<BoxCollider2D>() == null)
  //       {
  //           piece.gameObject.AddComponent<BoxCollider2D>();
  //       }

  //       // Assign the correct part of the texture for this jigsaw piece
  //       // We need our width and height both to be normalised between 0 and 1 for the UV.
  //       float width1 = 1f / dimensions.x;
  //       float height1 = 1f / dimensions.y;
  //       // UV coord order is anti-clockwise: (0, 0), (1, 0), (0, 1), (1, 1)
  //       Vector2[] uv = new Vector2[4];
  //       uv[0] = new Vector2(width1 * col, height1 * row);
  //       uv[1] = new Vector2(width1 * (col + 1), height1 * row);
  //       uv[2] = new Vector2(width1 * col, height1 * (row + 1));
  //       uv[3] = new Vector2(width1 * (col + 1), height1 * (row + 1));
  //       // Assign our new UVs to the mesh.
  //       Mesh mesh = piece.GetComponent<MeshFilter>().mesh;
  //       mesh.uv = uv;
  //       // Update the texture on the piece
  //       piece.GetComponent<MeshRenderer>().material.SetTexture("_MainTex", jigsawTexture);
  //     }
  //   }
  // }

  // Place the pieces randomly in the visible area.
  // public void Scatter()
  // {
  //   Debug.Log("Scatter function running.");
  //   // Calculate the visible orthographic size of the screen.
  //   float orthoHeight = Camera.main.orthographicSize;
  //   float screenAspect = (float)Screen.width / Screen.height;
  //   float orthoWidth = (screenAspect * orthoHeight);

  //   // Ensure pieces are away from the edges.
  //   float pieceWidth = width * gameHolder.localScale.x;
  //   float pieceHeight = height * gameHolder.localScale.y;

  //   orthoHeight -= pieceHeight / 2;
  //   orthoWidth -= pieceWidth / 2;

  //   // Place each piece randomly in the visible area.
  //   foreach (Transform piece in pieces)
  //   {
  //     float x = UnityEngine.Random.Range(-orthoWidth, orthoWidth);
  //     float y = UnityEngine.Random.Range(-orthoHeight, orthoHeight);
  //     piece.position = new Vector3(x, y, -1);
  //   }
  // }
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
          // --- התיקון: שימוש ב-UnityEngine.Random ---
          float randomX = UnityEngine.Random.Range(-safeX, safeX);
          float randomY = UnityEngine.Random.Range(-safeY, safeY);
          // ------------------------------------------

          // מיקום בטוח בתוך המסך, ו-Z=-5 כדי שיהיה מעל הרקע
          piece.localPosition = new Vector3(randomX, randomY, -5.0f);
          
          Collider2D col = piece.GetComponent<Collider2D>();
          if(col != null) col.enabled = true;
      }
  }

  // Update the border to fit the chosen puzzle.
  // public void UpdateBorder()
  // {
  //   Debug.Log("UpdateBorder function running.");
  //   LineRenderer lineRenderer = gameHolder.GetComponent<LineRenderer>();

  //   // Calculate half sizes to simplify the code.
  //   float halfWidth = (width * dimensions.x) / 2f;
  //   float halfHeight = (height * dimensions.y) / 2f;

  //   // We want the border to be behind the pieces.
  //   float borderZ = 0f;

  //   // Set border vertices, starting top left, going clockwise.
  //   lineRenderer.SetPosition(0, new Vector3(-halfWidth, halfHeight, borderZ));
  //   lineRenderer.SetPosition(1, new Vector3(halfWidth, halfHeight, borderZ));
  //   lineRenderer.SetPosition(2, new Vector3(halfWidth, -halfHeight, borderZ));
  //   lineRenderer.SetPosition(3, new Vector3(-halfWidth, -halfHeight, borderZ));

  //   // Set the thickness of the border line.
  //   lineRenderer.startWidth = 0.1f;
  //   lineRenderer.endWidth = 0.1f;

  //   // Show the border line.
  //   lineRenderer.enabled = true;
  // }
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

  // private void SnapAndDisableIfCorrect()
  // {
  //   Debug.Log("SnapAndDisableIfCorrect function running.");
  //   // We need to know the index of the piece to determine it's correct position.
  //   int pieceIndex = pieces.IndexOf(draggingPiece);

  //   // The coordinates of the piece in the puzzle.
  //   int col = pieceIndex % dimensions.x;
  //   int row = pieceIndex / dimensions.x;

  //   // The target position in the non-scaled coordinates.
  //   Vector2 targetPosition = new((-width * dimensions.x / 2) + (width * col) + (width / 2),
  //                                (-height * dimensions.y / 2) + (height * row) + (height / 2));

  //   // Check if we're in the correct location.
  //   if (Vector2.Distance(draggingPiece.localPosition, targetPosition) < (width / 2))
  //   {
  //     // Snap to our destination.
  //     draggingPiece.localPosition = targetPosition;

  //     // Disable the collider so we can't click on the object anymore.
  //     draggingPiece.GetComponent<BoxCollider2D>().enabled = false;

  //     // Increase the number of correct pieces, and check for puzzle completion.
  //     piecesCorrect++;
  //     if (piecesCorrect == pieces.Count)
  //     {
  //       playAgainButton.SetActive(true);
  //       emoji.SetActive(true);
  //     }
  //   }
  // }

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
    levelSelectPanel.gameObject.SetActive(true);
    inlevels = true;
    Debug.Log($"Restart in levels: {inlevels}");

  }
  public void OnClickReturn()
  {
    Debug.Log("OnClickReturn function running.");
    Debug.Log($"onclick in levels, befor change: {inlevels}");

    if (inlevels)
    {
      Debug.LogWarning("see levelSelectPanel as true.");
      levelSelectPanel.gameObject.SetActive(false);
      inlevels = false;
      Debug.Log($"onclick when panel was showing after change in levels: {inlevels}");
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
      levelSelectPanel.gameObject.SetActive(true);
      inlevels = true;
      Debug.Log($"onclick when panel wasn't showing after change in levels: {inlevels}");

    } 
  }
}