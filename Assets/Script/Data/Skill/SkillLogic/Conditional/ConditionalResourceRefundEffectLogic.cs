using Game.Core;
using Game.Data;

namespace Game.Battle
{
    [SkillLogic(SkillLogicType.ConditionalResourceRefund)]
    public class ConditionalResourceRefundEffectLogic : ConditionalEffectLogic<ConditionalResourceRefundEffectData>
    {
        protected override void OnExecute(ConditionalResourceRefundEffectData data, SkillContext context)
        {
            // 소모했던 자원 일부 환급
            if (0 < context.HpCost)
                context.caster.RestoreResource(ResourceType.HP, (int)(context.HpCost * data.refundPercent * 0.01f), false);
            if (0 < context.SpCost)
                context.caster.RestoreResource(ResourceType.SP, (int)(context.SpCost * data.refundPercent * 0.01f), false);
            if (0 < context.MpCost)
                context.caster.RestoreResource(ResourceType.MP, (int)(context.MpCost * data.refundPercent * 0.01f), false);
        }
    }
}