using UnityEngine;
#if ENABLE_INPUT_SYSTEM 
using UnityEngine.InputSystem; // 유니티 새 입력 시스템(New Input System) 사용 시 컴파일에 포함
#endif

namespace StarterAssets
{
    // 클래스 실행에 필요한 컴포넌트를 강제로 부착하도록 보장하는 어트리뷰트
    [RequireComponent(typeof(CharacterController))]
#if ENABLE_INPUT_SYSTEM 
    [RequireComponent(typeof(PlayerInput))]
#endif
    public class ThirdPersonController : MonoBehaviour
    {
        [Header("Player")]
        public float MoveSpeed = 2.0f; // 캐릭터의 기본 이동 속도 (m/s)
        public float SprintSpeed = 5.335f; // 캐릭터의 달리기 속도 (m/s)

        [Range(0.0f, 0.3f)]
        public float RotationSmoothTime = 0.12f; // 캐릭터가 이동 방향을 향해 회전하는 부드러움의 정도

        public float SpeedChangeRate = 10.0f; // 가속 및 감속 비율

        // 오디오 클립 및 사운드 설정 변수
        public AudioClip LandingAudioClip;
        public AudioClip[] FootstepAudioClips;
        [Range(0, 1)] public float FootstepAudioVolume = 0.5f;

        [Space(10)]
        public float JumpHeight = 1.2f; // 플레이어가 점프할 수 있는 목표 높이
        public float Gravity = -15.0f; // 캐릭터 자체에 적용할 독자적인 중력값 (유니티 기본 엔진 기본값은 -9.81f)

        [Space(10)]
        public float JumpTimeout = 0.50f; // 재점프가 가능해지기까지 걸리는 대기 시간 (0f 설정 시 즉시 재점프 가능)
        public float FallTimeout = 0.15f; // 낙하 상태(Fall State)로 진입하기 전 대기 시간. 계단을 걸어 내려갈 때 의도치 않게 낙하 애니메이션이 뜨는 것을 방지

        [Header("Player Grounded")]
        public bool Grounded = true; // 현재 캐릭터가 지면에 닿아 있는지 여부 (CharacterController 자체 기능 대신 독자적 체크 사용)
        public float GroundedOffset = -0.14f; // 울퉁불퉁한 지형 처리를 위한 지면 체크 구체의 오프셋(높이 보정값)
        public float GroundedRadius = 0.28f; // 지면 체크 구체의 반지름. CharacterController의 반지름과 일치시키는 것이 좋음
        public LayerMask GroundLayers; // 어떤 레이어(Layer)를 지면으로 인식할 것인지 설정

        [Header("Cinemachine")]
        public GameObject CinemachineCameraTarget; // 시네머신 가상 카메라(Cinemachine Virtual Camera)가 추적할 타겟 오브젝트
        public float TopClamp = 70.0f; // 카메라를 위로 얼마나 들어 올릴 수 있는지 제한하는 각도 (최대 상향각)
        public float BottomClamp = -30.0f; // 카메라를 아래로 얼마나 내릴 수 있는지 제한하는 각도 (최대 하향각)
        public float CameraAngleOverride = 0.0f; // 카메라 각도를 미세 조정하거나 고정할 때 사용할 추가 오버라이드 각도
        public bool LockCameraPosition = false; // 모든 축에 대해 카메라 위치를 고정할지 여부

        // 시네머신 카메라 내부 계산용 변수 (Yaw: 좌우 회전, Pitch: 상하 회전)
        private float _cinemachineTargetYaw;
        private float _cinemachineTargetPitch;

        // 플레이어 내부 상태 변수
        private float _speed;              // 현재 수평 속도
        private float _animationBlend;     // 애니메이션 블렌드 트리에 전달할 보간된 속도값
        private float _targetRotation = 0.0f; // 목표 회전 각도
        private float _rotationVelocity;   // 회전 감속용 내부 속도 변수
        private float _verticalVelocity;   // 수직 속도 (점프/중력 계산용)
        private float _terminalVelocity = 53.0f; // 최대 추락 속도 (종단 속도 제한)

        // 타이머 카운트용 변수 (Delta Time 차감용)
        private float _jumpTimeoutDelta;
        private float _fallTimeoutDelta;

        // 애니메이션 파라미터 ID 최적화용 캐싱 변수 (정수형 해시값 사용)
        private int _animIDSpeed;
        private int _animIDGrounded;
        private int _animIDJump;
        private int _animIDFreeFall;
        private int _animIDMotionSpeed;
        private int _animIDAttack;

#if ENABLE_INPUT_SYSTEM 
        private PlayerInput _playerInput;
#endif
        private Animator _animator;
        private CharacterController _controller;
        private StarterAssetsInputs _input;
        private GameObject _mainCamera;

