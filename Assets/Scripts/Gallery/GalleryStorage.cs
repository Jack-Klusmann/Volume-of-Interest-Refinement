using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Stores and manages the photos taken including their respective markings and calculates the adequate virtual cube for intersection

public class GalleryStorage : MonoBehaviour
{
    public int lastImageSelected = -1;
    public readonly List<ImageMarking> ImageMarkings = new();
    private bool _unqualified;
    
    public struct ImageMarking
    {
        public ImageMarking(Texture2D image, List<Vector3> marking, Matrix4x4 projectionMatrix, Vector3 position,
            Quaternion rotation, List<Vector3> points, List<Vector3> worldPoints)
        {
            Image = image;
            Marking = marking;
            ProjectionMatrix = projectionMatrix;
            Position = position;
            Rotation = rotation;
            Points = points;
            WorldPoints = worldPoints;
        }

        public readonly Texture2D Image;
        public readonly List<Vector3> Marking;
        public readonly Matrix4x4 ProjectionMatrix;
        public readonly Vector3 Position;
        public readonly Quaternion Rotation;
        public readonly List<Vector3> Points;
        public readonly List<Vector3> WorldPoints;

    }

    [SerializeField] public PopupMessage popupMessage;
    [SerializeField] private GalleryUI galleryUI;
    [SerializeField] private SP_Controller intersect;

    public int GetImageMarkingCount()
    {
        return ImageMarkings.Count;
    }

    public void AddImageMarking(Texture2D tex, Matrix4x4 projectionMatrix, Vector3 position, Quaternion rotation,
        List<Vector3> points)
    {
        ImageMarkings.Add(new ImageMarking(tex, null, projectionMatrix, position, rotation, points, null));
        galleryUI.updateUI(ImageMarkings, this);
    }

    public void RemoveLatestImageMarking()
    {
        if (lastImageSelected != -1)
        {
            ImageMarkings.RemoveAt(lastImageSelected);
            lastImageSelected = -1;
            galleryUI.updateUI(ImageMarkings, this);
        }
        else
        {
            popupMessage.PopUp(PopupMessage.NoPhotoSelected);
        }

        galleryUI.unshowImage();
    }

    private void SetMarking(List<Vector3> newMarking, List<Vector3> newWorldMarking, int index)
    {
        ImageMarkings[index] = new ImageMarking(ImageMarkings[index].Image, newMarking,
            ImageMarkings[index].ProjectionMatrix, ImageMarkings[index].Position, ImageMarkings[index].Rotation,
            ImageMarkings[index].Points, newWorldMarking);
        galleryUI.updateUI(ImageMarkings, this);
    }

    public void SetMarkingForLatest(List<Vector3> newMarking, List<Vector3> newWorldMarking)
    {
        SetMarking(newMarking, newWorldMarking, ImageMarkings.Count - 1);
        galleryUI.updateUI(ImageMarkings, this);
    }

    public void SetMarkingForActive(List<Vector3> newMarking, List<Vector3> newWorldMarking)
    {
        SetMarking(newMarking, newWorldMarking, lastImageSelected);
        galleryUI.updateUI(ImageMarkings, this);
    }

    public void ShowImage(int index)
    {
        lastImageSelected = index;
        galleryUI.showImage(ImageMarkings[index]);
    }

    public Texture GetActiveImage()
    {
        return ImageMarkings[lastImageSelected].Image;
    }

    // Applies the intersection algorithm to all images, where there is a marking.
    public void IntersectEverythingNew()
    {
        if (ImageMarkings.Count < 2)
        {
            //imageMarkings = DefaultImages.createImages();
            popupMessage.PopUp(PopupMessage.NotEnoughPhotos);
            return;
        }

        if (intersect.settings.getBoolByName("Smart Cube Adjustment")) NewAdjustCubePoints();

        if (!_unqualified) StartCoroutine(intersect.IntersectAreaNew(ImageMarkings.FindAll(im => im.Marking != null)));
    }

