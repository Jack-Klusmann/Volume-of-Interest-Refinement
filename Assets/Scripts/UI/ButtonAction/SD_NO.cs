using UnityEngine;

public class SD_NO : MonoBehaviour
{
    [SerializeField] private ScreenDrawing drawing;

    public void OnClick()
    {
        switch (GlobalContextVariable.globalContextVariable)
        {
            case GlobalContextVariable.GlobalContextVariableValue.gallery_drawing:
                GlobalContextVariable.updateValue(GlobalContextVariable.GlobalContextVariableValue.gallery);
                break;
            case GlobalContextVariable.GlobalContextVariableValue.photo_drawing:
            case GlobalContextVariable.GlobalContextVariableValue.settings:
                GlobalContextVariable.updateValue(GlobalContextVariable.GlobalContextVariableValue.main_view);
                break;
        }

        if(drawing != null)
            drawing.fingerLifted = false;
    }
}