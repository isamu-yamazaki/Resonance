using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Resonance.PlayerController;

public class Compass : MonoBehaviour
{
    public RawImage CompassImage;
    public TextMeshProUGUI CompassDirectionText;
    
    private Transform player;
    private Transform cam;

    private IEnumerator Start()
    {
        while (PlayerController.LocalPlayer == null || Camera.main == null)
        {
            yield return null;
        }

        player = PlayerController.LocalPlayer.transform;
        cam = Camera.main.transform;

        Debug.Log("[Compass] Bound to camera");
    }

    void Update()
    {
        if (cam == null) return;

        CompassImage.uvRect = new Rect(cam.localEulerAngles.y / 360, 0, 1, 1);

        Vector3 forward = cam.forward;
        forward.y = 0;

        float headingAngle = Quaternion.LookRotation(forward).eulerAngles.y;
        headingAngle = 5 * (Mathf.RoundToInt(headingAngle / 5.0f));

        int displayangle = Mathf.RoundToInt(headingAngle);

        switch (displayangle)
        {
            case 0:
                CompassDirectionText.text = "N";
                break;
            case 360:
                CompassDirectionText.text = "N";
                break;
            case 45:
                CompassDirectionText.text = "NE";
                break;
            case 90:
                CompassDirectionText.text = "E";
                break;
            case 135:
                CompassDirectionText.text = "SE";
                break;
            case 180:
                CompassDirectionText.text = "S";
                break;
            case 225:
                CompassDirectionText.text = "SW";
                break;
            case 270:
                CompassDirectionText.text = "W";
                break;
            default:
                CompassDirectionText.text = headingAngle.ToString ();
                break;
        }
    }
}