// using UnityEngine;
// using UnityEngine.UI;
// using TMPro;
// using System.Net.Http;
// using System.Threading.Tasks;
// using Newtonsoft.Json;
// using System.Text;
// using Newtonsoft.Json.Linq;

// public class PromptsInput : MonoBehaviour
// {
//     [SerializeField] private GameManager gameManager;
//     [SerializeField] private TMP_InputField messageInput;
//     [SerializeField] private Button sendButton;
//     [SerializeField] private Button returnButton;
//     [SerializeField] private ScreenManager screenManager;
//     int ModePanelIndex = 1;
//     private static readonly HttpClient httpClient = new HttpClient();

//     const string geminiURL = "https://generativelanguage.googleapis.com/v1beta/models/imagen-3.0-generate-002:generateImages";
//     private string apiKey = "AIzaSyClKQniZM7Z4lmtBIsXeB3I9PMln3Ts1X0"; // TODO: Replace with your actual OpenAI API key

//     void Start()
//     {
//         // Attach the click handler
//         sendButton.onClick.AddListener(OnSendClicked);
//         returnButton.onClick.AddListener(OnClickReturn);
//     }
//     public void OnClickReturn()
//     {
//         screenManager.ShowScreen(ModePanelIndex);
//     }
//     public async void OnSendClicked()
//     {
//         string text = messageInput.text;
//         if (string.IsNullOrEmpty(text))
//         {
//             Debug.Log("Empty input, please enter an image description");
//             return;
//         }

//         await ProcessMessage(text);
//         messageInput.text = "";
//     }

//     // private async Task ProcessMessage(string msg)
//     // {
//     //     Debug.Log("Sending prompt to OpenAI: " + msg);

//     //     var requestBody = new
//     //     {
//     //         requests = new[]
//     //         {
//     //             new
//     //             {
//     //                 prompt = new { text = msg },
//     //                 imageConfig = new
//     //                 {
//     //                     numberOfImages = 1,
//     //                     aspectRatio = "1:1"
//     //                 }
//     //             }
//     //         }
//     //     };

//     //     string json = JsonConvert.SerializeObject(requestBody);
//     //     var request = new HttpRequestMessage(HttpMethod.Post, geminiURL + $"?key={apiKey}");
//     //     // request.Headers.Add("Authorization", $"Bearer {apiKey}");
//     //     request.Content = new StringContent(json, Encoding.UTF8, "application/json");

//     //     try
//     //     {
//     //         HttpResponseMessage response = await httpClient.SendAsync(request);
//     //         string result = await response.Content.ReadAsStringAsync();

//     //         Debug.Log("OpenAI response: " + result);

//     //         // You can parse result JSON here (contains image URL(s))
//     //         JObject parsed = JObject.Parse(result);
//     //         // string imageUrl = parsed["data"][0]["url"].ToString();
//     //         // Debug.Log("Image URL: " + imageUrl);
//     //         string base64Image = parsed["generated_images"][0]["image"]["image_bytes"].ToString();

//     //         // Texture2D tex = await DownloadImage(imageUrl);
//     //         Texture2D tex = DecodeBase64(base64Image);

//     //         if (tex != null)
//     //         {
//     //             Debug.Log("Image downloaded, creating jigsaw...");
//     //             gameManager.CreateJigsawPieces(tex);
//     //         }
//     //     }
//     //     catch (System.Exception ex)
//     //     {
//     //         Debug.LogError("Error sending request: " + ex.Message);
//     //     }
//     // }

//     private async Task ProcessMessage(string msg)
//     {
//         Debug.Log("Sending prompt to Gemini Image model: " + msg);

//         string modelUrl = "https://generativelanguage.googleapis.com/v1beta/models/imagen-4.0-generate-001:predict";


//         var requestBody = new
//         {
//             prompt = msg,
//             number_of_images = 1,
//             output_mime_type = "image/jpeg",
//             aspect_ratio = "1:1"
//         };

//         string json = JsonConvert.SerializeObject(requestBody);
//         var request = new HttpRequestMessage(HttpMethod.Post, modelUrl + $"?key={apiKey}");
//         request.Content = new StringContent(json, Encoding.UTF8, "application/json");

//         try
//         {
//             HttpResponseMessage response = await httpClient.SendAsync(request);
//             string result = await response.Content.ReadAsStringAsync();

//             if (!response.IsSuccessStatusCode)
//             {
//                 Debug.LogError($"API Error: {response.StatusCode}");
//                 Debug.LogError($"Response Content: {result}");
//                 return;
//             }

