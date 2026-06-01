using UnityEngine;

public class BasicRigidBodyPush : MonoBehaviour
{
    public LayerMask pushLayers;
    public bool canPush = true; // 에디터에서 동작하도록 기본값을 true로 설정해두십시오.
    [Range(0.5f, 5f)] public float strength = 1.1f;

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (canPush) PushRigidBodies(hit);
    }

    private void PushRigidBodies(ControllerColliderHit hit)
    {
        // 1. 부딪힌 오브젝트에 Rigidbody가 있는지 확인 (정육면체는 Rigidbody가 필요합니다)
        Rigidbody body = hit.collider.attachedRigidbody;
        if (body == null || body.isKinematic) return;

        // 2. 지정된 레이어 검사
        var bodyLayerMask = 1 << body.gameObject.layer;
        if ((bodyLayerMask & pushLayers.value) == 0) return;

        // 3. 아래에 있는 오브젝트를 미는 것은 제외
        if (hit.moveDirection.y < -0.3f) return;

        // 4. 밀기 방향 계산 및 물리적인 힘(밀쳐내기) 적용
        Vector3 pushDir = new Vector3(hit.moveDirection.x, 0.0f, hit.moveDirection.z);
        body.AddForce(pushDir * strength, ForceMode.Impulse);
    }
}