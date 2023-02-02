using GenericModConfigMenu;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using System.IO;

namespace LoanMod
{
    public partial class ModEntry
    {
        private IMobilePhoneApi mobileApi;
        private void AddModFunctions()
        {
            var configMenu = Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (configMenu != null)
            {
                // register mod
                configMenu.Register(
                    mod: ModManifest,
                    reset: () => Config = new ModConfig(),
                    save: () => Helper.WriteConfig(Config)
                );

                MainSection(configMenu);
                DurationSection(configMenu);
                InterestSection(configMenu);
                LegacyMoneySection(configMenu);
                AdvancedSelection(configMenu);
            }

            mobileApi = Helper.ModRegistry.GetApi<IMobilePhoneApi>("aedenthorn.MobilePhone");

            if (mobileApi == null || !Config.AddMobileApp) return;

            var appIcon = Helper.ModContent.Load<Texture2D>(Path.Combine("assets", "app_icon.png"));
            var success = mobileApi.AddApp(Helper.ModRegistry.ModID, I18n.App_Name(), () => StartBorrow(1, "Key_Amount"), appIcon);
            Monitor.Log($"loaded phone app successfully: {success}", LogLevel.Debug);
        }

        private void AdvancedSelection(IGenericModConfigMenuApi configMenu)
        {
            configMenu.AddSectionTitle(
                mod: ModManifest,
                text: () => "Advanced Options"
            );

            configMenu.AddParagraph(
                mod: ModManifest,
                text: () => "Only modify these values if you absolutely have to."
            );

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => "Use Input Box vs Dialog Boxes",
                tooltip: () => "Uses input box to enter a custom amount of money to borrow.",
                getValue: () => Config.CustomMoneyInput,
                setValue: value => Config.CustomMoneyInput = value
            );

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => "Reset Loan Profile",
                tooltip: () => "Resets the loan profile on the next save file you load.",
                getValue: () => Config.Reset,
                setValue: value => Config.Reset = value
            );
        }

        private void MainSection(IGenericModConfigMenuApi configMenu)
        {
            configMenu.AddSectionTitle(
                mod: ModManifest,
                text: () => "Main Options"
            );

            configMenu.AddKeybind(
                mod: ModManifest,
                getValue: () => Config.LoanButton,
                setValue: value => Config.LoanButton = value,
                name: () => "Change Keybind",
                tooltip: () => "Change the button to press to open the loan menu."
            );

            configMenu.AddNumberOption(
                mod: ModManifest,
                getValue: () => Config.LatePaymentChargeRate,
                setValue: value => Config.LatePaymentChargeRate = value,
                name: () => "Late Payment Interest Rate",
                min: 0f,
                interval: 100f
            );

            configMenu.AddNumberOption(
                mod: ModManifest,
                getValue: () => Config.MaxBorrowAmount,
                setValue: value => Config.MaxBorrowAmount = (int)value,
                name: () => "Maximum Borrow Amount",
                min: 100f,
                interval: 100f
            );

            configMenu.AddBoolOption(
                mod: ModManifest,
                getValue: () => Config.AddMobileApp,
                setValue: value => Config.AddMobileApp = value,
                name: () => "Add Mobile App"
            );
        }
        private void LegacyMoneySection(IGenericModConfigMenuApi configMenu)
        {
            configMenu.AddSectionTitle(
                   mod: ModManifest,
                   text: () => "Money Amount Dialog"
               );

            configMenu.AddParagraph(
                mod: ModManifest,
                text: () => "Only modify these values if you DO NOT have \"Use Input Box\" checked in the advanced section."
            );

            configMenu.AddNumberOption(
                mod: ModManifest,
                getValue: () => Config.MoneyAmount1,
                setValue: value => Config.MoneyAmount1 = (int)value,
                name: () => "Money Amount 1",
                min: 0f,
                interval: 100f
            );

            configMenu.AddNumberOption(
                mod: ModManifest,
                getValue: () => Config.MoneyAmount2,
                setValue: value => Config.MoneyAmount2 = (int)value,
                name: () => "Money Amount 2",
                min: 0f,
                interval: 100f
            );

            configMenu.AddNumberOption(
                mod: ModManifest,
                getValue: () => Config.MoneyAmount3,
                setValue: value => Config.MoneyAmount3 = (int)value,
                name: () => "Money Amount 3",
                min: 0f,
                interval: 100f
            );

            configMenu.AddNumberOption(
                mod: ModManifest,
                getValue: () => Config.MoneyAmount4,
                setValue: value => Config.MoneyAmount4 = (int)value,
                name: () => "Money Amount 4",
                min: 0f,
                interval: 100f
            );
        }
        private void DurationSection(IGenericModConfigMenuApi configMenu)
        {
            configMenu.AddSectionTitle(
                      mod: ModManifest,
                      text: () => "Loan Duration Menu Options"
                  );

            configMenu.AddParagraph(
                mod: ModManifest,
                text: () => "Value in days that define the duration of the loan."
            );

            configMenu.AddNumberOption(
                mod: ModManifest,
                getValue: () => Config.DayLength1,
                setValue: value => Config.DayLength1 = value,
                name: () => "Duration Option 1"
            );

            configMenu.AddNumberOption(
                mod: ModManifest,
                getValue: () => Config.DayLength2,
                setValue: value => Config.DayLength2 = value,
                name: () => "Duration Option 2"
            );

            configMenu.AddNumberOption(
                mod: ModManifest,
                getValue: () => Config.DayLength3,
                setValue: value => Config.DayLength3 = value,
                name: () => "Duration Option 3"
            );

            configMenu.AddNumberOption(
                mod: ModManifest,
                getValue: () => Config.DayLength4,
                setValue: value => Config.DayLength4 = value,
                name: () => "Duration Option 4"
            );
        }
        private void InterestSection(IGenericModConfigMenuApi configMenu)
        {
            configMenu.AddSectionTitle(
                      mod: ModManifest,
                      text: () => "Interest Menu Options"
                  );

            configMenu.AddParagraph(
                mod: ModManifest,
                text: () => "Each value corresponds to the Loan Duration menu above."
            );

            configMenu.AddNumberOption(
                mod: ModManifest,
                getValue: () => Config.InterestModifier1,
                setValue: value => Config.InterestModifier1 = value,
                name: () => "Interest Modifier 1"
            );

            configMenu.AddNumberOption(
                mod: ModManifest,
                getValue: () => Config.InterestModifier2,
                setValue: value => Config.InterestModifier2 = value,
                name: () => "Interest Modifier 2"
            );

            configMenu.AddNumberOption(
                mod: ModManifest,
                getValue: () => Config.InterestModifier3,
                setValue: value => Config.InterestModifier3 = value,
                name: () => "Interest Modifier 3"
            );

            configMenu.AddNumberOption(
                mod: ModManifest,
                getValue: () => Config.InterestModifier4,
                setValue: value => Config.InterestModifier4 = value,
                name: () => "Interest Modifier 4"
            );
        }
    }
}
