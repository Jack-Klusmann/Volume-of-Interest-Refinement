using UnityEngine;

public class UPLOAD : MonoBehaviour
{
    [SerializeField] private VoxelGridVisualizer voxelGridVisualizer;
    [SerializeField] private PopupMessage popupMessage;
    [SerializeField] private Settings settings;

    public void OnClick()
    {
        if (voxelGridVisualizer.meshExists)
        {
            if (settings.getBoolByName("Local Mode"))
            {
                GLTFstuff.ExportGameObjectToPath(GameObject.Find("VoxelGridAsMesh"), Application.persistentDataPath);
            }
            else
            {
                voxelGridVisualizer.uploadToserverAndVisualize();
                popupMessage.PopUp("UPLOADING...");
            }
        }
        else
        {
            popupMessage.PopUp("You have to generate the Mesh before sending it!");
        }
    }
}