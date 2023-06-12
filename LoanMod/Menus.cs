using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using System.Collections.Generic;

namespace LoanMod
{
    public partial class ModEntry
    {
        private List<Response> _menuItems, _repayMenuItems, _durationMenu, _menuYesNo;
        private void InitMenus()
        {
            _menuItems = new List<Response>
            {
                new("money_500", $"{_config.MoneyAmount1}g"),
                new("money_1k", $"{_config.MoneyAmount2}g"),
                new("money_5k", $"{_config.MoneyAmount3}g"),
                new("money_10k", $"{_config.MoneyAmount4}g"),
                new("money_Cancel", I18n.Menu_Cancel())
            };

            _durationMenu = new List<Response>
            {
                new("time_3D", $"{_config.DayLength1} {I18n.Menu_Days()} @ {_config.InterestModifier1 * 100}%"),
                new("time_7D", $"{_config.DayLength2} {I18n.Menu_Days()} @ {_config.InterestModifier2 * 100}%"),
                new("time_14D", $"{_config.DayLength3} {I18n.Menu_Days()} @ {_config.InterestModifier3 * 100}%"),
                new("time_28D", $"{_config.DayLength4} {I18n.Menu_Days()} @ {_config.InterestModifier4 * 100}%"),
                new("time_Cancel", I18n.Menu_Cancel())
            };

            _repayMenuItems = new List<Response>
            {
                new("repay_show_Balance", I18n.Menu_Showbalance()),
                new("repay_Custom", I18n.Menu_Repaycustom()),
                new("repay_Full", I18n.Menu_Repayfull()),
                new("repay_Leave", I18n.Menu_Leave())
            };

            _menuYesNo = new List<Response>
            {
                new("menu_Yes", I18n.Menu_Yes()),
                new("menu_No", I18n.Menu_No()),
                new("menu_Leave", I18n.Menu_Leave())
            };
        }

        private void StartBorrow(int stage, string key)
        {
            var gamer = Game1.currentLocation;
            if (!_loanManager.IsBorrowing)
            {
                switch (stage)
                {
                    case 1:
                        if (_config.CustomMoneyInput)
                            Game1.activeClickableMenu = new NumberSelectionMenu(I18n.Msg_Startborrow1(), (val, cost, farmer) => ProcessBorrowing(val, key), -1, 100, _config.MaxBorrowAmount, 500);
                        else
                            gamer.createQuestionDialogue(I18n.Msg_Startborrow1(), _menuItems.ToArray(), BorrowMenu);
                        break;
                    case 2:
                        gamer.createQuestionDialogue(I18n.Msg_Startborrow2(), _durationMenu.ToArray(), BorrowDuration);
                        break;
                }
            }
            else
            {
                switch (stage)
                {
                    case 1:
                        gamer.createQuestionDialogue(I18n.Msg_Menu1(), _repayMenuItems.ToArray(), RepayMenu);
                        break;
                    case 3:
                        gamer.createQuestionDialogue(I18n.Msg_Menu2(_loanManager.Balance.ToString("N0")), _menuYesNo.ToArray(), RepayFullMenu);
                        break;
                }
            }
        }

        private void ProcessBorrowing(int val, string key)
        {
            switch (key)
            {
                case "Key_Amount":
                    _amount = val;
                    _borrowProcess = true;
                    Monitor.Log($"Selected {_amount}g", LogLevel.Info);
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
                    _amount = _config.MoneyAmount1;
                    _borrowProcess = true;
                    Monitor.Log("Selected 500g.", LogLevel.Info);
                    break;
                case "money_1k":
                    _amount = _config.MoneyAmount2;
                    _borrowProcess = true;
                    Monitor.Log("Selected 1,000g.", LogLevel.Info);
                    break;
                case "money_5k":
                    _amount = _config.MoneyAmount3;
                    _borrowProcess = true;
                    Monitor.Log("Selected 5,000g.", LogLevel.Info);
                    break;
                case "money_10k":
                    _amount = _config.MoneyAmount4;
                    _borrowProcess = true;
                    Monitor.Log("Selected 10,000g.", LogLevel.Info);
                    break;
                case "money_Cancel":
                    _borrowProcess = false;
                    Monitor.Log("Option Cancel");
                    break;
            }
        }

        private void BorrowDuration(Farmer who, string dur)
        {
            switch (dur)
            {
                case "time_3D":
                    _duration = _config.DayLength1;
                    _interest = _config.InterestModifier1;
                    Monitor.Log($"Selected {_config.DayLength1} days.");
                    break;
                case "time_7D":
                    _duration = _config.DayLength2;
                    _interest = _config.InterestModifier2;
                    Monitor.Log($"Selected {_config.DayLength2} days.");
                    break;
                case "time_14D":
                    _duration = _config.DayLength3;
                    _interest = _config.InterestModifier3;
                    Monitor.Log($"Selected {_config.DayLength3} days.");
                    break;
                case "time_28D":
                    _duration = _config.DayLength4;
                    _interest = _config.InterestModifier4;
                    Monitor.Log($"Selected {_config.DayLength4} days.");
                    break;
                case "time_Cancel":
                    _borrowProcess = false;
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
                    AddMessage(I18n.Msg_Payment_Remaining(_loanManager.Balance.ToString("N0"), _loanManager.Duration, _loanManager.CalculateAmountToPayToday.ToString("N0")), HUDMessage.newQuest_type);
                    break;
                case "repay_Custom":
                    Monitor.Log("Option repay custom", LogLevel.Debug);
                    InitiateRepayment(false, true);
                    break;
                case "repay_Full":
                    Monitor.Log("Option repay Full", LogLevel.Debug);
                    _repayProcess = true;
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
                    _repayProcess = false;
                    break;
                case "menu_Leave":
                    Monitor.Log("Option Leave", LogLevel.Debug);
                    _repayProcess = false;
                    break;
            }
        }
    }
}
