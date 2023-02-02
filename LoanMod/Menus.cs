using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using System.Collections.Generic;

namespace LoanMod
{
    public partial class ModEntry
    {
        private List<Response> menuItems, repayMenuItems, durationMenu, menuYesNo;
        private void InitMenus()
        {
            menuItems = new List<Response>
            {
                new("money_500", $"{Config.MoneyAmount1}g"),
                new("money_1k", $"{Config.MoneyAmount2}g"),
                new("money_5k", $"{Config.MoneyAmount3}g"),
                new("money_10k", $"{Config.MoneyAmount4}g"),
                new("money_Cancel", I18n.Menu_Cancel())
            };

            durationMenu = new List<Response>
            {
                new("time_3D", $"{Config.DayLength1} {I18n.Menu_Days()} @ {Config.InterestModifier1 * 100}%"),
                new("time_7D", $"{Config.DayLength2} {I18n.Menu_Days()} @ {Config.InterestModifier2 * 100}%"),
                new("time_14D", $"{Config.DayLength3} {I18n.Menu_Days()} @ {Config.InterestModifier3 * 100}%"),
                new("time_28D", $"{Config.DayLength4} {I18n.Menu_Days()} @ {Config.InterestModifier4 * 100}%"),
                new("time_Cancel", I18n.Menu_Cancel())
            };

            repayMenuItems = new List<Response>
            {
                new("repay_show_Balance", I18n.Menu_Showbalance()),
                new("repay_Custom", I18n.Menu_Repaycustom()),
                new("repay_Full", I18n.Menu_Repayfull()),
                new("repay_Leave", I18n.Menu_Leave())
            };

            menuYesNo = new List<Response>
            {
                new("menu_Yes", I18n.Menu_Yes()),
                new("menu_No", I18n.Menu_No()),
                new("menu_Leave", I18n.Menu_Leave())
            };
        }

        private void StartBorrow(int stage, string key)
        {
            var Gamer = Game1.currentLocation;
            if (!loanManager.IsBorrowing)
            {
                switch (stage)
                {
                    case 1:
                        if (Config.CustomMoneyInput)
                            Game1.activeClickableMenu = new NumberSelectionMenu(I18n.Msg_Startborrow1(), (val, cost, farmer) => ProcessBorrowing(val, key), -1, 100, Config.MaxBorrowAmount, 500);
                        else
                            Gamer.createQuestionDialogue(I18n.Msg_Startborrow1(), menuItems.ToArray(), BorrowMenu);
                        break;
                    case 2:
                        Gamer.createQuestionDialogue(I18n.Msg_Startborrow2(), durationMenu.ToArray(), BorrowDuration);
                        break;
                }
            }
            else
            {
                switch (stage)
                {
                    case 1:
                        Gamer.createQuestionDialogue(I18n.Msg_Menu1(), repayMenuItems.ToArray(), RepayMenu);
                        break;
                    case 3:
                        Gamer.createQuestionDialogue(I18n.Msg_Menu2(loanManager.Balance.ToString("N0")), menuYesNo.ToArray(), RepayFullMenu);
                        break;
                }
            }
        }

        private void ProcessBorrowing(int val, string key)
        {
            switch (key)
            {
                case "Key_Amount":
                    amount = val;
                    borrowProcess = true;
                    Monitor.Log($"Selected {amount}g", LogLevel.Info);
                    Game1.activeClickableMenu = null;
                    StartBorrow(2, "Key_Duration");
                    break;
            }
        }

        private void BorrowMenu(Farmer who, string menu)
        {
            switch (menu)
            {
                case "money_500":
                    amount = Config.MoneyAmount1;
                    borrowProcess = true;
                    Monitor.Log("Selected 500g.", LogLevel.Info);
                    break;
                case "money_1k":
                    amount = Config.MoneyAmount2;
                    borrowProcess = true;
                    Monitor.Log("Selected 1,000g.", LogLevel.Info);
                    break;
                case "money_5k":
                    amount = Config.MoneyAmount3;
                    borrowProcess = true;
                    Monitor.Log("Selected 5,000g.", LogLevel.Info);
                    break;
                case "money_10k":
                    amount = Config.MoneyAmount4;
                    borrowProcess = true;
                    Monitor.Log("Selected 10,000g.", LogLevel.Info);
                    break;
                case "money_Cancel":
                    borrowProcess = false;
                    Monitor.Log("Option Cancel");
                    break;
            }
        }

        private void BorrowDuration(Farmer who, string dur)
        {
            switch (dur)
            {
                case "time_3D":
                    duration = Config.DayLength1;
                    interest = Config.InterestModifier1;
                    Monitor.Log($"Selected {Config.DayLength1} days.");
                    break;
                case "time_7D":
                    duration = Config.DayLength2;
                    interest = Config.InterestModifier2;
                    Monitor.Log($"Selected {Config.DayLength2} days.");
                    break;
                case "time_14D":
                    duration = Config.DayLength3;
                    interest = Config.InterestModifier3;
                    Monitor.Log($"Selected {Config.DayLength3} days.");
                    break;
                case "time_28D":
                    duration = Config.DayLength4;
                    interest = Config.InterestModifier4;
                    Monitor.Log($"Selected {Config.DayLength4} days.");
                    break;
                case "time_Cancel":
                    borrowProcess = false;
                    Monitor.Log("Option Cancel");
                    break;
            }
        }
        private void RepayMenu(Farmer who, string option)
        {
            switch (option)
            {
                case "repay_show_Balance":
                    Monitor.Log("Option show balance", LogLevel.Debug);
                    AddMessage(I18n.Msg_Payment_Remaining(loanManager.Balance.ToString("N0"), loanManager.Duration, loanManager.CalculateAmountToPayToday.ToString("N0")), HUDMessage.newQuest_type);
                    break;
                case "repay_Custom":
                    Monitor.Log("Option repay custom", LogLevel.Debug);
                    InitiateRepayment(false, true);
                    break;
                case "repay_Full":
                    Monitor.Log("Option repay Full", LogLevel.Debug);
                    repayProcess = true;
                    break;
                case "repay_Leave":
                    Monitor.Log("Option Leave", LogLevel.Debug);
                    break;
            }
        }
        private void RepayFullMenu(Farmer who, string option)
        {
            switch (option)
            {
                case "menu_Yes":
                    Monitor.Log("Option Yes", LogLevel.Debug);
                    InitiateRepayment(true);
                    break;
                case "menu_No":
                    Monitor.Log("Option No", LogLevel.Debug);
                    repayProcess = false;
                    break;
                case "menu_Leave":
                    Monitor.Log("Option Leave", LogLevel.Debug);
                    repayProcess = false;
                    break;
            }
        }
    }
}
