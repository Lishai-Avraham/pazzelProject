using UnityEngine;
using UnityEngine.UI;

public class ImageUploader : MonoBehaviour
{
    public RawImage previewImage; // optional, to preview the image before puzzle
    public GameManager gameManager; // reference to your script with CreateJigsawPieces
    [SerializeField] private ScreenManager screenManager;
    [SerializeField] private Button returnButton;
    int ModePanelIndex = 1;
    public void PickImage()
    {
        // Opens Android's gallery
        NativeGallery.GetImageFromGallery((path) =>
        {
            if (path != null)
            {
                // Load the selected image
                Texture2D texture = NativeGallery.LoadImageAtPath(path, 1024);
                if (texture == null)
                {
                    Debug.LogError("Couldn't load texture from " + path);
                    return;
                }

                // Preview it (optional)
                if (previewImage != null)
                    previewImage.texture = texture;

                // Send to your jigsaw function
                if (gameManager != null)
                    gameManager.StartGame(texture);
            }
        }, "Select an image", "image/*");
    }
    public void OnClickReturn()
    {
        screenManager.ShowScreen(ModePanelIndex);
    }

}
