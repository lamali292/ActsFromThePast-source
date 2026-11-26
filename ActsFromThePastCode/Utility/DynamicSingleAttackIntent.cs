using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;

namespace ActsFromThePast;

public class DynamicSingleAttackIntent : AttackIntent
{
    public DynamicSingleAttackIntent(Func<int> damageFunc)
    {
        var damageFunc1 = damageFunc;
        DamageCalc = () => damageFunc1();
    }

    protected override LocString IntentLabelFormat => new("intents", "FORMAT_DAMAGE_SINGLE");
    public override int Repeats => 1;

    public override int GetTotalDamage(IEnumerable<Creature> targets, Creature owner)
    {
        return GetSingleDamage(targets, owner);
    }

    public override LocString GetIntentLabel(IEnumerable<Creature> targets, Creature owner)
    {
        var intentLabelFormat = IntentLabelFormat;
        var totalDamage = (float)GetTotalDamage(targets, owner);
        intentLabelFormat.Add("Damage", (int)totalDamage);
        return intentLabelFormat;
    }
}