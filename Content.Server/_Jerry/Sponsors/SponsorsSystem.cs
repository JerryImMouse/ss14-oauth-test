using System.Threading.Tasks;
using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server._Jerry.Sponsors;

public sealed class SponsorsSystem : EntitySystem
{
    [Dependency] private readonly IConsoleHost _host = default!;
    [Dependency] private readonly SponsorsManager _sponsors = default!;

    public override void Initialize()
    {
        base.Initialize();

        _host.RegisterCommand("set_given", SetGiven);
        _host.RegisterCommand("is_given", IsGiven);
        _host.RegisterCommand("make_wipe", MakeWipe);
    }

    [AdminCommand(AdminFlags.Debug)]
    public void SetGiven(IConsoleShell shell, string args, string[] argv)
    {
        if (shell.Player is null)
            return;
        var userId = shell.Player.UserId;

        if (!_sponsors.TryGetSponsorData(userId, out var data))
            return;

        data.IsGiven = true;

        Task.Run(() => _sponsors.SetGiven(userId, true));
    }

    [AdminCommand(AdminFlags.Debug)]
    public void IsGiven(IConsoleShell shell, string args, string[] argv)
    {
        if (shell.Player is null)
            return;
        var userId = shell.Player.UserId;
        if (!_sponsors.TryGetSponsorData(userId, out var data))
            return;
        shell.WriteLine($"Your given status is: {(data.IsGiven ? "GIVEN" : "NONGIVEN")}");
    }
    [AdminCommand(AdminFlags.Host)]
    public void MakeWipe(IConsoleShell shell, string args, string[] argv)
    {
        Task.Run(() => _sponsors.MakeWipe());
    }
}
