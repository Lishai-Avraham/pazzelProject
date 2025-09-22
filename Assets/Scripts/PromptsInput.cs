using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Text;
using Newtonsoft.Json.Linq;

public class PromptsInput : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;
    [SerializeField] private TMP_InputField messageInput;
    [SerializeField] private Button sendButton;
    [SerializeField] private Button returnButton;
    [SerializeField] private ScreenManager screenManager;
    int ModePanelIndex = 1;
    private static readonly HttpClient httpClient = new HttpClient();

    const string openaiUrl = "https://api.openai.com/v1/images/generations";
    private string apiKey = "YOUR_OPENAI_API_KEY_HERE"; // TODO: Replace with your actual OpenAI API key

    void Start()
    {
        // Attach the click handler
        sendButton.onClick.AddListener(OnSendClicked);
    }
    public void OnClickReturn()
    {
        screenManager.ShowScreen(ModePanelIndex);
    }
    public async void OnSendClicked()
    {
        string text = messageInput.text;
        if (string.IsNullOrEmpty(text))
        {
            Debug.Log("Empty input, please enter an image description");
            return;
        }

        await ProcessMessage(text);
        messageInput.text = "";
    }

    private async Task ProcessMessage(string msg)
    {
        Debug.Log("Sending prompt to OpenAI: " + msg);

        var requestBody = new
        {
            model = "gpt-image-1",
            prompt = msg,
            n = 1,
            size = "1024x1024"
        };

        string json = JsonConvert.SerializeObject(requestBody);
        var request = new HttpRequestMessage(HttpMethod.Post, openaiUrl);
        request.Headers.Add("Authorization", $"Bearer {apiKey}");
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            HttpResponseMessage response = await httpClient.SendAsync(request);
            string result = await response.Content.ReadAsStringAsync();

            Debug.Log("OpenAI response: " + result);

            // You can parse result JSON here (contains image URL(s))
            JObject parsed = JObject.Parse(result);
            string imageUrl = parsed["data"][0]["url"].ToString();
            Debug.Log("Image URL: " + imageUrl);

            Texture2D tex = await DownloadImage(imageUrl);

            if (tex != null)
            {
                Debug.Log("Image downloaded, creating jigsaw...");
                gameManager.CreateJigsawPieces(tex);
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Error sending request: " + ex.Message);
        }
    }
    
    private async Task<Texture2D> DownloadImage(string url)
    {
        try
        {
            HttpResponseMessage imgResponse = await httpClient.GetAsync(url);
            byte[] imgData = await imgResponse.Content.ReadAsByteArrayAsync();

            Texture2D tex = new Texture2D(2, 2);
            tex.LoadImage(imgData); // Decode PNG/JPG
            return tex;
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Error downloading image: " + ex.Message);
            return null;
        }
    }
}
