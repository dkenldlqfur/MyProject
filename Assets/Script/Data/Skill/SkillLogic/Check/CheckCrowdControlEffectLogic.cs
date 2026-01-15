using Game.Core;
using Game.Data;

namespace Game.Battle
{
    [SkillLogic(SkillLogicType.CheckCrowdControl)]
    public class CheckCrowdControlEffectLogic : CheckEffectLogic<CheckCrowdControlEffectData>
    {
        protected override bool OnCheck(CheckCrowdControlEffectData data, SkillContext context)
        {
            return context.target.CurrentCrowdControlType.HasFlag(data.ccType);
        }
    }
}
