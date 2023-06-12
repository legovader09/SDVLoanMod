using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using System;
using LoanMod.Common;
using LoanMod.Common.Interfaces;

namespace LoanMod
{
    public partial class ModEntry : Mod
    {
        private ModConfig _config;
        private ILoanManager _loanManager;
        private bool _canSave;
        private bool _borrowProcess, _repayProcess;
        private int _amount, _duration;
        private float _interest;

        private void OnGameLaunched(object sender, GameLaunchedEventArgs e) => AddModFunctions();

        public override void Entry(IModHelper helper)
        {
            I18n.Init(helper.Translation);
            helper.Events.Input.ButtonPressed += OnButtonPressed;
            helper.Events.GameLoop.SaveLoaded += GameLoaded;
            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
            helper.Events.GameLoop.Saving += Saving;
            helper.Events.GameLoop.DayEnding += DayEnding;
            helper.Events.GameLoop.DayStarted += DayStarted;
            helper.Events.Display.MenuChanged += MenuChanged;

            _config = helper.ReadConfig<ModConfig>();
        }

        private void OnButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            // ignore if player hasn't loaded a save yet
            if (!Context.IsWorldReady || !Context.CanPlayerMove)
                return;

            if (!Helper.Input.IsDown(_config.LoanButton)) return;
            StartBorrow(1, "Key_Amount");
            Monitor.Log($"{Game1.player.Name} pressed {e.Button} to open Loan Menu.", LogLevel.Debug);
        }

        private void MenuChanged(object sender, MenuChangedEventArgs e)
        {
            if (_borrowProcess && Game1.player.canMove)
            {
                switch (_amount)
                {
                    case >= 0 when _duration == 0:
                        StartBorrow(2, "Key_Duration");
                        break;
                    case >= 0 when _duration > 0:
                        InitiateBorrow(_amount, _duration, _interest);
                        break;
                }
            }

            if (_repayProcess && Game1.player.canMove)
                StartBorrow(3, "Key_Repay");
        }

        private void InitiateBorrow(int option, int duration, float interest)
        {
            _loanManager.AmountBorrowed = option;
            _loanManager.Duration = duration;
            _loanManager.Interest = interest;
            _loanManager.Balance = (int)_loanManager.CalculateBalance;
            _loanManager.DailyAmount = (int)_loanManager.CalculateDailyAmount;

            Monitor.Log($"Amount: {option}, Duration: {duration}, Interest: {interest}.", LogLevel.Info);

            Game1.player.Money += option;

            _loanManager.IsBorrowing = true;
            _borrowProcess = false;

            Monitor.Log($"Is Borrowing: {_loanManager.IsBorrowing}.", LogLevel.Info);

            _amount = 0;
            _duration = 0;
            _interest = 0;

            AddMessage(I18n.Msg_Payment_Credited(_loanManager.AmountBorrowed.ToString("N0")), HUDMessage.achievement_type);

            if (_mobileApi?.GetRunningApp() == Helper.ModRegistry.ModID)
                _mobileApi.SetAppRunning(false);
        }

