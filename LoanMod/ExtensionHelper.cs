using StardewValley;

namespace LoanMod
{
    public class ExtensionHelper
    {
        public static void AddMessage(string message, int messageType)
        {
            Game1.addHUDMessage(new HUDMessage(message, messageType));
        }
    }
}
