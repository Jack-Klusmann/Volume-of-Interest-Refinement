using UnityEngine;

public class DOWNLOAD : MonoBehaviour
{
    [SerializeField] private VoxelGridVisualizer voxelGridVisualizer;
    [SerializeField] private PopupMessage popupMessage;
    [SerializeField] private Settings settings;

    public void OnClick()
    {
        voxelGridVisualizer.downloadAndVisualizeGLTF();
        popupMessage.PopUp("DOWNLOADING");
    }
}
