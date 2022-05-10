using StardewModdingAPI;
using StardewValley;
using System;
using System.Collections.Generic;

namespace LoanMod
{
    public partial class ModEntry
    {

        private void AddMessage(string message, int messageType)
        {
            Game1.addHUDMessage(new HUDMessage(message, messageType));
        }
    }
}
