using UnityEngine;

namespace ConductorSymphony.Utility
{
    // 2026-08-10: 갈무리(Galmuri) 도트 폰트 통일 작업. 기존엔 모든 UI 텍스트가
    // Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")(유니티 기본 폰트, Arial 계열)를
    // 그대로 썼다 - 카드 설명 같은 "읽는" 텍스트와 LEVEL UP/PERFECT/승리·패배 같은 "강조하는"
    // 텍스트를 구분해 2단 체계로 통일한다.
    //   - Body(Galmuri9): 카드 설명, HUD 숫자, 버튼 라벨 등 본문 텍스트.
    //   - Headline(Galmuri11-Bold): 카드 제목, 판정 팝업(PERFECT!/GREAT!/MISS), 보스 HP 경고,
    //     승리/패배 문구 등 임팩트가 필요한 텍스트.
    // Resources.Load 결과를 정적 필드에 캐싱해서, UI 텍스트를 새로 만들 때마다(레벨업 카드 등은
    // 매 판마다 재사용) 디스크에서 반복 로드하지 않도록 한다.
    public static class GameFonts
    {
        private static Font body;
        private static Font headline;

        public static Font Body
        {
            get
            {
                if (body == null) body = Resources.Load<Font>("Fonts/Galmuri9");
                return body;
            }
        }

        public static Font Headline
        {
            get
            {
                if (headline == null) headline = Resources.Load<Font>("Fonts/Galmuri11-Bold");
                return headline;
            }
        }
    }
}