        private void InitiateRepayment(bool full, bool custom = false)
        {
            if (!_loanManager.IsBorrowing || _loanManager.Balance <= 0) return;

            //check if player wants to repay in full.
            if (custom)
            {
                Game1.activeClickableMenu = new NumberSelectionMenu(I18n.Msg_Startrepay(), (val, _, _) =>
                {
                    //if user chooses to repay full amount from custom menu
                    if (val == _loanManager.Balance)
                    {
                        StartBorrow(3, "Key_Repay");
                        return;
                    }

                    if (Game1.player.Money >= val)
                    {
                        Game1.player.Money -= val;
                        _loanManager.AmountRepaid += val;
                        _loanManager.Balance -= val;
                        //recalculate daily amount in case balance is lower than daily repayment
                        _loanManager.AmountRepaidToday += val;
                        AddMessage(I18n.Msg_Payment_Complete(val.ToString("N0")), HUDMessage.achievement_type);
                    }
                    Game1.activeClickableMenu = null;
                }, -1, 1, _loanManager.Balance, Math.Min(_loanManager.DailyAmount, _loanManager.Balance));
            }
            else if (full)
            {
                if (Game1.player.Money >= _loanManager.Balance)
                {   //Repays the remaining balance
                    Game1.player.Money -= _loanManager.Balance;
                    _loanManager.InitiateReset();
                    AddMessage(I18n.Msg_Payment_Full(), HUDMessage.achievement_type);
                }
                else
                {
                    AddMessage(I18n.Msg_Payment_Failed(_loanManager.Balance.ToString("N0")), HUDMessage.error_type);
                }
                _repayProcess = false;
            }
            else if (_loanManager.Balance > 0) //Check if you are still in loan contract
            {
                var moneyToRepay = _loanManager.CalculateAmountToPayToday;
                
                //If player has enough Money for the daily deduction amount
                if (Game1.player.Money >= moneyToRepay)
                {
                    //Checks if the balance is greater than or equal to the daily repayment amount
                    if (_loanManager.Balance > moneyToRepay)
                    {
                        Game1.player.Money -= moneyToRepay;
                        _loanManager.AmountRepaid += moneyToRepay;
                        _loanManager.Balance -= moneyToRepay;
                    }
                    else
                    {
                        //Repays the remaining balance
                        Game1.player.Money -= _loanManager.Balance;
                        _loanManager.IsBorrowing = false;
                        AddMessage(I18n.Msg_Payment_Full(), HUDMessage.achievement_type);
                    }
                    _loanManager.HasPaid = true;

                    if (_loanManager.Duration > 0) _loanManager.Duration--;
                    _loanManager.LateDays = 0;
                }
                else
                {
                    if (_config.LatePaymentChargeRate != 0)
                    {
                        _loanManager.LateChargeRate = _config.LatePaymentChargeRate;
                        _loanManager.LateChargeAmount = (int)_loanManager.CalculateLateFees;
                        AddMessage(I18n.Msg_Payment_Failed(_loanManager.DailyAmount.ToString("N0")), HUDMessage.error_type);
                        if (_loanManager.LateDays == 0)
                        {
                            AddMessage(I18n.Msg_Payment_Missed1(_loanManager.LateChargeAmount.ToString("N0")), HUDMessage.error_type);
                            _loanManager.LateDays++;
                        }
                        else
                        {
                            AddMessage(I18n.Msg_Payment_Missed2(_loanManager.LateChargeAmount.ToString("N0")), HUDMessage.error_type);
                            _loanManager.Balance += _loanManager.LateChargeAmount;
                        }
                    }
                    _loanManager.HasPaid = false;
                }
            }
        }

        private void GameLoaded(object sender, SaveLoadedEventArgs e)
        {
            Monitor.Log("Current Locale: " + Helper.Translation.LocaleEnum, LogLevel.Info);
            InitMenus();

            //checks if player is currently taking any loans, if so it will load all the loan data.
            if (Game1.player.IsMainPlayer)
                _loanManager = Helper.Data.ReadSaveData<ILoanManager>("Doomnik.MoneyManage");

            if (_loanManager != null && !_config.Reset) return;

            _loanManager = new LoanManager();
            _config.Reset = false;
            Helper.WriteConfig(_config);
            AddMessage(I18n.Msg_Create(), HUDMessage.achievement_type);
        }

        private void DayStarted(object sender, DayStartedEventArgs e)
        {
            if (_loanManager.IsBorrowing)
            {
                if (_loanManager.HasPaid)
                {
                    if (_loanManager.DailyAmount > 0)
                        AddMessage(I18n.Msg_Payment_Complete(_loanManager.CalculateAmountToPayToday.ToString("N0")), HUDMessage.achievement_type);

                    _loanManager.AmountRepaidToday = 0;
                    _loanManager.HasPaid = false;
                }
                if (_loanManager.Balance < _loanManager.DailyAmount) _loanManager.DailyAmount = _loanManager.Balance;
            }
            else
            {
                _loanManager.InitiateReset();
            }

            if (_loanManager.Balance >= 0) return;

            Monitor.Log($"Amount Borrowed vs Repaid: {_loanManager.AmountBorrowed} / {_loanManager.AmountRepaid}, Duration: {_loanManager.Duration}. Interest: {_loanManager.Interest}", LogLevel.Error);
            _loanManager.InitiateReset();
            AddMessage(I18n.Msg_Payment_Error(), HUDMessage.error_type);
        }

        /// <summary>
        /// This method prevents mods like SaveAnytime from interfering with repayments.
        /// </summary>
        private void DayEnding(object sender, DayEndingEventArgs e) => _canSave = true;

        private void Saving(object sender, SavingEventArgs e)
        {
            if (!_canSave) return;

            InitiateRepayment(false);

            if (Context.IsMainPlayer) Helper.Data.WriteSaveData("Doomnik.MoneyManage", _loanManager);
            _canSave = false;
        }
    }
}