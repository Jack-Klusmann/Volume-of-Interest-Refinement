using System.Collections;
using System.Collections.Generic;
using System.Threading;
using TriangleNet.Geometry;
using UnityEngine;

// Acts as an interface to other scripts in the scene for handling the entire intersection process within the voxel grid
public class SP_Controller : MonoBehaviour
{
    [SerializeField] public VoxelGridMC voxelGrid;
    [SerializeField] public VoxelGrid oldVoxelGrid;
    [SerializeField] private ScreenDrawing screenDrawing;
    [SerializeField] private CamPos camPos;
    [SerializeField] private VoxelGridVisualizer visualizer;
    [SerializeField] private GalleryStorage galleryStorage;
    [SerializeField] private RenderToTexture renderToTexture;
    [SerializeField] public PopupMessage popupMessage;
    [SerializeField] public Settings settings;
    [SerializeField] public GameObject loadingScreen;

    private GalleryStorage.ImageMarking[] _markings;
    public List<Vector3> points;
    Quaternion rotation;
    Vector3 position;
    Vector3 localScale;

    [HideInInspector] public float autoVoxelSize = 0.0f;

    private void Awake()
    {
        voxelGrid.initialize();
    }

    // Intersects the projected image markings by testing each voxel against the view planes' markings, subdividing and repeating.
    public IEnumerator IntersectAreaNew(List<GalleryStorage.ImageMarking> markings)
    {
        var newMethod = settings.getBoolByName("MarchingCubes method");
        _markings = markings.ToArray();

        if (newMethod)
            voxelGrid.initialize(autoVoxelSize);
        else
            oldVoxelGrid.initialize(autoVoxelSize);

        if (newMethod)
        {
            loadingScreen.SetActive(true);
            var gridSpace = voxelGrid.gridSpace();
            rotation = gridSpace.rotation;
            position = gridSpace.position;
            localScale = gridSpace.localScale;
            var thread = new Thread(Calculation);
            thread.Start();

            while (thread.IsAlive) yield return new WaitForEndOfFrame();

            loadingScreen.SetActive(false);
            visualizer.VisualizeMarchingCubes();
        }
        else
        {
            loadingScreen.SetActive(true);

            var maxSubdivisionIterations = settings.getIntByName("Max number subdivision iterations");
            var maxVoxelsToSubdivide = settings.getIntByName("Max number of voxels to subdivide");

            for (var iteration = 0;
                 iteration < maxSubdivisionIterations &&
                 (oldVoxelGrid.GetVoxelCount() < maxVoxelsToSubdivide || iteration == 0);
                 iteration++)
            {
                oldVoxelGrid.subdivideVoxels(CheckForIntersectionFromImageMarkings);
                while (!oldVoxelGrid.calculationDone()) yield return new WaitForEndOfFrame();
            }

            visualizer.Visualize();
            loadingScreen.SetActive(false);
        }

        GlobalContextVariable.updateValue(GlobalContextVariable.GlobalContextVariableValue.main_view);
    }

    private void Calculation()
    {
        var maxSubdivisionIterations = settings.getIntByName("Max number subdivision iterations");
        var maxVoxelsToSubdivide = settings.getIntByName("Max number of voxels to subdivide");

        for (var iteration = 0;
             iteration < maxSubdivisionIterations &&
             (voxelGrid.getTotalVoxelCount() < maxVoxelsToSubdivide || iteration == 0);
             iteration++)
            voxelGrid.subdivideVoxelsThreading(CheckForIntersectionFromImageMarkings, iteration != 0, rotation,
                position, localScale);
    }

    /*
     * Check, if a single point is within EVERY projected marking
     * The checked point is meant to be a corner of a voxel
     * 
     * This algorithm results in:
     *  - a true negative, if not every marking contains at least one corner of this voxel
     *  - a true positive, if at least one corner projects into EVERY marking 
     *  - BUT a true negative, if EVERY corner projects into EVERY marking (because we want to highlight and subdivide only those voxels, which lie on the border of the resulting volume)
     *  - !!! a false negative, if no corner projects into ALL the markings, but EVERY marking contains at least one corner of this voxel (which should trigger a true positive)
     *      =>  possible solution: don't return true or false but return what markings (by index) the point is projected into.
     *          E.g. return an integer defined by the sum of all 2^i where i is the index of a marking it projects into.
     *          So if the first and third marking of three markings contain this point, the function returns 0b101. If the OR of all returns it 0b111... all markings contain at least one corner.
     *          Just check if there is at least one corner not returning 0b111... to make sure it is a corner voxel
     *  - !!! a false negative, if the voxel is too big and no corner projects into a certain marking, which is still intersecting the projected voxel
     */
    private bool CheckForIntersectionFromImageMarkings(Vector3 vec3)
    {
        if (_markings.Length == 0) return false;
        
        foreach (var im in _markings)
        {
            var tmp = im.ProjectionMatrix * (vec3 - im.Position);
            
            if (tmp.z < 0) return false;

            var screenPoint = new Point(tmp.x / tmp.z, tmp.y / tmp.z);
            var contains = false;
            var triangleNetMesh = ScreenDrawing.Triangulate(im.Marking);

            foreach (var triangle in triangleNetMesh.Triangles)
                if (triangle.Contains(screenPoint))
                {
                    contains = true;
                    break;
                }

            if (!contains) return false;
        }

        return true;
    }

    // WHen the user takes a picture, the camera properties of mainCamera are copied to mainCameraCopy.
    // The rest of the code is no longer used.
    public List<Vector3> CreateCubeIntersectionFrustum()
    {
        camPos.mainCameraCopy.CopyFrom(camPos.mainCamera);

        var frustumCorners = new Vector3[4];
        camPos.mainCamera.CalculateFrustumCorners(new Rect(0, 0, 1, 1), camPos.mainCamera.farClipPlane,
            Camera.MonoOrStereoscopicEye.Mono, frustumCorners);

        var intersectionVectors = new List<Vector3>();
        var position = camPos.mainCamera.transform.position;
        intersectionVectors.Add(position);

        for (var i = 0; i < 4; i++)
        {
            var worldSpaceCorner = camPos.mainCamera.transform.TransformVector(frustumCorners[i]);
            intersectionVectors.Add(worldSpaceCorner);
        }

        return intersectionVectors;
    }

    // pass down the function
    public void SaveImageMarking()
    {

        galleryStorage.AddImageMarking(renderToTexture.photoTexture, camPos.ProjectionMat, camPos.Position,
            camPos.mainCamera.transform.rotation, points);

        if (screenDrawing.points.Count >= 3)
            galleryStorage.SetMarkingForLatest(screenDrawing.points, screenDrawing.worldPoints);
    }

    // pass down the function
    public void UpdateImageMarking()
    {
        if (screenDrawing.points.Count >= 3)
            galleryStorage.SetMarkingForActive(screenDrawing.points, screenDrawing.worldPoints);
        else
            popupMessage.PopUp(PopupMessage.NoMarking);
    }
}