#load "DanNet.csx"

var toonName = Args[0];

MQ2.WriteChat($"Watching {toonName}");

var dannet = new DanNet(MQ2, TLO);

// Can query with an expression:
var plat = await dannet.Query(toonName, tlo => tlo.Me.Platinum, 1000);
// Or just as a plain old string:
var gold = await dannet.Query(toonName, "Me.Gold", 1000);

MQ2.WriteChat($"They have {plat}pp and {gold}gp");

// Observers are the same:
var diseases = dannet.Observe(toonName, tlo => tlo.Me.CountersDisease);
var poisons = dannet.Observe(toonName, "Me.CountersPoison");

// To drop the observer, dispose of it:
diseases.Dispose();
poisons.Dispose();

// C# can automatically dispose for you with a using block:
using (var curses = dannet.Observe(toonName, tlo => tlo.Me.CountersCurse))
{
}
// Variable is no longer in scope, and the observer has been dropped

// Observers take time to become available:
var sw = Stopwatch.StartNew();
using (var curses = dannet.Observe(toonName, tlo => tlo.Me.CountersCurse)) // Note reuse of the variable name - allowed since it was limited to the using scope above
{
    while (!curses.Available)
        await Task.Yield();
    MQ2.WriteChat($"Observer took {sw.ElapsedMilliseconds}ms to be ready");
}

// You can use the helper method to wait for it to be ready:
using (var loc = dannet.Observe(toonName, tlo => tlo.Me.Spawn.LocYXZ))
{
    if (!(await loc.WaitUntilAvailable(1000, Token)))
    {
        MQ2.WriteChat("Timeout waiting for query to become available");
        return;
    }

    for (var i = 0; i < 10; i++)
    {
        // The value of an observer is just .Value
        MQ2.WriteChat($"{toonName} is at {loc.Value}");
        await Task.Delay(1000);
    }
}

// There's nothing atm to stop you doing this other than it being a bad idea
var observer1 = dannet.Observe(toonName, "Stuff");
using (var observer2 = dannet.Observe(toonName, "Stuff"))
{
}
// observer1 still exists but is now invalid because observer2 dropped it :(