// PoseLogRepository.cs — Supabase `pose_logs` 테이블 read + (선택) 제한 INSERT.
// Unity 관제는 read 우선. 주 쓰기 주체는 로봇 PC(scripts/pose_logger.py).
// service_role 키 절대 미반입. anon key + RLS 정책 경로만 사용.
// 실제 supabase-csharp 호출은 Phase 7(Supabase 통합) 진입 시 채움.
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using URHYNIX.ControlRoom.Data;

namespace URHYNIX.ControlRoom.Database
{
    public class PoseLogRepository
    {
        readonly SupabaseClient client;

        public PoseLogRepository(SupabaseClient client)
        {
            this.client = client;
        }

        // 세션별 시간순 pose 조회 (Unity 관제 read 경로).
        // SCHEMA index: idx_pose_logs_session_robot on (session_id, robot_id, ts).
        public Task<List<RobotPoseEntry>> QueryBySession(string sessionId, string robotId = null, int limit = 1000)
        {
            throw new NotImplementedException("Phase 7 Supabase 통합 시 구현. supabase-csharp .From<RobotPoseEntry>().Filter(\"session_id\", ...).Order(\"ts\").Limit(limit).Get()");
        }

        // 모드별 pose 조회 (시연 시 patrol/dispatch 구간 분석).
        public Task<List<RobotPoseEntry>> QueryByNavMode(string navMode, int limit = 1000)
        {
            throw new NotImplementedException("Phase 7. idx_pose_logs_mode 활용.");
        }

        // 보조 INSERT — 사람이 Unity에서 수동 마킹할 때만. 자동 좌표 기록은 로봇 PC가 담당.
        public Task Insert(RobotPoseEntry entry)
        {
            throw new NotImplementedException("Phase 7. anon RLS 정책에 따라 허용 시에만.");
        }
    }
}
