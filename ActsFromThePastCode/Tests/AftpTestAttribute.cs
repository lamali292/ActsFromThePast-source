namespace ActsFromThePast.Tests;

/// <summary>
///     A single-combat test: <c>Task Name(TestContext ctx)</c>. The runner starts a fresh combat against
///     <paramref name="encounterType" /> (the first registered encounter when null) and tears it down afterwards.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class AftpTestAttribute(Type? encounterType = null) : Attribute
{
    public Type? EncounterType { get; } = encounterType;
}

/// <summary>A generator: <c>IEnumerable&lt;AftpTestCase&gt; Name()</c>. Every case gets its own fresh combat.</summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class AftpPoolTestAttribute : Attribute;

public sealed record AftpTestCase(string Name, Type? EncounterType, Func<TestContext, Task> Run);
