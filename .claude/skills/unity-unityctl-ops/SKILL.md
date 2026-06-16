---
name: unity-unityctl-ops
description: unityctl로 Unity 에디터를 헤드리스 제어할 때 반복해서 물리는 함정과 안전 절차 — Play 중 재컴파일 차단(stale check), IPC "not ready"/Busy, UI Toolkit 스크린샷 검정, 로봇-오프 ROS 재연결 스팸 교착. 2026-06-16 학습.
when_to_use:
  - 코드 수정 후 unityctl로 컴파일/Play 검증할 때
  - unityctl check가 stale하거나 ping이 "not ready"/에러일 때
  - 스크린샷이 검게 나올 때
  - Play가 IPC로 안 멈추거나 에디터가 묶였을 때
references:
  - .claude/skills/unity-live-map-twin/SKILL.md
  - .claude/skills/unity-camera-panel/SKILL.md
---

# unity-unityctl-ops

ControlRoom(`unity/ControlRoom`, `--project` 필수) 헤드리스 제어. `unityctl ping/check/asset refresh/play/log/screenshot/console`.

## 코드 수정 → 검증 안전 절차 (필수 순서)

```
1) unityctl play stop --project "$P"        # ★ 반드시 먼저. Play 중엔 재컴파일 안 됨.
2) unityctl asset refresh --project "$P"
3) isCompiling:false 될 때까지 폴링 후 결과 확인:
   unityctl check --project "$P" --json     # scriptCompilationFailed 봄
4) 성공 시 play start. 검증은 Editor.log(Debug.Log) + 로봇측 endpoint 로그.
```

## 함정 (반복 학습)

| 증상 | 원인 | 해결 |
|---|---|---|
| **수정해도 컴파일이 옛 에러/옛 성공 그대로(stale)** | **Play 모드 중엔 도메인 리로드 지연 → 재컴파일 안 됨**. check가 직전 결과 반환 | 변경 검증 전 **무조건 `play stop`**. 그 후 refresh+check. |
| check가 너무 빨리 "passed" | asset refresh가 schedule만 됨, 컴파일 시작 전에 check가 직전 결과 반환 | `isCompiling:false` **확인 + 충분 대기**(7s×폴링). 에러는 Editor.log `error CS`로 교차확인 |
| `ping` 등이 예외/`color or style 'Busy'` | 에디터 Busy(컴파일/리로드) 중 CLI 렌더 버그 | `--json` 사용. 메시지 "IPC not ready yet" 뜨면 대기 |
| IPC 장시간 "not ready" + Play 못 멈춤 | Play 중 asset refresh → 리로드가 Play 종료 대기에 걸려 교착 | Unity 창에서 **수동 Stop(■)**, 또는 Cmd+Q(코드는 디스크에 안전). 이후 IPC 복구 |
| **스크린샷이 전부 검정** | UI Toolkit은 카메라가 아니라 **스크린 오버레이 직접 렌더** → 카메라 RenderTexture 캡처에 안 잡힘 | 시각은 **Unity 창 직접 확인**. 동작 검증은 Editor.log Debug.Log + endpoint 로그로 |
| 로봇 OFF인데 Play → 에디터 버벅/교착 | ROSConnection이 죽은 IP로 재연결 폭주(~20ms마다 "Host is down" 스팸) | 로봇 OFF 동안엔 **Play 자제**. 검증은 컴파일까지만, 라이브는 로봇 ON 세션에서 일괄 |
| Unity가 `Connection refused`(로봇 켜져있는데) | 로봇측 **ros_tcp endpoint 크래시**(`InvalidHandle: destruction was requested`) — 구독 churn/wifi 끊김 스트레스 | 로봇에서 ros_tcp 세션만 재기동(bringup/slam 안 끊고). `urhynix-fullstack-bringup` 참고 |
| `RaiseLogAdded` 메시지가 Editor.log에 없음 | ControlRoomEvents 인앱 로그 패널 전용(파일 미기록) | 검증 필요한 지점엔 `Debug.Log` 별도 추가 |

## 실측 패턴

- 컴파일 대기 폴링: `for i in $(seq 1 12); do sleep 7; J=$(unityctl check --project "$P" --json); echo "$J"|grep -q '"isCompiling": false' && echo "$J"|grep -q '"scriptCompilationFailed": false' && break; done`
- Play/스크린샷 전 포그라운드: `osascript -e 'tell application "Unity" to activate'`
- 코드 동작 확인: `tail/grep ~/Library/Logs/Unity/Editor.log` (Debug.Log 출력처)
- PlayerPrefs 읽기(Mac): `defaults read unity.DefaultCompany.<product> '<key>'`

## 한줄정리

코드 검증 전 **반드시 play stop**(Play 중 컴파일/check는 stale). IPC 에러는 `--json`+대기, 스크린샷은 UI Toolkit이라 검정이니 Editor.log/endpoint 로그로 검증, 로봇 OFF 중엔 Play 자제.
