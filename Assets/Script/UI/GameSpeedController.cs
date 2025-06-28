using UnityEngine;

public class GameSpeedController : MonoBehaviour
{
    public static float SpeedMultiplier { get;  set; } = 1f;

    public void SetSpeedTo1()
    {
        SpeedMultiplier = 1f;
    }

    public void SetSpeedTo2()
    {
        SpeedMultiplier = 2f;
    }

    public void SetSpeedTo3()
    {
        SpeedMultiplier = 3f;
    }
}
