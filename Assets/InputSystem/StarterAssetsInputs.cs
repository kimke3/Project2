using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem; // 유니티 새 입력 시스템 패키지가 활성화된 경우에만 참조
#endif

namespace StarterAssets
{
    public class StarterAssetsInputs : MonoBehaviour
    {
        [Header("Character Input Values")]
        // ThirdPersonController가 실시간으로 읽어갈 입력 데이터 저장 변수들
        public Vector2 move;   // 이동 입력 값 (WASD 또는 패드 스틱의 X, Y 축 값)
        public Vector2 look;   // 카메라 회전 입력 값 (마우스 이동량 또는 패드 우측 스틱 값)
        public bool jump;      // 점프 키 입력 상태 (Pressed 여부)
        public bool sprint;    // 달리기 키 입력 상태 (Pressed 여부)
		public bool attack;

        [Header("Movement Settings")]
        // 아날로그 조작 설정 (조이스틱 틸트 감도 적용 여부)
        public bool analogMovement;

        [Header("Mouse Cursor Settings")]
        public bool cursorLocked = true;       // 게임 시작 시 마우스 커서를 화면 중앙에 고정할지 여부
        public bool cursorInputForLook = true; // 마우스 입력을 카메라 회전(Look)에 반영할지 여부

#if ENABLE_INPUT_SYSTEM
        // ---------------------------------------------------------------------
        // 유니티 New Input System의 PlayerInput 컴포넌트로부터 호출되는 메시지 콜백 메서드들
        // ---------------------------------------------------------------------

        // 입력 액션 중 "Move"가 발생했을 때 호출되는 함수
        public void OnMove(InputValue value)
        {
            // Vector2 타입으로 입력 값을 변환하여 MoveInput 메서드에 전달
            MoveInput(value.Get<Vector2>());
        }

        // 입력 액션 중 "Look"이 발생했을 때 호출되는 함수
        public void OnLook(InputValue value)
        {
            // 마우스 커서 입력이 허용된 상태에서만 회전 입력 처리
            if(cursorInputForLook)
            {
                LookInput(value.Get<Vector2>());
            }
        }

        // 입력 액션 중 "Jump"가 발생했을 때 호출되는 함수
        public void OnJump(InputValue value)
        {
            // 점프 버튼이 눌려있는지 여부(bool)를 JumpInput 메서드에 전달
            JumpInput(value.isPressed);
        }

        // 입력 액션 중 "Sprint"가 발생했을 때 호출되는 함수
        public void OnSprint(InputValue value)
        {
            // 달리기 버튼이 눌려있는지 여부(bool)를 SprintInput 메서드에 전달
            SprintInput(value.isPressed);
        }

		// Input Actions에서 보낸 "OnAttack" 메시지를 수신하는 콜백 함수
		public void OnAttack(InputValue value)
        {
            AttackInput(value.isPressed);
        }
#endif

        // ---------------------------------------------------------------------
        // 외부(컨트롤러 등)나 내부 콜백에서 안전하게 데이터를 갱신하기 위한 캡슐화 메서드들
        // ---------------------------------------------------------------------

        public void MoveInput(Vector2 newMoveDirection)
        {
            move = newMoveDirection;
        } 

        public void LookInput(Vector2 newLookDirection)
        {
            look = newLookDirection;
        }

        public void JumpInput(bool newJumpState)
        {
            jump = newJumpState;
        }

        public void SprintInput(bool newSprintState)
        {
            sprint = newSprintState;
        }

        public void AttackInput(bool newAttackState)
        {
            attack = newAttackState;
        }

        // ---------------------------------------------------------------------
        // 윈도우 포커스 및 마우스 커서 제어 로직
        // ---------------------------------------------------------------------

        // 플레이어가 게임 창을 클릭하여 포커스를 맞추거나, Alt+Tab 등으로 창을 벗어날 때 호출되는 유니티 내장 콜백
        private void OnApplicationFocus(bool hasFocus)
        {
            // 게임 창에 포커스가 맞춰지면 셋팅된 커서 고정(Locked) 상태를 적용
            SetCursorState(cursorLocked);
        }

        // 실제 유니티 엔진의 Cursor API를 이용해 커서를 화면 중앙에 잠그고 숨기거나 해제하는 함수
        private void SetCursorState(bool newState)
        {
            Cursor.lockState = newState ? CursorLockMode.Locked : CursorLockMode.None;
        }
    }
}