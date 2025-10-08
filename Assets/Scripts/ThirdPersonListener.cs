using UnityEngine;

[ExecuteInEditMode]
public class ThirdPersonListener : AkGameObj
{
    public Transform player;

    public override Vector3 GetPosition()
    {
        if (player == null)
        {
            Debug.LogWarning("ThirdPersonListener: Player reference not set!");
            return transform.position;
        }

        AkGameObj playerAkGameObj = player.GetComponent<AkGameObj>();
        
        if (playerAkGameObj == null)
        {
            Debug.LogWarning("ThirdPersonListener: Player missing AkGameObj component!");
            return transform.position;
        }

        return playerAkGameObj.GetPosition();
    }
}