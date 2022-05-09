using StardewModdingAPI;
using StardewValley;
using System;
using System.Collections.Generic;

namespace LoanMod
{
    public partial class ModEntry
    {
        private List<Response> menuItems, repayMenuItems, durationMenu, menuYesNo;
        private void Log(object message)
        { 
            this.Monitor.Log(Convert.ToString(message), LogLevel.Info); 
        }

        private void AddMessage(string message)
        {
            Game1.addHUDMessage(new HUDMessage(message, HUDMessage.achievement_type));
        }
    }
}
