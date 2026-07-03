using System.Collections.Generic;

// ####################################################################
// Debug console command to set the charge level of the battery
// currently held in the players hand (toolbelt selection).
// Usage:
//   db       - fully drain the held battery
//   db 50    - set held battery to 50% charge
// Note: In this mod `UseTimes` is repurposed as the charge level,
// where 0 means fully charged and `MaxUseTimes` means fully empty.
// ####################################################################

public class ConsoleCmdDrainBattery : ConsoleCmdAbstract
{

    // ####################################################################
    // ####################################################################

    public override string[] getCommands()
    {
        return new string[] { "db" };
    }

    public override string getDescription()
    {
        return "Set charge of held battery (default: drain fully)";
    }

    public override string getHelp()
    {
        return "Sets the charge level of the battery item currently held\n" +
               "in the players hand. Without arguments the battery is\n" +
               "drained completely. Optionally pass a charge percentage.\n" +
               "Usage:\n" +
               "  db      => 0% charge (empty)\n" +
               "  db 75   => 75% charge\n";
    }

    // ####################################################################
    // ####################################################################

    public override void Execute(List<string> _params, CommandSenderInfo _senderInfo)
    {
        World world = GameManager.Instance.World;
        if (world == null)
        {
            SdtdConsole.Instance.Output("World not loaded");
            return;
        }

        EntityPlayerLocal player = world.GetPrimaryPlayer();
        if (player == null)
        {
            SdtdConsole.Instance.Output("No local player found (command is client only)");
            return;
        }

        ItemValue held = player.inventory.holdingItemItemValue;
        if (held == null || held.IsEmpty())
        {
            SdtdConsole.Instance.Output("Not holding any item");
            return;
        }

        if (held.MaxUseTimes <= 0)
        {
            SdtdConsole.Instance.Output(string.Format(
                "Held item '{0}' has no use times (not a battery?)",
                held.ItemClass?.GetItemName() ?? "unknown"));
            return;
        }

        // Default: fully drained (0% charge)
        float percent = 0f;
        if (_params.Count > 0)
        {
            if (!float.TryParse(_params[0], out percent)
                || percent < 0f || percent > 100f)
            {
                SdtdConsole.Instance.Output(
                    "Invalid charge percentage (expected 0-100)");
                return;
            }
        }

        // UseTimes = 0 is full, UseTimes = MaxUseTimes is empty
        held.UseTimes = held.MaxUseTimes * (1f - percent / 100f);

        // Force the toolbelt/UI to notice the change
        player.inventory.notifyListeners();

        SdtdConsole.Instance.Output(string.Format(
            "Set '{0}' to {1}% charge (UseTimes {2}/{3})",
            held.ItemClass?.GetItemName() ?? "unknown",
            percent, held.UseTimes, held.MaxUseTimes));
    }

    // ####################################################################
    // ####################################################################

}
