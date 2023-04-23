using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

// Used to render a voxel grid into the scene
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class VoxelGridVisualizer : MonoBehaviour
{
    [SerializeField] private VoxelGridMC voxelGridMC;
    [SerializeField] private VoxelGrid voxelGrid;
    [SerializeField] private GameObject voxelPrefab;

    [SerializeField] private PopupMessage popupMessage;
    [SerializeField] private GameObject loadingScreen;

    public bool meshExists;
    public bool serverMeshExists;

    // Stores references to all instantiated game objects so they can be deleted (reusing them would be more efficient...)
    private List<GameObject> voxelMarkers = new();

    public void Visualize()
    {
        if (meshExists) Destroy(GameObject.Find("exportedGLTF"));
        foreach (var voxelMarker in voxelMarkers) Destroy(voxelMarker);
        voxelMarkers = new List<GameObject>();

        var voxels = voxelGrid.getVoxels();
        voxelPrefab.transform.localScale = voxelGrid.voxelSize * Vector3.one;
        // Code for mesh combining adopted from https://docs.unity3d.com/ScriptReference/Mesh.CombineMeshes.html
        List<MeshFilter> meshFilters = new();

        foreach (var voxel in voxels)
        {
            var cube = Instantiate(voxelPrefab, voxel, Quaternion.identity, voxelGrid.gridSpace());
            meshFilters.Add(cube.GetComponent<MeshFilter>());
            Destroy(cube);
        }

        MeshHelper.combineMesh(meshFilters, voxelGrid.gridSpace(), transform.GetComponent<Renderer>().material);
        GLTFstuff.ExportGameObjectToPath(GameObject.Find("VoxelGridAsMesh"), Application.persistentDataPath);
        meshExists = true;
    }

    public void VisualizeMarchingCubes()
    {
        if (meshExists) Destroy(GameObject.Find("exportedGLTF"));
        MarchingCubesAdapter adapter = new();
        adapter.marchingCubesOnVoxelArray(voxelGridMC, transform.GetComponent<Renderer>().material);
        GLTFstuff.ExportGameObjectToPath(GameObject.Find("MarchingCubesMesh"), Application.persistentDataPath);
        meshExists = true;
    }

    public void UploadToServerAndVisualize()
    {
        if (serverMeshExists) Destroy(GameObject.Find("serverGLTF"));
        SendMeshToServer(Application.persistentDataPath + "/exportedGLTF",
            Application.persistentDataPath + "/serverGLTF");
        serverMeshExists = true;
    }

    public void ExportLocallyAndVisualize()
    {
        if (serverMeshExists) Destroy(GameObject.Find("serverGLTF"));
        GLTFstuff.ExportGameObjectToPath(GameObject.Find("VoxelGridAsMesh"),
            Application.persistentDataPath + "/exportedGLTF");
        serverMeshExists = true;
    }

    private void SendMeshToServer(string filePath = "Assets/Resources/GLTF/exportedGLTF",
        string storeToPath = "Assets/Resources/GLTF/serverGLTF")
    {
        loadingScreen.SetActive(true);
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        StartCoroutine(ServerCommunicator.uploadGLTF(filePath + ".gltf", storeToPath + ".gltf"));
        StartCoroutine(ServerCommunicator.uploadBIN(filePath + ".bin", storeToPath + ".bin", storeToPath + ".gltf",
            stopwatch, popupMessage, loadingScreen));
    }

    public void DownloadAndVisualizeMesh()
    {
        loadingScreen.SetActive(true);
        if (meshExists) Destroy(GameObject.Find("exportedGLTF"));
        var savePath = Application.persistentDataPath + "/exportedGLTF";
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        StartCoroutine(ServerCommunicator.downloadGLTF(savePath + ".gltf"));
        StartCoroutine(ServerCommunicator.downloadBIN(savePath + ".bin", savePath + ".gltf", stopwatch, popupMessage,
            loadingScreen));
        meshExists = true;
    }

    public void VisualizeLocalMesh()
    {
        loadingScreen.SetActive(true);
        if (meshExists) Destroy(GameObject.Find("exportedGLTF"));
        GLTFstuff.ImportGameObjectFromPath(GameObject.Find("ImageTarget"),
            Application.persistentDataPath + "/exportedGLTF");
        meshExists = true;
    }
}