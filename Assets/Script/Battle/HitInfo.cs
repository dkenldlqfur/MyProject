using Game.Core;

namespace Game.Battle
{
    /// <summary>
    /// 공격 결과 정보를 저장하는 구조체입니다.
    /// Post-Attack 반응 스킬 처리에 사용됩니다.
    /// </summary>
    public struct HitInfo
    {
        public Character target;      // 피격자
        public Character attacker;    // 공격자
        public HitResult hitResult;   // 적중 결과

        public HitInfo(Character target, Character attacker, HitResult hitResult)
        {
            this.target = target;
            this.attacker = attacker;
            this.hitResult = hitResult;
        }
    }
}