//             Debug.Log("Imagen API response: " + result);

//             JObject parsed = JObject.Parse(result);
            
//             // ✅ CORRECTED: The Imagen response contains a 'generated_images' array.
//             // This is different from the Gemini response structure.
//             var imagesArray = parsed["generated_images"];
//             if (imagesArray == null || !imagesArray.HasValues)
//             {
//                 Debug.LogError("No images found in the API response.");
//                 return;
//             }

//             string base64Image = imagesArray[0]["image"]["image_bytes"].ToString();

//             Texture2D tex = DecodeBase64(base64Image);

//             if (tex != null)
//             {
//                 Debug.Log("Image decoded, creating jigsaw...");
//                 gameManager.CreateJigsawPieces(tex);
//             }
//         }
//         catch (System.Exception ex)
//         {
//             Debug.LogError("Error during API call: " + ex.Message);
//         }
//     }

//     private async Task<Texture2D> DownloadImage(string url)
//     {
//         try
//         {
//             HttpResponseMessage imgResponse = await httpClient.GetAsync(url);
//             byte[] imgData = await imgResponse.Content.ReadAsByteArrayAsync();

//             Texture2D tex = new Texture2D(2, 2);
//             tex.LoadImage(imgData); // Decode PNG/JPG
//             return tex;
//         }
//         catch (System.Exception ex)
//         {
//             Debug.LogError("Error downloading image: " + ex.Message);
//             return null;
//         }
//     }
    
//     private Texture2D DecodeBase64(string base64)
//     {
//         try
//         {
//             // Convert the Base64 string to a byte array
//             byte[] imgData = System.Convert.FromBase64String(base64);

//             // Create a new Texture2D and load the image data
//             Texture2D tex = new Texture2D(2, 2);
//             tex.LoadImage(imgData); // Decode PNG/JPG
//             return tex;
//         }
//         catch (System.Exception ex)
//         {
//             Debug.LogError("Error decoding Base64 image: " + ex.Message);
//             return null;
//         }
//     }
// }





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

    private static readonly HttpClient httpClient = new HttpClient();
    private string apiKey = "AIzaSyClKQniZM7Z4lmtBIsXeB3I9PMln3Ts1X0";
    private const string MODEL_URL = "https://generativelanguage.googleapis.com/v1beta/models/imagen-4.0-generate-001:predict";
    int ModePanelIndex = 1;

    void Start()
    {
        sendButton.onClick.AddListener(OnSendClicked);
        returnButton.onClick.AddListener(OnClickReturn);
    }

    public void OnClickReturn()
    {
        screenManager.ShowScreen(ModePanelIndex);
    }

    public async void OnSendClicked()
    {
        string prompt = messageInput.text;
        if (string.IsNullOrEmpty(prompt))
        {
            Debug.Log("Empty input, please enter an image description");
            return;
        }

        await GenerateImage(prompt);
        messageInput.text = "";
    }

    private async Task GenerateImage(string prompt)
    {
        Debug.Log($"Sending image generation request for: {prompt}");

        // Match the exact REST structure from your curl example
        var requestBody = new
        {
            instances = new[]
            {
                new { prompt = prompt }
            },
            parameters = new
            {
                sampleCount = 1 // Number of images to generate
            }
        };

        string json = JsonConvert.SerializeObject(requestBody);
        var request = new HttpRequestMessage(HttpMethod.Post, MODEL_URL + $"?key={apiKey}");
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            HttpResponseMessage response = await httpClient.SendAsync(request);
            string result = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                Debug.LogError($"API Error: {response.StatusCode}");
                Debug.LogError($"Response Content: {result}");
                return;
            }

            Debug.Log("Imagen API Response: " + result);

            JObject parsed = JObject.Parse(result);
            var images = parsed["predictions"]?[0]?["generated_images"];

            if (images == null || !images.HasValues)
            {
                Debug.LogError("No generated images found in the response.");
                return;
            }

            string base64Image = images[0]["image"]["image_bytes"].ToString();
            Texture2D tex = DecodeBase64(base64Image);

            if (tex != null)
            {
                Debug.Log("Image decoded successfully, creating jigsaw...");
                //gameManager.CreateJigsawPieces(tex);
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Error during API call: " + ex.Message);
        }
    }

    private Texture2D DecodeBase64(string base64)
    {
        try
        {
            byte[] imgData = System.Convert.FromBase64String(base64);
            Texture2D tex = new Texture2D(2, 2);
            tex.LoadImage(imgData);
            return tex;
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Error decoding Base64 image: " + ex.Message);
            return null;
        }
    }
}
