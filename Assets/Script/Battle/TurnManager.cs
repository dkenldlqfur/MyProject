using System;
using System.Collections.Generic;
using System.Linq;
using Game.Core;
using UnityEngine;

namespace Game.Battle
{
    /// <summary>
    /// 전투의 턴 순서를 관리하고 진행하는 매니저입니다.
    /// </summary>
    [Serializable]
    public class TurnManager
    {
        public Queue<Character> turnQueue = new(); // 턴 순서 큐
        public Character currentTurnCharacter; // 현재 턴인 캐릭터

        readonly UnitManager unitManager;
        readonly Action<Character> onAutoTurnCallback;

        public TurnManager(UnitManager unitManager, Action<Character> onAutoTurnCallback)
        {
            this.unitManager = unitManager;
            this.onAutoTurnCallback = onAutoTurnCallback;
        }

        /// <summary>
        /// 모든 캐릭터를 속도(Speed) 기준으로 내림차순 정렬하여 턴 큐를 초기화합니다.
        /// </summary>
        public void InitializeTurnQueue()
        {
            var allChars = unitManager.GetAllCharacters();
            
            // 정렬 규칙: 1. 속도 내림차순, 2. 그리드 인덱스 오름차순
            var sortedChars = allChars
                .OrderByDescending(c => c.CurrentStats.speed)
                .ThenBy(c => c.gridIndex)
                .ToList();

            turnQueue.Clear();
            foreach (var character in sortedChars)
            {
                // 생존한 캐릭터만 큐에 추가
                if (0 < character.CurrentStats.hp)
                    turnQueue.Enqueue(character);
            }
        }

        /// <summary>
        /// 다음 캐릭터의 턴을 시작합니다.
        /// </summary>
        public void StartNextTurn()
        {
            // 생존자가 남아있는지 확인
            if (CheckBattleEndCondition())
                return;

            if (0 == turnQueue.Count)
                InitializeTurnQueue();

            if (0 < turnQueue.Count)
            {
                currentTurnCharacter = turnQueue.Dequeue();

                // 턴 시작 시 이미 죽어있다면 스킵하고 다음 턴
                if (0 >= currentTurnCharacter.CurrentStats.hp)
                {
                    StartNextTurn();
                    return;
                }

                // 턴 시작 시 상태 초기화
                currentTurnCharacter.IsInterruptRequested = false;

                ExecuteAutoTurn(currentTurnCharacter);
            }
            else
            {
                // 큐를 채울 수 없는 상황 (전멸 등) 처리
                Debug.LogWarning("[TurnManager] No characters available for turn.");
            }
        }

        /// <summary>
        /// 현재 턴을 종료하고 다음 턴으로 넘깁니다.
        /// </summary>
        public void EndTurn()
        {
            StartNextTurn();
        }

        /// <summary>
        /// AI 캐릭터의 자동 턴 실행 로직입니다.
        /// </summary>
        void ExecuteAutoTurn(Character character)
        {
            if (null == character)
                return;
            
            // 외부 콜백(AI 로직) 호출
            onAutoTurnCallback?.Invoke(character);
        }

        bool CheckBattleEndCondition()
        {
            // 간단한 종료 조건 체크: 양쪽 진영 중 하나라도 전멸하면 종료
            // (실제 구현에서는 BattleManager나 GameState가 관리하는 것이 좋음, 여기서는 null 체크 등 최소한만 수행)
            var allies = unitManager.GetCharacters(true);
            var enemies = unitManager.GetCharacters(false);

            // 1. 전멸 체크
            if (allies.All(c => 0 >= c.CurrentStats.hp))
                return true;
            if (enemies.All(c => 0 >= c.CurrentStats.hp))
                return true;

            // 2. 자원 고갈 체크 (모든 생존 캐릭터가 스킬 사용 불가 상태인지)
            // (주의: 일반 공격도 스킬로 취급한다면, 일반 공격조차 못하는 상황을 의미)
            var allSurvival = new List<Character>();
            allSurvival.AddRange(allies.Where(c => 0 < c.CurrentStats.hp));
            allSurvival.AddRange(enemies.Where(c => 0 < c.CurrentStats.hp));

            // 생존자가 하나라도 있는데, 행동 가능한 캐릭터가 단 하나도 없다면 전투 종료
            if (0 < allSurvival.Count && allSurvival.All(c => !c.HasEnoughResourceForAnyActiveSkill()))
                return true;

            return false;
        }
    }
}
