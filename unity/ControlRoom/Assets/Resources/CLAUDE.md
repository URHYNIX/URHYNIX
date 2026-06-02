# Assets/Resources/

> `Resources.Load<T>(path)`로 런타임에 로드되는 자산.

## 예정 자산

| 파일 | 역할 |
|---|---|
| `SupabaseConfig.template.asset` | Supabase URL + anon key **빈 값** 템플릿 (커밋 OK) |
| `SupabaseConfig.local.asset` | 실제 anon key 박힘 — **`.gitignore` 차단** |
| `default_robots.json` ✅ | 로봇 목록 (`tb3_1` 티원 / `tb3_2` 젠지, hostAddress + cameraTopic + poseTopic 포함) |
| `default_features.json` | 기능 목록 + UI 표시 규칙 |
| `default_sensors.json` | 센서 목록 + 토픽/단위/임계값 |
| `office_base_map.json` | 맵/웨이포인트/차단구역/보호대상 위치 |

## 보안 규칙

- `SupabaseConfig.local.asset`은 `.gitignore`에 박혀 있음. 커밋 절대 금지.
- service_role 키 절대 박지 말 것. anon key만.
- 비밀 키 회전 시 `.local.asset`만 갱신, 템플릿은 빈 값 유지.

## 로드 패턴

```csharp
var cfg = Resources.Load<SupabaseConfig>("SupabaseConfig.local");
if (cfg == null) cfg = Resources.Load<SupabaseConfig>("SupabaseConfig.template");
```

## 주의

- `Resources/` 폴더의 자산은 빌드에 무조건 포함됨 (Tree shaking 불가). 큰 파일은 `Addressables` 또는 `StreamingAssets`로.
