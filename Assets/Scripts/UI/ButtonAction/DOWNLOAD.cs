using UnityEngine;

public class DOWNLOAD : MonoBehaviour
{
    [SerializeField] private VoxelGridVisualizer voxelGridVisualizer;
    [SerializeField] private PopupMessage popupMessage;
    [SerializeField] private Settings settings;

    public void OnClick()
    {
        if (settings.getBoolByName("Local Mode"))
        {
            voxelGridVisualizer.VisualizeLocalMesh();
            popupMessage.PopUp("Import successful", 3);
        }
        else
        {
            voxelGridVisualizer.DownloadAndVisualizeMesh();
            popupMessage.PopUp("Download successful", 3);
        }
    }
}