        private const float _threshold = 0.01f; // 입력 오차 무시를 위한 최소 임계값

        private bool _hasAnimator; // Animator 컴포넌트 존재 여부 캐싱

        // 현재 사용 중인 입력 장치가 마우스/키보드인지 판별하는 프로퍼티
        private bool IsCurrentDeviceMouse
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                return _playerInput.currentControlScheme == "KeyboardMouse";
#else
                return false;
#endif
            }
        }

        private void Awake()
        {
            // 태그를 통해 씬 내의 메인 카메라를 찾아 참조를 확보
            if (_mainCamera == null)
            {
                _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
            }
        }

        private void Start()
        {
            // 시작 시 카메라 타겟의 초기 y축 회전값을 Yaw 각도 초기값으로 설정
            _cinemachineTargetYaw = CinemachineCameraTarget.transform.rotation.eulerAngles.y;

            // 필수 컴포넌트들을 가져와 변수에 할당 (컴포넌트 캐싱)
            _hasAnimator = TryGetComponent(out _animator);
            _controller = GetComponent<CharacterController>();
            _input = GetComponent<StarterAssetsInputs>();
#if ENABLE_INPUT_SYSTEM 
            _playerInput = GetComponent<PlayerInput>();
#else
            // 새 입력 시스템 패키지가 없거나 의존성 에러가 날 경우 경고 출력
            Debug.LogError( "Starter Assets package is missing dependencies. Please use Tools/Starter Assets/Reinstall Dependencies to fix it");
#endif

            // 애니메이션 문자열 파라미터들을 정수 해시 아이디로 변환하여 할당
            AssignAnimationIDs();

            // 시작 시 타이머 변수들을 설정한 대기 시간 초기값으로 셋팅
            _jumpTimeoutDelta = JumpTimeout;
            _fallTimeoutDelta = FallTimeout;
        }

        private void Update()
        {
            // 실시간으로 애니메이터 컴포넌트가 유효한지 다시 확인
            _hasAnimator = TryGetComponent(out _animator);

            // 매 프레임 실행되어야 하는 핵심 상태 함수 호출 순서
            JumpAndGravity();   // 1. 점프 입력 처리 및 중력 적용
            GroundedCheck();    // 2. 지면 접지 상태 체크
            Move();             // 3. 이동 및 애니메이션 갱신
            HandleAttack();     // 4. 공격 입력 처리 및 애니메이션 실행
        }

        private void LateUpdate()
        {
            // 캐릭터 이동이 완료된 후, 카메라 회전을 안전하게 처리하기 위해 LateUpdate에서 실행
            CameraRotation();
        }

        private void AssignAnimationIDs()
        {
            // 문자열 연산을 피하기 위해 고유 정수 해시 코드로 변환 (런타임 최적화)
            _animIDSpeed = Animator.StringToHash("Speed");
            _animIDGrounded = Animator.StringToHash("Grounded");
            _animIDJump = Animator.StringToHash("Jump");
            _animIDFreeFall = Animator.StringToHash("FreeFall");
            _animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
            _animIDAttack = Animator.StringToHash("Attack");
        }

        private void GroundedCheck()
        {
            // 캐릭터의 중심점 하단에 오프셋을 적용하여 물리 검사용 구체의 위치 계산
            Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z);

            // Physics.CheckSphere를 이용해 해당 반경 내에 GroundLayers에 속한 콜라이더가 있는지 판별 (트리거 속성은 무시)
            Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers, QueryTriggerInteraction.Ignore);

            // 애니메이터가 존재하면 접지 상태(bool)를 파라미터에 동기화
            if (_hasAnimator)
            {
                _animator.SetBool(_animIDGrounded, Grounded);
            }
        }

        private void CameraRotation()
        {
            // 입력된 look(마우스/스틱 움직임) 크기가 임계값 이상이고, 카메라 위치가 고정되지 않은 경우에만 회전 처리
            if (_input.look.sqrMagnitude >= _threshold && !LockCameraPosition)
            {
                // 마우스 입력은 프레임률(FPS)의 영향을 받지 않으므로 1.0을 곱하고, 컨트롤러 패드는 Time.deltaTime을 곱함
                float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;

                _cinemachineTargetYaw += _input.look.x * deltaTimeMultiplier;
                _cinemachineTargetPitch += _input.look.y * deltaTimeMultiplier;
            }

            // 회전각이 무한대로 커지거나 작아지지 않도록 360도 안에서 회전 제한 처리
            _cinemachineTargetYaw = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
            _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

            // 최종 계산된 각도를 시네머신의 추적 타겟(Target)의 쿼터니언 회전값으로 적용
            CinemachineCameraTarget.transform.rotation = Quaternion.Euler(_cinemachineTargetPitch + CameraAngleOverride, _cinemachineTargetYaw, 0.0f);
        }

        private void Move()
        {
            // 시프트(Sprint) 입력 여부에 따라 목표 속도를 달리기 또는 걷기 속도로 이원화
            float targetSpeed = _input.sprint ? SprintSpeed : MoveSpeed;

            // 이동 입력이 전혀 없는 상태(방향키를 안 누름)라면 목표 속도를 0으로 변환
            if (_input.move == Vector2.zero) targetSpeed = 0.0f;

            // 캐릭터의 현재 수평(X, Z축) 속도의 크기(Magnitude)를 구함
            float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z).magnitude;

            float speedOffset = 0.1f;
            // 패드 스틱의 미세 조작 여부에 따른 입력 강도 반영 (키보드는 항시 1f)
            float inputMagnitude = _input.analogMovement ? _input.move.magnitude : 1f;

            // 현재 수평 속도가 목표 속도 범위(오차 범위 +-0.1f)를 벗어나 있다면 가속 또는 감속 보간 수행
            if (currentHorizontalSpeed < targetSpeed - speedOffset ||
                currentHorizontalSpeed > targetSpeed + speedOffset)
            {
                // Mathf.Lerp를 이용하여 직선형 가감속이 아닌 곡선 형태의 유기적인 속도 변화 그래프를 시뮬레이션
                _speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude,
                    Time.deltaTime * SpeedChangeRate);

                // 연산 정밀도 확보를 위해 소수점 셋째 자리까지 반올림 처리
                _speed = Mathf.Round(_speed * 1000f) / 1000f;
            }
            else
            {
                // 목표 속도 범위 내에 들어왔다면 목표 속도로 값을 확정
                _speed = targetSpeed;
            }

            // 애니메이션 블렌딩용 속도 보간 연산 수행
            _animationBlend = Mathf.Lerp(_animationBlend, targetSpeed, Time.deltaTime * SpeedChangeRate);
            if (_animationBlend < 0.01f) _animationBlend = 0f;

            // 입력받은 Vector2 이동 방향 데이터를 수평 Vector3 방향 데이터로 규격화(정규화)
            Vector3 inputDirection = new Vector3(_input.move.x, 0.0f, _input.move.y).normalized;

            // 이동 입력을 하고 있는 상태라면 캐릭터가 카메라 정면 방향을 기준으로 회전하도록 연산
            if (_input.move != Vector2.zero)
            {
                // 아크탄젠트(Atan2) 공식으로 입력 방향의 각도를 구한 뒤 메인 카메라의 y축 회전량을 더해 줌
                _targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg +
                                  _mainCamera.transform.eulerAngles.y;

                // SmoothDampAngle을 사용하여 현재 각도에서 목표 각도까지 부드럽게 회전 연산 진행
                float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation, ref _rotationVelocity,
                    RotationSmoothTime);

                // 계산된 Y축 회전값을 캐릭터의 실제 트랜스폼 회전에 적용
                transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
            }

            // 목표 회전 방향을 기준으로 앞 방향 벡터를 연산
            Vector3 targetDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;

            // CharacterController 컴포넌트를 이용해 최종 캐릭터를 물리 이동 시킴 (수평 이동 + 수직 이동 분리 합산)
            _controller.Move(targetDirection.normalized * (_speed * Time.deltaTime) +
                             new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);

            // 애니메이터 변수 갱신 (블렌드 트리용 Speed값과 패드 입력 강도용 MotionSpeed 전달)
            if (_hasAnimator)
            {
                _animator.SetFloat(_animIDSpeed, _animationBlend);
                _animator.SetFloat(_animIDMotionSpeed, inputMagnitude);
            }
        }

        private void JumpAndGravity()
        {
            if (Grounded) // 지면에 닿아 있는 상태
            {
                // 낙하 대기시간 타이머 초기화
                _fallTimeoutDelta = FallTimeout;

                // 애니메이터 내 점프 및 자유낙하 플래그 꺼줌
                if (_hasAnimator)
                {
                    _animator.SetBool(_animIDJump, false);
                    _animator.SetBool(_animIDFreeFall, false);
                }

                // 수직 낙하 속도가 계속 무한대로 떨어지는 현상을 막기 위해 수직 속도를 소량의 마이너스값(-2f)으로 고정
                if (_verticalVelocity < 0.0f)
                {
                    _verticalVelocity = -2f;
                }

                // 점프 입력이 들어왔고, 점프 쿨타임(Timeout)이 끝났다면 점프 처리 수행
                if (_input.jump && _jumpTimeoutDelta <= 0.0f)
                {
                    // 물리학 공식인 [v = sqrt(h * -2 * g)]를 대입하여 목표 높이에 정확히 도달하기 위한 수직 초기 속도 산출
                    _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);

                    // 애니메이터에 점프 활성화 트리거 작동
                    if (_hasAnimator)
                    {
                        _animator.SetBool(_animIDJump, true);
                    }
                }

                // 점프 쿨타임 타이머 실시간 차감
                if (_jumpTimeoutDelta >= 0.0f)
                {
                    _jumpTimeoutDelta -= Time.deltaTime;
                }
            }
            else // 공중에 떠 있는 낙하/점프 상태
            {
                // 점프 타이머 초기화 유지
                _jumpTimeoutDelta = JumpTimeout;

                // 공중에 머문 시간이 낙하 시간 제한(FallTimeout)을 넘겼다면 
                if (_fallTimeoutDelta >= 0.0f)
                {
                    _fallTimeoutDelta -= Time.deltaTime;
                }
                else
                {
                    // 자유 낙하(FreeFall) 애니메이션 상태를 활성화
                    if (_hasAnimator)
                    {
                        _animator.SetBool(_animIDFreeFall, true);
                    }
                }

                // 공중 상태에서는 연속 점프가 불가능하도록 점프 입력 상태를 강제로 꺼버림
                _input.jump = false;
            }

            // 수직 속도가 한계 속도(Terminal Velocity) 미만일 때만 중력을 시간에 따라 가속 누적시킴
            if (_verticalVelocity < _terminalVelocity)
            {
                _verticalVelocity += Gravity * Time.deltaTime;
            }
        }

        // 회전 각도가 오버플로우되거나 언더플로우되지 않도록 -360도 ~ 360도 범위 내로 고정 및 클램프하는 유틸리티 메서드
        private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
        {
            if (lfAngle < -360f) lfAngle += 360f;
            if (lfAngle > 360f) lfAngle -= 360f;
            return Mathf.Clamp(lfAngle, lfMin, lfMax);
        }

        // 유니티 에디터의 씬(Scene) 뷰 화면에서 컴포넌트가 선택되었을 때 디버깅용 기즈모(Gizmo)를 그려주는 함수
        private void OnDrawGizmosSelected()
        {
            Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f); // 땅에 닿았을 때 (반투명 녹색)
            Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);   // 공중에 떴을 때 (반투명 적색)

            if (Grounded) Gizmos.color = transparentGreen;
            else Gizmos.color = transparentRed;

            // 실제 지면 판정에 쓰이는 Physics.CheckSphere와 동일한 위치, 동일한 반지름을 가진 구체를 화면에 렌더링
            Gizmos.DrawSphere(
            new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z),
                GroundedRadius);
        }

        // 애니메이션 클립 내부 프레임에 박혀 있는 발소리(Footstep) 애니메이션 이벤트 콜백 메서드
        private void OnFootstep(AnimationEvent animationEvent)
        {
            // 애니메이션 가중치가 0.5 이상일 때만 (즉, 블렌딩 중 불완전한 상태가 아닐 때만) 소리 재생
            if (animationEvent.animatorClipInfo.weight > 0.5f)
            {
                if (FootstepAudioClips.Length > 0)
                {
                    // 랜덤하게 하나의 발소리 음원을 선택한 뒤 캐릭터 발 밑 위치에 3D 사운드로 재생
                    var index = Random.Range(0, FootstepAudioClips.Length);
                    AudioSource.PlayClipAtPoint(FootstepAudioClips[index], transform.TransformPoint(_controller.center), FootstepAudioVolume);
                }
            }
        }

        // 애니메이션 클립 내부 프레임에 박혀 있는 착지(Land) 애니메이션 이벤트 콜백 메서드
        private void OnLand(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f)
            {
                // 착지 사운드를 캐릭터 콜라이더 중심 위치 기준으로 재생
                AudioSource.PlayClipAtPoint(LandingAudioClip, transform.TransformPoint(_controller.center), FootstepAudioVolume);
            }
        }

        private void HandleAttack()
        {
            // 데이터 바구니(_input)에서 attack 버튼이 눌렸인지 확인
            if (_input.attack)
            {
                // 공격 애니메이션 실행 및 데미지 판정 로직 처리
                Debug.Log("공격 개시!");
                _animator.SetTrigger(_animIDAttack);

                // 단발성 입력인 경우 처리가 끝난 후 플래그를 수동으로 꺼주는 것이 좋습니다.
                _input.attack = false;
            }
        }
    }
}