using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;

namespace ActsFromThePast;

public class DynamicMultiAttackIntent : AttackIntent
{
    private readonly Func<int>? _repeatFunc;

    public DynamicMultiAttackIntent(Func<int> damageFunc, int repeat)
    {
        var damageFunc1 = damageFunc;
        Repeats = repeat;
        DamageCalc = () => damageFunc1();
    }

    public DynamicMultiAttackIntent(Func<int> damageFunc, Func<int> repeatFunc)
    {
        var damageFunc1 = damageFunc;
        _repeatFunc = repeatFunc;
        DamageCalc = () => damageFunc1();
    }

    protected override LocString IntentLabelFormat => new("intents", "FORMAT_DAMAGE_MULTI");
    public override int Repeats => _repeatFunc?.Invoke() ?? field;

    public override int GetTotalDamage(IEnumerable<Creature> targets, Creature owner)
    {
        return GetSingleDamage(targets, owner) * Repeats;
    }

    public override LocString GetIntentLabel(IEnumerable<Creature> targets, Creature owner)
    {
        var intentLabelFormat = IntentLabelFormat;
        var singleDamage = (float)GetSingleDamage(targets, owner);
        intentLabelFormat.Add("Damage", (int)singleDamage);
        intentLabelFormat.Add("Repeat", Repeats);
        return intentLabelFormat;
    }
}