using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using Resonance.Assemblies.MatchStat;

public class AssistCalculatorTests
{
    private ulong expectedVictimId = 2;
    private AssistCalculator calculator;
    private HashSet<ulong> expectedAssistIds = new()
        {
            3,
            4
        };

    [SetUp]
    public void Setup()
    {
        calculator = new AssistCalculator(
            assistTimeWindowMs: 100f,
            assistDamageThreshold: 20
        );
    }

    [Test]
    public void GetAssistAttackersForVictim_RetrievesCorrectAssistersAfterMultipleRecordCalls()
    {

        foreach (var id in expectedAssistIds)
        {
            // test out total damage before death
            calculator.RecordDamage(id, expectedVictimId, 15);
            calculator.RecordDamage(id, expectedVictimId, 15);
        }

        var actualAssistIds = calculator.GetAssistAttackersForVictim(expectedVictimId, 1);
        Assert.AreEqual(expectedAssistIds, actualAssistIds);
    }

    [Test]
    public void GetAssistAttackersForVictim_RetrievesCorrectAssistersAfterOneRecordCall()
    {
        foreach (var id in expectedAssistIds)
        {
            calculator.RecordDamage(id, expectedVictimId, 30);
        }
        var actualAssistIds = calculator.GetAssistAttackersForVictim(expectedVictimId, 1);
        Assert.AreEqual(expectedAssistIds, actualAssistIds);
    }

    [Test]
    public async Task GetAssistAttackersForVictim_RetrievesEmptyIfTimeElapsed()
    {
        foreach (var id in expectedAssistIds)
        {
            calculator.RecordDamage(id, expectedVictimId, 15);
            calculator.RecordDamage(id, expectedVictimId, 15);
        }

        await Task.Delay(1000);
        var actualAssistIds = calculator.GetAssistAttackersForVictim(expectedVictimId, 1);
        Assert.IsEmpty(actualAssistIds);
    }
}
