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
                voxelGridVisualizer.ExportLocallyAndVisualize();
                popupMessage.PopUp("Export successful", 3);
            }
            else
            {
                voxelGridVisualizer.UploadToServerAndVisualize();
                popupMessage.PopUp("Upload successful", 3);
            }
        }
        else
        {
            popupMessage.PopUp("There is currently no mesh to be uploaded");
        }
    }
}