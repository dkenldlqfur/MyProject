using UnityEngine;
using Game.Core;
using Game.Data;
using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;

namespace Game.Battle
{
    /// <summary>
    /// 전투의 전반적인 흐름과 로직을 관리하는 파사드(Facade) 클래스입니다.
    /// 실제 로직은 각 하위 매니저(UnitManager, TurnManager, CombatManager)로 위임합니다.
    /// </summary>
    public class BattleManager : MonoBehaviour
    {
        public static BattleManager Instance { get; private set; }

        struct HitInfo
        {
            public Character target;
            public Character attacker;
            public HitResult hitResult;

            public HitInfo(Character target, Character attacker, HitResult hitResult)
            {
                this.target = target;
                this.attacker = attacker;
                this.hitResult = hitResult;
            }
        }

        [Header("Transform References")]
        public Transform allyGridRoot;  // 아군 배치 루트
        public Transform enemyGridRoot; // 적군 배치 루트

        // Sub-Managers
        public UnitManager unitManager;
        public TurnManager turnManager;
        public CombatManager combatManager;

        // Legacy Support Properties
        public List<Character> AllyCharacters => unitManager.allyCharacters;
        public List<Character> EnemyCharacters => unitManager.enemyCharacters;
        public Character CurrentTurnCharacter => turnManager.currentTurnCharacter;
        public Queue<Character> TurnQueue => turnManager.turnQueue;

        void Awake()
        {
            if (null != Instance && this != Instance)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            InitializeSubManagers();
        }

        void InitializeSubManagers()
        {
            // 하위 매니저 초기화
            unitManager = new UnitManager();
            
            // TurnManager 연결: AI 턴 실행 로직 주입
            turnManager = new TurnManager(unitManager, ExecuteAutoTurn);

            // CombatManager 연결: 스킬 실행 로직 주입
            combatManager = new CombatManager(unitManager, ExecuteSkill);
        }

        public void StartBattle()
        {
            turnManager.StartNextTurn();
        }

        /// <summary>
        /// 캐릭터를 전투 시스템에 등록합니다.
        /// </summary>
        public void RegisterCharacter(Character character, bool isAlly)
        {
            unitManager.RegisterCharacter(character, isAlly);
            
            // 이벤트 구독 (Decoupling)
            character.OnAttackRequested += ProcessAttack;
            character.OnRestoreRequested += ProcessRestoreResource;
            character.OnCrowdControlRequested += ProcessCrowdControlState;
        }

        public List<Character> GetAllCharacters()
        {
            return unitManager.GetAllCharacters();
        }

        /// <summary>
        /// 턴 관련 메서드들 (Facade Pattern)
        /// </summary>
        public void StartNextTurn()
        {
            turnManager.StartNextTurn();
        }

        public void EndTurn()
        {
            turnManager.EndTurn();
        }

        /// <summary>
        /// 외부(UI 등)에서 스킬 사용을 요청할 때 사용합니다.
        /// </summary>
        public void RequestUseSkill(Character caster, SkillData skill, List<Character> targets)
        {
            if (turnManager.currentTurnCharacter != caster)
                return;

            ExecuteSkill(caster, skill, targets);
            turnManager.EndTurn();
        }

        /// <summary>
        /// AI 캐릭터의 자동 턴 실행 로직입니다.
        /// </summary>
        void ExecuteAutoTurn(Character character)
        {
            if (null == character)
                return;

            // 스킬 선택 로직 (임시: 랜덤 선택)
            SkillData selectedSkill = null;
            foreach (var skill in character.equippedSkills)
            {
                if (null != skill && SkillType.Active == skill.skillType)
                {
                    selectedSkill = skill;
                    break;
                }
            }

            if (null != selectedSkill)
            {
                // 타겟 선택 로직 (임시: 랜덤 선택)
                var targets = !character.isPlayerFaction ? unitManager.GetCharacters(true) : unitManager.GetCharacters(false);
                if (0 < targets.Count)
                {
                    var targetList = new List<Character>{ targets[UnityEngine.Random.Range(0, targets.Count)] };
                    ExecuteSkill(character, selectedSkill, targetList);
                }
            }

            turnManager.EndTurn();
        }

        /// <summary>
        /// 스킬을 실제로 실행하는 중앙 관리 메서드입니다.
        /// </summary>
        /// <param name="incomingHitResult">반응 스킬용 피격 결과 (일반 스킬은 기본값 사용)</param>
        public void ExecuteSkill(Character caster, SkillData skillData, List<Character> targets, HitResult incomingHitResult = HitResult.None, AttackType incomingAttackType = AttackType.None, RangeType incomingRangeType = RangeType.None)
        {
            ExecuteSkillAsync(caster, skillData, targets, incomingHitResult, incomingAttackType, incomingRangeType).Forget();
        }

