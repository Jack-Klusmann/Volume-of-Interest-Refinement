using UnityEngine;

public class SD_YES : MonoBehaviour
{
    [SerializeField] private SP_Controller spController;
    [SerializeField] private ScreenDrawing drawingImage;
    [SerializeField] private GalleryStorage gallery;

    public void OnClick()
    {
        switch (GlobalContextVariable.globalContextVariable)
        {
            case GlobalContextVariable.GlobalContextVariableValue.photo_drawing:
                spController.SaveImageMarking();
                drawingImage.GenerateWorldPoints();
                GlobalContextVariable.updateValue(GlobalContextVariable.GlobalContextVariableValue.main_view);
                break;
            case GlobalContextVariable.GlobalContextVariableValue.gallery_drawing:
                spController.UpdateImageMarking();
                drawingImage.camPos.mainCameraCopy.transform.position.Set(
                    gallery.ImageMarkings[gallery.lastImageSelected].Position.x,
                    gallery.ImageMarkings[gallery.lastImageSelected].Position.y,
                    gallery.ImageMarkings[gallery.lastImageSelected].Position.z);
                drawingImage.camPos.mainCameraCopy.transform.rotation.Set(
                    gallery.ImageMarkings[gallery.lastImageSelected].Rotation.w,
                    gallery.ImageMarkings[gallery.lastImageSelected].Rotation.x,
                    gallery.ImageMarkings[gallery.lastImageSelected].Rotation.y,
                    gallery.ImageMarkings[gallery.lastImageSelected].Rotation.z);
                drawingImage.GenerateWorldPoints();
                GlobalContextVariable.updateValue(GlobalContextVariable.GlobalContextVariableValue.gallery);
                break;
        }

        drawingImage.fingerLifted = false;
    }
}