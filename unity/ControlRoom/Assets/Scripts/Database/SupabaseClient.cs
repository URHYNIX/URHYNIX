// SupabaseClient.cs — Supabase REST/Edge Function 클라이언트 stub.
// Phase 7(Supabase 통합) 진입 시 supabase-csharp + UniTask + anon key로 채움.
// 현재는 컴파일 통과용 골격. service_role 키 절대 미반입 정책 유지.
namespace URHYNIX.ControlRoom.Database
{
    public class SupabaseClient
    {
        public string Url { get; }
        public string AnonKey { get; }

        public SupabaseClient(string url, string anonKey)
        {
            Url = url;
            AnonKey = anonKey;
        }
    }
}
