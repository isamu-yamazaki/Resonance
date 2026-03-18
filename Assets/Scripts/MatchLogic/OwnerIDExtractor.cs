using System.Collections.Generic;
using PurrNet;
using PurrNet.Packing;
using Resonance.Assemblies.MatchStat;
using UnityEngine;

namespace Resonance.Match
{

    public class OwnerIDExtractor
    {

        public static PlayerID UlongToPlayerId(ulong id)
        {
            return new PlayerID(new PackedULong(id), false);
        }

        public static PlayerID? UlongNullableToPlayerIdNullable(ulong? id)
        {
            return id.HasValue ? UlongToPlayerId(id.Value) : null;
        }

        public static Dictionary<PlayerID, PlayerMatchStats> UlongDictionaryToPlayerIDDictionary(
            Dictionary<ulong, PlayerMatchStats> allStats)
        {
            var result = new Dictionary<PlayerID, PlayerMatchStats>();
            foreach (var (id, stats) in allStats)
            {
                result.Add(UlongToPlayerId(id), stats);
            }
            return result;
        }

        public static List<ulong> PlayerIdListToUlongList(List<PlayerID> players)
        {
            var result = new List<ulong>(players.Count);
            foreach (var p in players)
            {
                result.Add(p.id.value);
            }
            return result;
        }

        public static bool TryExtractPlayerIds(GameObject gameObject, out ulong playerId)
        {
            playerId = 0;
            
            if (!gameObject.TryGetComponent(out NetworkBehaviour controller))
                return false;

            if (controller.owner?.id.value is ulong idPrimitive)
            {
                playerId = idPrimitive;
                return true;
            }

            return false;
        }

        public static bool TryExtractPlayerIds(GameObject first, GameObject second, out ulong firstId, out ulong secondId)
        {
            firstId = 0;
            secondId = 0;

            if (!first.TryGetComponent(out NetworkBehaviour firstController) ||
                !second.TryGetComponent(out NetworkBehaviour secondController))
                return false;

            if (firstController.owner?.id.value is ulong firstIdPrimitive &&
                secondController.owner?.id.value is ulong secondIdPrimitive)
            {
                firstId = firstIdPrimitive;
                secondId = secondIdPrimitive;
                return true;
            }

            return false;
        }
    }
}
