using UnityEngine;

public class DualLayout : MonoBehaviour
{
    public RectTransform rectTransform;
    public RectTransform verticalTransform;
    public RectTransform horizontalTransform;

    void Start()
    {
        
    }

    void Update()
    {
        
    }

    public void SetVerticalLayout()
    {
        //anchor preset
        rectTransform.anchorMin = verticalTransform.anchorMin;
        rectTransform.anchorMax = verticalTransform.anchorMax;

        rectTransform.pivot = verticalTransform.pivot;

        rectTransform.position = verticalTransform.position;
    }

    public void SetHorizontalLayout()
    {
        //anchor preset
        rectTransform.anchorMin = horizontalTransform.anchorMin;
        rectTransform.anchorMax = horizontalTransform.anchorMax;

        rectTransform.pivot = horizontalTransform.pivot;
        rectTransform.position = horizontalTransform.position;
    }
}
