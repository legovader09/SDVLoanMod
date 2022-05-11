using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace LoanMod
{
    public partial class ModEntry : Mod
    {
        internal ModConfig Config;
        internal bool canSave;
        private bool borrowProcess, repayProcess;
        private int amount, duration;
        private float interest;
        internal ITranslationHelper I18n => Helper.Translation;
        private LoanManager loanManager;

        private void OnGameLaunched(object sender, GameLaunchedEventArgs e) => AddModFunctions();

        public override void Entry(IModHelper helper)
        {
            helper.Events.Input.ButtonPressed += OnButtonPressed;
            helper.Events.GameLoop.SaveLoaded += GameLoaded;
            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
            helper.Events.GameLoop.Saving += Saving;
            helper.Events.GameLoop.DayEnding += DayEnding;
            helper.Events.GameLoop.DayStarted += DayStarted;
            helper.Events.Display.MenuChanged += MenuChanged;

            Config = helper.ReadConfig<ModConfig>();

            InitMenus();
        }

        private void OnButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            // ignore if player hasn't loaded a save yet
            if (!Context.IsWorldReady || !Context.CanPlayerMove)
                return;

            if (Helper.Input.IsDown(Config.LoanButton))
            {
                StartBorrow(1, "Key_Amount");
                Monitor.Log($"{Game1.player.Name} pressed {e.Button} to open Loan Menu.", LogLevel.Info);
            }
        }

        private void MenuChanged(object sender, MenuChangedEventArgs e)
        {
            if (borrowProcess && Game1.player.canMove)
            {
                if (amount >= 0 && duration == 0)
                    StartBorrow(2, "Key_Duration");
                else if (amount >= 0 && duration > 0)
                    InitiateBorrow(amount, duration, interest);
            }

            if (repayProcess && Game1.player.canMove)
                StartBorrow(3, "Key_Repay");
        }

        private void InitiateBorrow(int option, int duration, float interest)
        {
            loanManager.AmountBorrowed = option;
            loanManager.Duration = duration;
            loanManager.Interest = interest;
            loanManager.Balance = (int)loanManager.CalculateBalance;
            loanManager.DailyAmount = (int)loanManager.CalculateDailyAmount;

            Monitor.Log($"Amount: {option}, Duration: {duration}, Interest: {interest}.", LogLevel.Info);

            Game1.player.Money += option;

            loanManager.IsBorrowing = true;
            borrowProcess = false;

            Monitor.Log($"Is Borrowing: {loanManager.IsBorrowing}.", LogLevel.Info);

            amount = 0;
            this.duration = 0;
            this.interest = 0;

            AddMessage(I18n.Get("msg.payment.credited", new { creditAmount = loanManager.AmountBorrowed }), HUDMessage.achievement_type);

            if (mobileApi?.GetRunningApp() == Helper.ModRegistry.ModID)
                mobileApi.SetAppRunning(false);
        }

        private void InitiateRepayment(bool full)
        {
            if (loanManager.IsBorrowing && loanManager.Balance > 0 )
            {
                //check if player wants to repay in full.
                if (full)
                {
                    if (Game1.player.Money >= loanManager.Balance)
                    {   //Repays the remaining balance
                        Game1.player.Money -= loanManager.Balance;
                        loanManager.InitiateReset();
                        AddMessage(I18n.Get("msg.payment.full"), HUDMessage.achievement_type);
                    }
                    else
                    {
                        AddMessage(I18n.Get("msg.payment.failed", new { DailyAmount = loanManager.Balance }), HUDMessage.error_type);
                    }
                    repayProcess = false;
                    return;
                }

                //Check if you are still in loan contract
                if (loanManager.Balance > 0)
                {
                    //If player has enough Money for the daily deduction amount
                    if (Game1.player.Money >= loanManager.DailyAmount)
                    {
                        //Checks if the balance is greater than or equal to the daily repayment amount
                        if (loanManager.Balance > loanManager.DailyAmount)
                        {
                            Game1.player.Money -= loanManager.DailyAmount;
                            loanManager.AmountRepaid += loanManager.DailyAmount;
                            loanManager.Balance -= loanManager.DailyAmount;
                        }
                        else
                        {
                            //Repays the remaining balance
                            Game1.player.Money -= loanManager.Balance;
                            loanManager.IsBorrowing = false;
                            AddMessage(I18n.Get("msg.payment.full"), HUDMessage.achievement_type);
                        }
                        loanManager.HasPaid = true;

                        if (loanManager.Duration > 0) loanManager.Duration--;
                        loanManager.LateDays = 0;
                    }
                    else
                    {
                        if (Config.LatePaymentChargeRate != 0)
                        {
                            loanManager.LateChargeRate = Config.LatePaymentChargeRate;
                            loanManager.LateChargeAmount = (int)loanManager.CalculateLateFees;
                            AddMessage(I18n.Get("msg.payment.failed", new { loanManager.DailyAmount }), HUDMessage.error_type);
                            if (loanManager.LateDays == 0)
                            {
                                Game1.addHUDMessage(new HUDMessage(I18n.Get("msg.payment.missed-1", new { loanManager.LateChargeAmount }), HUDMessage.error_type));
                                loanManager.LateDays++;
                            }
                            else
                            {
                                Game1.addHUDMessage(new HUDMessage(I18n.Get("msg.payment.missed-2", new { loanManager.LateChargeAmount }), HUDMessage.error_type));
                                loanManager.Balance += loanManager.LateChargeAmount;
                            }
                        }
                        loanManager.HasPaid = false;
                    }
                }
            }
        }

        private void GameLoaded(object sender, SaveLoadedEventArgs e)
        {
            Monitor.Log("Current Locale: " + I18n.LocaleEnum, LogLevel.Info);

            //checks if player is currently taking any loans, if so it will load all the loan data.
            if (Game1.player.IsMainPlayer)
                loanManager = this.Helper.Data.ReadSaveData<LoanManager>("Doomnik.MoneyManage");

            if (loanManager == null || Config.Reset)
            {
                loanManager = new LoanManager();
                Config.Reset = false;
                AddMessage(I18n.Get("msg.create"), HUDMessage.achievement_type);
            }
        }

        private void DayStarted(object sender, DayStartedEventArgs e)
        {
            //checks if player has made payment.
            if (loanManager.IsBorrowing)
            {
                if (loanManager.HasPaid)
                {
                    AddMessage(I18n.Get("msg.payment.complete", new { loanManager.DailyAmount }), HUDMessage.achievement_type);
                    loanManager.HasPaid = false;
                }
                if (loanManager.Balance < loanManager.DailyAmount) { loanManager.DailyAmount = loanManager.Balance; }
            }
            else
            {
                loanManager.InitiateReset();
            }

            if (loanManager.Balance < 0)
            {
                Monitor.Log($"Amount Borrowed vs Repaid: {loanManager.AmountBorrowed} / {loanManager.AmountRepaid}, Duration: {loanManager.Duration}. Interest: {loanManager.Interest}", LogLevel.Info);
                loanManager.InitiateReset();
                AddMessage(I18n.Get("msg.payment.error"), HUDMessage.error_type);
            }
        }

        /// <summary>
        /// This method prevents mods like SaveAnytime from interfering with repayments.
        /// </summary>
        private void DayEnding(object sender, DayEndingEventArgs e) => canSave = true;

        private void Saving(object sender, SavingEventArgs e)
        {
            if (Game1.player.IsMainPlayer)
            {
                if (canSave)
                {
                    InitiateRepayment(false);
                    Helper.Data.WriteSaveData("Doomnik.MoneyManage", loanManager);

                    canSave = false;
                }
            }
        }
    }
}