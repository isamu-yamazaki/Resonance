using System;
using UnityEngine;

public class CrosshairFollow : MonoBehaviour
{
    RectTransform rectTransform;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }
    private void Update()
    {
        FollowMouse();
    }

    private void FollowMouse()
    {
        rectTransform.position = Input.mousePosition;
    }
}
