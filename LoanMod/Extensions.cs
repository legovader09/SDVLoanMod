using StardewValley;

namespace LoanMod
{
    public partial class ModEntry
    {
        private static void AddMessage(string message, int messageType)
        {
            Game1.addHUDMessage(new HUDMessage(message, messageType));
        }
    }
}
