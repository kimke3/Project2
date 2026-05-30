using System.Collections;
using UnityEngine;

public class EnemyFOV : MonoBehaviour
{
    [Header("FOV Settings")]
    [Range(0f, 30f)] public float viewRadius = 10f;   // 시야 범위 (반지름 거리)
    [Range(0f, 360f)] public float viewAngle = 90f;   // 시야각 (정면 기준 전체 각도)

    [Header("Layer Masks")]
    public LayerMask targetMask;   // 감지할 대상 레이어 (Player)
    public LayerMask obstacleMask; // 시야를 가리는 장애물 레이어 (Wall, Obstacle 등)

    [HideInInspector]
    public Transform targetInFOV;  // 시야 내에 감지된 플레이어의 Transform

    private void Start()
    {
        // 연산 부하를 줄이기 위해 코루틴을 활용하여 주기적으로 타겟 탐색
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
        targetInFOV = null;

        // 1. 지정된 반지름 내에 있는 대상 레이어의 콜라이더들을 검출
        Collider[] targetsInViewRadius = Physics.OverlapSphere(transform.position, viewRadius, targetMask);

        for (int i = 0; i < targetsInViewRadius.Length; i++)
        {
            Transform target = targetsInViewRadius[i].transform;

            // ====================================================================
            // [오차 보정] 정확한 시야 검사를 위해 Y축(높이) 보정값을 적용합니다.
            // transform.position(발밑) 대신 눈높이에 맞춘 임시 벡터를 생성합니다.
            // ====================================================================
            Vector3 myEyePosition = transform.position + (Vector3.up * 0.5f); // 적의 눈높이 (중심점에서 약간 위)
            Vector3 targetEyePosition = target.position + (Vector3.up * 1.0f); // 플레이어의 눈높이 (머리~가슴 부근)

            // 적의 눈높이에서 타겟의 눈높이로 향하는 방향 벡터 계산 및 정규화
            Vector3 dirToTarget = (targetEyePosition - myEyePosition).normalized;

            // 2. 적이 바라보는 정면 방향(transform.forward)과 타겟 방향 사이의 각도가 시야각 범위 내인지 확인
            if (Vector3.Angle(transform.forward, dirToTarget) < viewAngle / 2)
            {
                // 적과 타겟 사이의 실제 거리를 계산
                float dstToTarget = Vector3.Distance(myEyePosition, targetEyePosition);

                // 3. 시야 위치(myEyePosition)에서 레이캐스트를 발사하여 장애물에 가려지는지 검사
                if (!Physics.Raycast(myEyePosition, dirToTarget, dstToTarget, obstacleMask))
                {
                    targetInFOV = target;
                    Debug.Log($"<color=red>[시야 감지]</color> 플레이어('{target.name}')를 정상적으로 포착했습니다!");
                }
            }
        }
    }

    // 에디터(Editor) 상에서 시야각을 시각적으로 표현하기 위한 방향 벡터 계산 함수
    public Vector3 DirFromAngle(float angleInDegrees, bool angleIsGlobal)
    {
        if (!angleIsGlobal)
        {
            angleInDegrees += transform.eulerAngles.y;
        }
        return new Vector3(Mathf.Sin(angleInDegrees * Mathf.Deg2Rad), 0, Mathf.Cos(angleInDegrees * Mathf.Deg2Rad));
    }
}