    // Automatic Cube Adjustment! Calculates the intersection of frustums from all positions at which the user has taken pictures and creates a
    // cube based on the intersection points of these frustums.
    private void NewAdjustCubePoints()
    {
        var cubeVolume = true;
        // We start out with the worst possible cube.

        intersect.settings.setXStart(-1000.0f);
        intersect.settings.setXEnd(1000.0f);
        intersect.settings.setYStart(-1000.0f);
        intersect.settings.setYEnd(1000.0f);
        intersect.settings.setZStart(-1000.0f);
        intersect.settings.setZEnd(1000.0f);

        var finalPointsX = new List<float>();
        var finalPointsY = new List<float>();
        var finalPointsZ = new List<float>();

        _unqualified = false;

        // We inspect every combination of images

        for (var i = 0; i < ImageMarkings.Count; i++)
            for (var j = 0; j < ImageMarkings.Count; j++)
            {
                if (i == j) continue;

                // [IMAGE I]
                // We inspect a plane made up of the device's position and lower left and upper left points of the carving.
                // We do the same for a plane made up of the device's position and lower right and upper right points of
                // the carving.

                var lowerLeftUpperLeft1 = new Plane(
                    Vector3.Cross(ImageMarkings[i].WorldPoints[4] + ImageMarkings[i].WorldPoints[0],
                        ImageMarkings[i].WorldPoints[4] + ImageMarkings[i].WorldPoints[1]).normalized,
                    ImageMarkings[i].WorldPoints[4]);

                var lowerRightUpperRight1 = new Plane(
                    Vector3.Cross(ImageMarkings[i].WorldPoints[4] + ImageMarkings[i].WorldPoints[2],
                        ImageMarkings[i].WorldPoints[4] + ImageMarkings[i].WorldPoints[3]).normalized,
                    ImageMarkings[i].WorldPoints[4]);

                // [IMAGE J]
                // We inspect rays made up of the device's position and the four corners of the carving, respectively.

                var ray1 = new Ray
                {
                    origin = ImageMarkings[j].WorldPoints[4],
                    direction = ImageMarkings[j].WorldPoints[0].normalized
                };

                var ray2 = new Ray
                {
                    origin = ImageMarkings[j].WorldPoints[4],
                    direction = ImageMarkings[j].WorldPoints[1].normalized
                };

                var ray3 = new Ray
                {
                    origin = ImageMarkings[j].WorldPoints[4],
                    direction = ImageMarkings[j].WorldPoints[2].normalized
                };

                var ray4 = new Ray
                {
                    origin = ImageMarkings[j].WorldPoints[4],
                    direction = ImageMarkings[j].WorldPoints[3].normalized
                };

                // We inspect the intersection points of the planes and rays.
                // If one intersection value is negative, it is located behind the user (intersection result is the
                // distance on the ray, starting at the device's position). So in that case we discard the iteration.
                // We store all the valid values in arrays.

                lowerRightUpperRight1.Raycast(ray1, out var intersection1);
                var intersection1Vec = ray1.GetPoint(intersection1);
                if (intersection1 < 0) continue;
                finalPointsX.Add(intersect.voxelGrid.gridSpace().InverseTransformPoint(intersection1Vec).x);
                finalPointsY.Add(intersect.voxelGrid.gridSpace().InverseTransformPoint(intersection1Vec).y);
                finalPointsZ.Add(intersect.voxelGrid.gridSpace().InverseTransformPoint(intersection1Vec).z);

                lowerRightUpperRight1.Raycast(ray2, out var intersection2);
                var intersection2Vec = ray2.GetPoint(intersection2);
                if (intersection2 < 0) continue;
                finalPointsX.Add(intersect.voxelGrid.gridSpace().InverseTransformPoint(intersection2Vec).x);
                finalPointsY.Add(intersect.voxelGrid.gridSpace().InverseTransformPoint(intersection2Vec).y);
                finalPointsZ.Add(intersect.voxelGrid.gridSpace().InverseTransformPoint(intersection2Vec).z);

                lowerRightUpperRight1.Raycast(ray3, out var intersection3);
                var intersection3Vec = ray3.GetPoint(intersection3);
                if (intersection3 < 0) continue;
                finalPointsX.Add(intersect.voxelGrid.gridSpace().InverseTransformPoint(intersection3Vec).x);
                finalPointsY.Add(intersect.voxelGrid.gridSpace().InverseTransformPoint(intersection3Vec).y);
                finalPointsZ.Add(intersect.voxelGrid.gridSpace().InverseTransformPoint(intersection3Vec).z);

                lowerRightUpperRight1.Raycast(ray4, out var intersection4);
                var intersection4Vec = ray4.GetPoint(intersection4);
                if (intersection4 < 0) continue;
                finalPointsX.Add(intersect.voxelGrid.gridSpace().InverseTransformPoint(intersection4Vec).x);
                finalPointsY.Add(intersect.voxelGrid.gridSpace().InverseTransformPoint(intersection4Vec).y);
                finalPointsZ.Add(intersect.voxelGrid.gridSpace().InverseTransformPoint(intersection4Vec).z);

                lowerLeftUpperLeft1.Raycast(ray1, out var intersection5);
                var intersection5Vec = ray1.GetPoint(intersection5);
                if (intersection5 < 0) continue;
                finalPointsX.Add(intersect.voxelGrid.gridSpace().InverseTransformPoint(intersection5Vec).x);
                finalPointsY.Add(intersect.voxelGrid.gridSpace().InverseTransformPoint(intersection5Vec).y);
                finalPointsZ.Add(intersect.voxelGrid.gridSpace().InverseTransformPoint(intersection5Vec).z);

                lowerLeftUpperLeft1.Raycast(ray2, out var intersection6);
                var intersection6Vec = ray2.GetPoint(intersection6);
                if (intersection6 < 0) continue;
                finalPointsX.Add(intersect.voxelGrid.gridSpace().InverseTransformPoint(intersection6Vec).x);
                finalPointsY.Add(intersect.voxelGrid.gridSpace().InverseTransformPoint(intersection6Vec).y);
                finalPointsZ.Add(intersect.voxelGrid.gridSpace().InverseTransformPoint(intersection6Vec).z);

                lowerLeftUpperLeft1.Raycast(ray3, out var intersection7);
                var intersection7Vec = ray3.GetPoint(intersection7);
                if (intersection7 < 0) continue;
                finalPointsX.Add(intersect.voxelGrid.gridSpace().InverseTransformPoint(intersection7Vec).x);
                finalPointsY.Add(intersect.voxelGrid.gridSpace().InverseTransformPoint(intersection7Vec).y);
                finalPointsZ.Add(intersect.voxelGrid.gridSpace().InverseTransformPoint(intersection7Vec).z);

                lowerLeftUpperLeft1.Raycast(ray4, out var intersection8);
                var intersection8Vec = ray4.GetPoint(intersection8);
                if (intersection8 < 0) continue;
                finalPointsX.Add(intersect.voxelGrid.gridSpace().InverseTransformPoint(intersection8Vec).x);
                finalPointsY.Add(intersect.voxelGrid.gridSpace().InverseTransformPoint(intersection8Vec).y);
                finalPointsZ.Add(intersect.voxelGrid.gridSpace().InverseTransformPoint(intersection8Vec).z);

                // If we made it through at least one iteration all the way until the end, then the angles of these two
                // pictures were good enough to generate a cube.

                cubeVolume = false;

                // We now choose the best values so far. However, we give a little wiggle room just in case.

                if (finalPointsX.Max() - finalPointsX.Min() < intersect.settings.getFloatByName("X end") -
                    intersect.settings.getFloatByName("X start"))
                {
                    intersect.settings.setXStart(finalPointsX.Min() - 0.01f);
                    intersect.settings.setXEnd(finalPointsX.Max() + 0.01f);
                }

                if (finalPointsY.Max() - finalPointsY.Min() < intersect.settings.getFloatByName("Y end") -
                    intersect.settings.getFloatByName("Y start"))
                {
                    intersect.settings.setYStart(finalPointsY.Min() - 0.01f);
                    intersect.settings.setYEnd(finalPointsY.Max() + 0.01f);
                }

                if (finalPointsZ.Max() - finalPointsZ.Min() < intersect.settings.getFloatByName("Z end") -
                    intersect.settings.getFloatByName("Z start"))
                {
                    intersect.settings.setZStart(finalPointsZ.Min() - 0.01f);
                    intersect.settings.setZEnd(finalPointsZ.Max() + 0.01f);
                }
            }

        var volume = (intersect.settings.getFloatByName("X end") - intersect.settings.getFloatByName("X start")) *
                     (intersect.settings.getFloatByName("Y end") - intersect.settings.getFloatByName("Y start")) *
                     (intersect.settings.getFloatByName("Z end") - intersect.settings.getFloatByName("Z start"));
        // If two pictures do not capture the object of interest from different enough positions and angles, the cube
        // intersection points will be located behind the user. In this case, the iteration is discarded. If all
        // iterations are discarded, the cube volume variable will be positive.
        if (volume == 0.0f || intersect.settings.getFloatByName("X start") < -999.0f ||
            intersect.settings.getFloatByName("Y start") < -999.0f ||
            intersect.settings.getFloatByName("Z start") < -999.0f ||
            intersect.settings.getFloatByName("X end") > 999.0f ||
            intersect.settings.getFloatByName("Y end") > 999.0f ||
            intersect.settings.getFloatByName("Z end") > 999.0f)
        {
            popupMessage.PopUp("Smart Cube Adjustment unsuccessful");
            _unqualified = true;
            return;
        }

        if (cubeVolume)
        {
            popupMessage.PopUp(PopupMessage.SameAngle);
            _unqualified = true;
        }

        var startVoxelCount = intersect.settings.getIntByName("Initial Voxel count");
        var voxelVolume = volume / startVoxelCount;
        var voxelSize = (float)Math.Pow(voxelVolume, 1.0f / 3.0f);
        //intersect.settings.setInitialVoxelSize(voxelSize); // Value gets rounded this way -> using other way
        intersect.autoVoxelSize = voxelSize;

        Debug.Log($"Automated VoxelSize: {voxelSize}");
    }
}