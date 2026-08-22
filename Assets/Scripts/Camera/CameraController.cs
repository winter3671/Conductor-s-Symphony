using System.Collections;
using UnityEngine;
using ConductorSymphony.Player;
using ConductorSymphony.Utility;

namespace ConductorSymphony.CameraControl
{
    public class CameraController : MonoSingleton<CameraController>
    {
        [Header("Target Tracking")]
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 offset = new Vector3(0f, 0f, -10f);

        private Vector3 shakeOffset = Vector3.zero;
        private Coroutine shakeRoutine;
        private Coroutine hitStopRoutine;

        // 히트스탑에 정확히 0 대신 아주 작은 값을 쓰는 이유: RhythmManager/PlayerController 등 여러
        // 곳이 "Time.timeScale <= 0f"를 실제 일시정지 신호로 그대로 쓰고 있어(레벨업 카드/ESC 메뉴 등과
        // 동일 취급), 정확히 0이면 그 짧은 순간 입력/판정이 통째로 무시될 위험이 있다.
        private const float HitStopTimeScale = 0.05f;

        private void Start()
        {
            if (target == null && PlayerController.Instance != null)
            {
                target = PlayerController.Instance.transform;
            }
        }

        private void LateUpdate()
        {
            if (target == null) return;

            // Instantly lock camera to player position so player is strictly centered on screen
            transform.position = target.position + offset + shakeOffset;
        }

        // 타격감(Juice) - 짧은 카메라 쉐이크. duration/magnitude가 클수록 강하게 흔들림(예: PERFECT >
        // GREAT, 피격 > 일반 타격). Time.unscaledDeltaTime 기준이라 히트스탑(timeScale 저하)과 동시에
        // 걸어도 부드럽게 이어진다. 리듬 노트 위치는 SongTime(오디오 하드웨어 시계) 기준이라 카메라
        // 흔들림/timeScale과 무관하게 전혀 영향을 받지 않는다.
        public void Shake(float duration, float magnitude)
        {
            if (shakeRoutine != null) StopCoroutine(shakeRoutine);
            shakeRoutine = StartCoroutine(ShakeRoutine(duration, magnitude));
        }

        private IEnumerator ShakeRoutine(float duration, float magnitude)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                float damper = 1f - (elapsed / duration);
                shakeOffset = (Vector3)Random.insideUnitCircle * magnitude * damper;
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
            shakeOffset = Vector3.zero;
            shakeRoutine = null;
        }

        // 타격감(Juice) - 아주 짧은 하드 프레임 정지. RhythmNote 이동은 Time.deltaTime이 아니라
        // AudioLayerManager.SongTime(오디오 하드웨어 재생 위치)을 기준으로 하므로, timeScale을
        // 잠깐 낮춰도 리듬 판정/오디오 싱크는 전혀 어긋나지 않는다(DOCUMENTATION.md 트러블슈팅
        // §6 SongTime 참고).
        public void HitStop(float duration)
        {
            if (hitStopRoutine != null) StopCoroutine(hitStopRoutine);
            hitStopRoutine = StartCoroutine(HitStopRoutine(duration));
        }

        private IEnumerator HitStopRoutine(float duration)
        {
            // 이미 실제로 일시정지 중(레벨업 카드/ESC 메뉴/승패 화면 등)이면 건드리지 않는다 -
            // 히트스탑이 그 위에 값을 덮어썼다가 되돌리면서 의도치 않게 정지를 풀어버릴 수 있다.
            if (Time.timeScale <= 0f) yield break;

            float previousScale = Time.timeScale;
            Time.timeScale = HitStopTimeScale;

            yield return new WaitForSecondsRealtime(duration);

            // 히트스탑 도중 플레이어가 진짜 일시정지를 열었다면(Time.timeScale이 그 사이 다른 값으로
            // 바뀌었다면) 이 코루틴이 그 값을 덮어쓰지 않는다.
            if (Time.timeScale == HitStopTimeScale)
            {
                Time.timeScale = previousScale;
            }
            hitStopRoutine = null;
        }
    }
}
