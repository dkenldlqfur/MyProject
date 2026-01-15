using System;
using System.Collections.Generic;
using Game.Core;

namespace Game.Battle
{
    /// <summary>
    /// 전투에 참여하는 유닛(캐릭터)들을 관리하는 매니저입니다.
    /// </summary>
    [Serializable]
    public class UnitManager
    {
        public const int MaxUnitsPerSide = 5; // 진영당 최대 인원

        public List<Character> allyCharacters = new(); // 런타임에 관리되는 아군 캐릭터 리스트
        public List<Character> enemyCharacters = new(); // 런타임에 관리되는 적군 캐릭터 리스트

        readonly List<Character> allCharacters = new(); // 전체 캐릭터 리스트 (캐시용)
        bool isDirty = true;

        /// <summary>
        /// 캐릭터를 전투 시스템에 등록합니다.
        /// </summary>
        public void RegisterCharacter(Character character, bool isAlly)
        {
            var characterList = isAlly ? allyCharacters : enemyCharacters;

            if (characterList.Count >= MaxUnitsPerSide)
                return;

            if (!characterList.Contains(character))
            {
                characterList.Add(character);
                character.isPlayerFaction = isAlly; // 캐릭터에게 진영 정보 주입
                isDirty = true;
            }
        }

        /// <summary>
        /// 전투에 참여 중인 모든 캐릭터를 반환합니다.
        /// </summary>
        public List<Character> GetAllCharacters()
        {
            if (isDirty)
            {
                allCharacters.Clear();
                allCharacters.AddRange(allyCharacters);
                allCharacters.AddRange(enemyCharacters);
                isDirty = false;
            }

            return allCharacters;
        }

        /// <summary>
        /// 특정 진영의 캐릭터 리스트를 반환합니다.
        /// </summary>
        public List<Character> GetCharacters(bool isAlly)
        {
            return isAlly ? allyCharacters : enemyCharacters;
        }
    }
}
