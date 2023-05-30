using UnityEngine;

public class CLEAR : MonoBehaviour
{
    [SerializeField] private VoxelGridVisualizer visualizer;
    public void OnClick()
    {
        Destroy(GameObject.Find("exportedGLTF"));
        Destroy(GameObject.Find("serverGLTF"));
        visualizer.meshExists = false;
        visualizer.serverMeshExists = false;
    }
}