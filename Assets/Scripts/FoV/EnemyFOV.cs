using System.Collections;
using UnityEngine;

public class EnemyFOV : MonoBehaviour
{
    [Header("FOV Settings")]
    [Range(0f, 30f)] public float viewRadius = 10f;   // 최대 시야 거리
    [Range(0f, 360f)] public float viewAngle = 90f;   // 시야각

    // 추가 기능: 의심표시(Question)를 경고표시(Alert)로 바꾸기 위한 기준 거리
    [Tooltip("이 거리 내에서 포착되면 의심 단계를 건너뛰고 즉시 경고 상태가 됩니다.")]
    public float alertCloseRadius = 4f;

    [Header("Layer Masks")]
    public LayerMask targetMask;
    public LayerMask obstacleMask;

    [HideInInspector] public Transform targetInFOV;

    // 실시간 상태 업데이트를 위해 대상 타겟의 컴포넌트를 참조합니다.
    private PlayerSensorController currentTargetSensor;

    private void Start()
    {
        StartCoroutine(FindTargetsWithDelay(0.2f));
    }

    private IEnumerator FindTargetsWithDelay(float delay)
    {
        while (true)
        {
            yield return new WaitForSeconds(delay);
            FindVisibleTargets();
        }
    }

    private void FindVisibleTargets()
    {
        Collider[] targetsInViewRadius = Physics.OverlapSphere(transform.position, viewRadius, targetMask);
        bool targetFoundThisFrame = false;

        for (int i = 0; i < targetsInViewRadius.Length; i++)
        {
            Transform target = targetsInViewRadius[i].transform;

            Vector3 myEyePosition = transform.position + (Vector3.up * 0.5f);
            Vector3 targetEyePosition = target.position + (Vector3.up * 1.0f);

            Vector3 dirToTarget = (targetEyePosition - myEyePosition).normalized;

            if (Vector3.Angle(transform.forward, dirToTarget) < viewAngle / 2)
            {
                float dstToTarget = Vector3.Distance(myEyePosition, targetEyePosition);

                if (!Physics.Raycast(myEyePosition, dirToTarget, dstToTarget, obstacleMask))
                {
                    targetInFOV = target;
                    targetFoundThisFrame = true;

                    // 플레이어에게서 센서 제어용 컨트롤러를 가져옵니다.
                    if (currentTargetSensor == null)
                    {
                        currentTargetSensor = target.GetComponent<PlayerSensorController>();
                    }

                    if (currentTargetSensor != null)
                    {
                        // [거리 검사] 거리에 따라 센서의 상태를 분기 처리합니다.
                        if (dstToTarget <= alertCloseRadius)
                        {
                            // 설정된 경고 거리 이내라면 즉시 경고 표시(!) 활성화
                            currentTargetSensor.SetSensorState(PlayerSensorController.SensorState.Alert);
                        }
                        else
                        {
                            // 시야 내에 있지만 거리가 멀다면 의심 표시(?) 활성화
                            currentTargetSensor.SetSensorState(PlayerSensorController.SensorState.Question);
                        }
                    }
                    break;
                }
            }
        }

        // 시야에서 플레이어가 사라졌다면 상태를 초기화합니다.
        if (!targetFoundThisFrame)
        {
            targetInFOV = null;
            if (currentTargetSensor != null)
            {
                currentTargetSensor.SetSensorState(PlayerSensorController.SensorState.None);
                currentTargetSensor = null;
            }
        }
    }

    public Vector3 DirFromAngle(float angleInDegrees, bool angleIsGlobal)
    {
        if (!angleIsGlobal) angleInDegrees += transform.eulerAngles.y;
        return new Vector3(Mathf.Sin(angleInDegrees * Mathf.Deg2Rad), 0, Mathf.Cos(angleInDegrees * Mathf.Deg2Rad));
    }
}