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
  [SerializeField] private Transform levelListContainer;
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
  private bool isGameActive = false;

  public void StartGame(Texture2D jigsawTexture)
  {
      Debug.Log("StartGame Running...");
      if (Settings.Instance != null)
      {
          difficulty = Settings.Instance.pieces;
          Debug.Log($"Difficulty loaded from Settings: {difficulty}");
      }
      isGameActive = true;
      
      if (levelSelectPanel != null) levelSelectPanel.gameObject.SetActive(false);
      inlevels = false;
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
    if (Settings.Instance != null)
    {
        difficulty = Settings.Instance.pieces;
    }
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
      // Image image = Instantiate(levelSelectPrefab, levelSelectPanel);
      Image image = Instantiate(levelSelectPrefab, levelListContainer);
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
        if (Settings.Instance != null) {
            Settings.Instance.pieces = difficulty; 
        }
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
      
      lineRenderer.sortingOrder = 20; 

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
    isGameActive = false;
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