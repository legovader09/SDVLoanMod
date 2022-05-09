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
        internal ITranslationHelper i18n => Helper.Translation;
        private LoanManager loanManager;

        private void OnGameLaunched(object sender, GameLaunchedEventArgs e) => AddConfigItems();

        public override void Entry(IModHelper helper)
        {
            helper.Events.Input.ButtonPressed += this.OnButtonPressed;
            helper.Events.GameLoop.SaveLoaded += this.GameLoaded;
            helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
            helper.Events.GameLoop.Saving += this.Saving;
            helper.Events.GameLoop.DayEnding += this.DayEnding;
            helper.Events.GameLoop.DayStarted += this.DayStarted;
            helper.Events.Display.MenuChanged += this.MenuChanged;
            
            Config = helper.ReadConfig<ModConfig>();

            InitMenus();
        }

        private void OnButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            // ignore if player hasn't loaded a save yet
            if (!Context.IsWorldReady || !Context.CanPlayerMove)
                return;

            if (this.Helper.Input.IsDown(Config.LoanButton))
            {
                StartBorrow(1, "Key_Amount");
                this.Monitor.Log($"{Game1.player.Name} pressed {e.Button} to open Loan Menu.", LogLevel.Info);
            }
        }

        private void MenuChanged(object sender, MenuChangedEventArgs e)
        {
            if (borrowProcess && Game1.player.canMove == true)
            {
                if (amount >= 0 && duration == 0)
                    StartBorrow(2, "Key_Duration");
                else if (amount >= 0 && duration > 0)
                    InitiateBorrow(amount, duration, interest);
            }

            if (repayProcess && Game1.player.canMove == true)
                StartBorrow(3, "Key_Repay");
        }

        private void InitiateBorrow(int option, int duration, float interest)
        {
            loanManager.AmountBorrowed = option;
            loanManager.Duration = duration;
            loanManager.Interest = interest;
            loanManager.Balance = (int)loanManager.CalculateBalance;
            loanManager.DailyAmount = (int)loanManager.CalculateDailyAmount;

            this.Monitor.Log($"Amount: {option}, Duration: {duration}, Interest: {interest}.", LogLevel.Info);

            Game1.player.Money += option;

            loanManager.IsBorrowing = true;
            borrowProcess = false;

            this.Monitor.Log($"Is Borrowing: {loanManager.IsBorrowing}.", LogLevel.Info);

            amount = 0;
            this.duration = 0;
            this.interest = 0;

            AddMessage(i18n.Get("msg.payment.credited", new { creditAmount = loanManager.AmountBorrowed }));
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
                        repayProcess = false;
                        loanManager.InitiateReset();
                    }
                    else
                    {
                        AddMessage(i18n.Get("msg.payment.failed", new { DailyAmount = loanManager.Balance }));
                        repayProcess = false;
                    }
                    return;
                }

                //Check if you are still in loan contract
                if (loanManager.Duration > 0)
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
                            loanManager.hasPaid = true;
                        }
                        else
                        {
                            //Repays the remaining balance
                            Game1.player.Money -= loanManager.Balance;
                            loanManager.hasPaid = true;
                            loanManager.IsBorrowing = false;
                            AddMessage(i18n.Get("msg.payment.full"));
                            //Game1.addHUDMessage(new HUDMessage("Thank you for your payment! You have successfully paid any outstanding balance in full.", HUDMessage.achievement_type));
                        }
                        loanManager.Duration -= 1;
                        loanManager.LateDays = 0;
                    }
                    else
                    {
                        if (Config.LatePaymentChargeRate != 0)
                        {
                            loanManager.LateChargeRate = Config.LatePaymentChargeRate;
                            loanManager.LateChargeAmount = (int)loanManager.CalculateLateFees;
                            AddMessage(i18n.Get("msg.payment.failed", new { DailyAmount = loanManager.DailyAmount }));
                            if (loanManager.LateDays == 0)
                            {
                                Game1.addHUDMessage(new HUDMessage(i18n.Get("msg.payment.missed-1", new { LateChargeAmount = loanManager.LateChargeAmount }), HUDMessage.error_type));
                                loanManager.LateDays += 1;
                            }
                            else
                            {
                                Game1.addHUDMessage(new HUDMessage(i18n.Get("msg.payment.missed-2", new { LateChargeAmount = loanManager.LateChargeAmount }), HUDMessage.error_type));
                                loanManager.Balance += loanManager.LateChargeAmount;
                            }
                        }
                        loanManager.hasPaid = false;
                    }
                }
            }
        }
        
        private void GameLoaded(object sender, SaveLoadedEventArgs e)
        {
            this.Monitor.Log("Current Locale: " + i18n.LocaleEnum, LogLevel.Info);

            try
            {
                //checks if player is currently taking any loans, if so it will load all the loan data.
                if (Game1.player.IsMainPlayer)
                    loanManager = Helper.Data.ReadSaveData<LoanManager>("Doomnik.MoneyManage");

                if (loanManager == null || Config.Reset)
                {
                    loanManager = new LoanManager();
                    loanManager.InitiateReset();
                    Config.Reset = false;
                    AddMessage(i18n.Get("msg.create"));
                }

                if (Game1.player.IsMainPlayer) //immediately saves loan file.
                    Helper.Data.WriteSaveData("Doomnik.MoneyManage", loanManager);
            }
            catch {  }
        }

        private void DayStarted(object sender, DayStartedEventArgs e)
        {
            //checks if player has made payment.
            if (loanManager.IsBorrowing)
            {
                if (loanManager.hasPaid)
                {
                    AddMessage(i18n.Get("msg.payment.complete", new { DailyAmount = loanManager.DailyAmount }));
                    loanManager.hasPaid = false;
                }
                if (loanManager.Balance < loanManager.DailyAmount) { loanManager.DailyAmount = loanManager.Balance; }
            }
            else loanManager.InitiateReset();

            if (loanManager.Balance < 0)
            {
                this.Monitor.Log($"Amount Borrowed vs Repaid: {loanManager.AmountBorrowed} / {loanManager.AmountRepaid}, Duration: {loanManager.Duration}. Interest: {loanManager.Interest}", LogLevel.Info);
                loanManager.InitiateReset();
                AddMessage(i18n.Get("msg.payment.error"));
            }
        }
        private void DayEnding(object sender, DayEndingEventArgs e) => canSave = true;

        private void Saving(object sender, SavingEventArgs e)
        {
            if (Game1.player.IsMainPlayer)
            {
                if (canSave)
                {
                    InitiateRepayment(false);
                    canSave = false;
                }
                //saving data
                Helper.Data.WriteSaveData("Doomnik.MoneyManage", loanManager);
                Helper.WriteConfig(Config);
            }
        }
    }
}