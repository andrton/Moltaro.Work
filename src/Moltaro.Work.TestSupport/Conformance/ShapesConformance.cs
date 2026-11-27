namespace Moltaro.Work.TestSupport.Conformance;

// The shapes: payloads, the keys of logs and runs while their items are active, and what remains of an item after
// success. No rule of concept section 3 is held here; S10 adds the cases of its brief.
public abstract class ShapesConformance
{
    private readonly ConformanceDatabase _database;

    protected ShapesConformance(ConformanceDatabase database)
    {
        _database = database;
    }
}
