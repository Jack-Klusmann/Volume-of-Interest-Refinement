using UnityEngine;

public class GALLERY : MonoBehaviour
{
    [SerializeField] private GalleryStorage gallery;
    [SerializeField] private PopupMessage popupMessage;

    public void OnClick()
    {
        if (gallery.GetImageMarkingCount() > 0)
            GlobalContextVariable.updateValue(GlobalContextVariable.GlobalContextVariableValue.gallery);
        else
            popupMessage.PopUp(PopupMessage.NotEnoughPhotos);
    }
}