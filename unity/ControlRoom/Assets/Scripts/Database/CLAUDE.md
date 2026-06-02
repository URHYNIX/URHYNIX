# Assets/Scripts/Database/

> Supabase 제한 권한 클라이언트. service_role 키 절대 미반입.

## 예정 파일

| 파일 | 역할 |
|---|---|
| `SupabaseClient.cs` | REST/Edge Function 클라이언트 (anon key + UniTask) |
| `RobotConfigRepository.cs` | 로봇/기능/센서 설정 조회 |
| `ProtectedTargetRepository.cs` | 보호대상 저장/조회 |
| `SessionRepository.cs` | `session_meta` 저장/조회 |
| `EventRepository.cs` | `events` 저장/조회 |
| `DispatchRepository.cs` | `dispatches` 저장/조회 |
| `CameraRepository.cs` | `camera_captures` 저장/조회 |
| `PoseLogRepository.cs` ✅ | `pose_logs` read + 보조 INSERT (주 쓰기는 로봇 PC) |

## 권한 정책 (중요)

- **Unity = read + 제한 INSERT만**. `dispatches`(출동), `session_meta`(사람 액션)만 쓰기.
- **service_role 키 절대 미반입**. anon key는 `Resources/SupabaseConfig.local.asset` (`.gitignore`).
- 민감 작업(전원 종료/RLS 우회)은 Supabase **Edge Function** 호출만.
- **주 쓰기 주체는 로봇 PC** (Python ROS2 노드 + anon + RLS).

## Supabase 진입점

- URL: `https://ueupkrxwybuuqxflstvg.supabase.co`
- SDK: `supabase-csharp` (kamyker/supabase-unity 또는 NuGetForUnity)
- 비동기: UniTask 필수.

## 관련 스킬

- `supabase-mcp` — TaillogToss 운영 스킬, URHYNIX에 적용 시 schema/migration 참고 가능.
- `secret-scan` — 커밋 전 키 노출 점검.
