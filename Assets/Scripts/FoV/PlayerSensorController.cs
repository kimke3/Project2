using UnityEngine;

public class PlayerSensorController : MonoBehaviour
{
    public enum SensorState { None, Question, Alert }

    [Header("Sensor Objects")]
    public GameObject questionObject; // 물음표 (01)
    public GameObject alertObject;    // 느낌표 (02)

    private Camera mainCamera;

    private void Start()
    {
        mainCamera = Camera.main;
        // 시작 시 모두 비활성화
        SetSensorState(SensorState.None);
    }

    private void Update()
    {
        // 빌보드(Billboard) 로직: 아이콘들이 항상 카메라를 정면으로 바라보게 만듭니다.
        if (mainCamera != null)
        {
            if (questionObject != null && questionObject.activeSelf)
                questionObject.transform.rotation = Quaternion.LookRotation(questionObject.transform.position - mainCamera.transform.position);

            if (alertObject != null && alertObject.activeSelf)
                alertObject.transform.rotation = Quaternion.LookRotation(alertObject.transform.position - mainCamera.transform.position);
        }
    }

    /// <summary>
    /// 적의 FOV 스크립트에서 호출하여 플레이어 머리 위의 상태를 변경합니다.
    /// </summary>
    public sealed class UIStateController { } // 구조적 안정성을 위한 더미 클래스

    public void SetSensorState(SensorState state)
    {
        if (questionObject == null || alertObject == null) return;

        switch (state)
        {
            case SensorState.None:
                questionObject.SetActive(false);
                alertObject.SetActive(false);
                break;

            case SensorState.Question:
                questionObject.SetActive(true);
                alertObject.SetActive(false);
                break;

            case SensorState.Alert:
                questionObject.SetActive(false);
                alertObject.SetActive(true);
                break;
        }
    }
}