        private async UniTaskVoid ExecuteSkillAsync(Character caster, SkillData skillData, List<Character> targets, HitResult incomingHitResult, AttackType incomingAttackType, RangeType incomingRangeType)
        {
            var hitInfos = new List<HitInfo>();
            
            // 0. 애니메이터 오버라이드 설정 (존재하는 경우)
            if (SkillType.Active == skillData.SkillType)
            {
                foreach (var effect in skillData.effects)
                {
                    if (effect.animatorOverride != null)
                    {
                        caster.SetAnimatorOverride(effect.animatorOverride);
                        break;
                    }
                }
            }

            // 1. 타겟 이동 및 공격 (액티브 스킬 전용)
            if (SkillType.Active == skillData.SkillType)
            {
                // A. 이동
                caster.PlayAnimation("MoveToTarget");
                
                float moveDuration = caster.GetAnimationClipLength("Action_MoveToTarget");
                // 애니메이션 클립을 찾지 못한 경우 기본값 1.0초 사용
                if (moveDuration <= 0)
                    moveDuration = 1.0f; 
                await UniTask.Delay((int)(moveDuration * 1000));

                // B. 공격 (텍스트 표시)
                caster.PlayAnimation("Attack");
                
                float attackDuration = caster.GetAnimationClipLength("Action_Attack");
                if (attackDuration <= 0)
                    attackDuration = 0.5f;
                await UniTask.Delay((int)(attackDuration * 1000));

                // C. 반응형 스킬 처리 (공격 전)
                if (0 < targets.Count)
                    combatManager.ProcessReactionSkills(targets[0], caster, ReactionTiming.PreAttack);
            }

            foreach (var target in targets)
            {
                var context = new SkillContext(caster, target);
                
                // 공격 타입 초기화
                if (incomingAttackType != AttackType.None)
                    context.LastAttackType = incomingAttackType;
                
                // 사거리 타입 초기화
                RangeType currentRangeType = incomingRangeType;
                if (currentRangeType == RangeType.None)
                    currentRangeType = skillData.rangeType;
                context.LastRangeType = currentRangeType;

                // 피격 결과 초기화 (반응형 스킬용)
                if (HitResult.None != incomingHitResult)
                    context.LastHitResult = incomingHitResult;

                foreach (var effect in skillData.effects)
                    {
                        if (null == effect)
                            continue;
 
                    if (SkillEffectType.Conditional == effect.EffectType && !context.IsConditionCheck)
                        continue;

                    // 애니메이션 재생
                    if (!string.IsNullOrEmpty(effect.animationTriggerName))
                        caster.PlayAnimation(effect.animationTriggerName);

                    // 애니메이션 이벤트 대기
                    if (!string.IsNullOrEmpty(effect.triggerKey))
                        await WaitForAnimationKey(caster, effect.triggerKey);

                    SkillProcessor.Process(effect, context);

                    if (caster.IsInterruptRequested || context.AbortProcessing)
                    {
                        context.AbortProcessing = true; 
                        break;
                    }
                }

                // 피격 결과 수집
                if (HitResult.None != context.LastHitResult)
                    hitInfos.Add(new HitInfo(target, caster, context.LastHitResult));
                    
                // 회피 애니메이션 재생
                if (context.LastHitResult == HitResult.Miss)
                    target.PlayDodgeAnimation();
            }

            // 3. 속도 기준 정렬 (내림차순)
            hitInfos.Sort((a, b) => b.target.CurrentStats.speed.CompareTo(a.target.CurrentStats.speed));

            // 4. 후처리 트리거 (액티브 스킬 전용)
            if (SkillType.Active == skillData.SkillType)
            {
                // 피격 후 처리 (타겟)
                foreach (var info in hitInfos)
                {
                    combatManager.ProcessReactionSkills(info.target, info.attacker, ReactionTiming.PostHit, info.hitResult);
                }

                // 공격 후 처리 (시전)
                combatManager.ProcessReactionSkills(caster, null, ReactionTiming.PostAttack);
            }

            // 5. 사망 처리
            foreach (var info in hitInfos)
            {
                if (0 >= info.target.CurrentStats.hp)
                    ProcessDeath(info.target);
            }

            // 6. 복귀 애니메이션 & 상태 복원
            if (SkillType.Active == skillData.SkillType)
            {
                caster.PlayAnimation("ReturnToStart");
                
                float returnDuration = caster.GetAnimationClipLength("Action_ReturnToStart");
                if (returnDuration <= 0)
                    returnDuration = 1.0f;
                await UniTask.Delay((int)(returnDuration * 1000));
                
                caster.RestoreAnimatorController();
            }
        }

        /// <summary>
        /// 캐릭터 사망 처리를 수행합니다.
        /// </summary>
        void ProcessDeath(Character character)
        {
            // TODO: Implement death event, animation, and unit removal
            Debug.Log($"{character.name} has died.");
        }

        /// <summary>
        /// 공격 처리 및 리소스 관리 메서드 (Facade -> CombatManager)
        /// </summary>
        public HitResult ProcessAttack(Character attacker, Character target, int physDamage, int magicDamage, SkillEffectData sourceEffect, AttackType attackType, RangeType rangeType)
        {
            return combatManager.ProcessAttack(attacker, target, physDamage, magicDamage, sourceEffect, attackType, rangeType);
        }

        public void ProcessRestoreResource(Character target, ResourceType resourceType, int value, bool isOverHeal)
        {
            combatManager.ProcessRestoreResource(target, resourceType, value, isOverHeal);
        }

        public void ProcessCrowdControlState(Character target, Character source, CrowdControlType ccType)
        {
            combatManager.ProcessCrowdControlState(target, source, ccType);
        }

        private async UniTask WaitForAnimationKey(Character character, string targetKey, float timeout = 10.0f)
        {
            if (string.IsNullOrEmpty(targetKey))
                return;

            bool triggered = false;
            Action<string> handler = (key) =>
            {
                if (key == targetKey)
                    triggered = true;
            };

            character.OnAnimationTriggered += handler;

            try
            {
                await UniTask.WaitUntil(() => triggered).Timeout(TimeSpan.FromSeconds(timeout));
            }
            catch (TimeoutException)
            {
                Debug.LogWarning($"Animation Key '{targetKey}' timed out for {character.name}.");
            }
            finally
            {
                character.OnAnimationTriggered -= handler;
            }
        }
    }
}