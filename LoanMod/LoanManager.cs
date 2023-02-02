using System;

namespace LoanMod
{
    public partial class ModEntry
    {
        internal class LoanManager
        {
            /// <summary>
            /// <param>Indicates whether the player is currently borrowing money.</param>
            /// </summary>
            public bool IsBorrowing { get; set; }

            /// <summary>
            /// <param>Shows the amount of money the player is borrowing.</param>
            /// </summary>
            public int AmountBorrowed { get; set; }

            /// <summary>
            /// <param>Shows the duration (in days) of the loan.</param>
            /// </summary>
            public int Duration { get; set; }

            /// <summary>
            /// <param>Shows the interest rate of the loan.</param>
            /// </summary>
            public float Interest { get; set; }

            /// <summary>
            /// <param>Shows the amount of money the player has already repayed.</param>
            /// </summary>
            public int AmountRepaid { get; set; }

            /// <summary>
            /// <param>Shows the amount of money the player has paid today.</param>
            /// </summary>
            public int AmountRepaidToday { get; set; }

            /// <summary>
            /// <param>Indicates if the player has already made a payment on the current day.</param>
            /// </summary>
            public bool HasPaid { get; set; }

            /// <summary>
            /// <param>Shows the daily repayment amount.</param>
            /// </summary>
            public int DailyAmount { get; set; }

            /// <summary>
            /// <param>Shows the current balance remaining to be paid off.</param>
            /// </summary>
            public int Balance { get; set; }

            /// <summary>
            /// <param>Shows the late payment charge rate.</param>
            /// </summary>
            public float LateChargeRate { get; set; }

            /// <summary>
            /// <param>Shows the late payment charge amount.</param>
            /// </summary>
            public int LateChargeAmount { get; set; }

            /// <summary>
            /// <param>Shows the days of late payments.</param>
            /// </summary>
            public int LateDays { get; set; }

            /// <summary>
            /// <param>Calculates the current balance remaining to be paid off.</param>
            /// </summary>
            internal double CalculateBalance
            {
                get
                {
                    double bal = (AmountBorrowed - AmountRepaid);
                    var balinterest = bal * Interest;

                    return Math.Round(bal + balinterest, MidpointRounding.AwayFromZero);
                }
            }

            /// <summary>
            /// <param>Calculates the amount of money the player has to pay at the end of the day.</param>
            /// </summary>
            internal int CalculateAmountToPayToday => Math.Max(DailyAmount - AmountRepaidToday, 0);

            /// <summary>
            /// <param>Calculates the daily amount based on the balance left to pay.</param>
            /// </summary>
            internal double CalculateInitDailyAmount
            {
                get
                {
                    var daily = CalculateBalance / Duration;
                    return Math.Round(daily, MidpointRounding.AwayFromZero);
                }
            }

            /// <summary>
            /// <param>Calculates the late payment fee amount based on the balance left to pay.</param>
            /// </summary>
            internal double CalculateLateFees
            {
                get
                {
                    double daily = (Balance * LateChargeRate);
                    return Math.Round(daily, MidpointRounding.AwayFromZero);
                }
            }

            /// <summary>
            /// <param>Resets the mod.</param>
            /// </summary>
            internal void InitiateReset()
            {
                IsBorrowing = false;
                AmountBorrowed = 0;
                Duration = 0;
                Interest = 0;
                AmountRepaid = 0;
                HasPaid = false;
                Balance = 0;
                DailyAmount = 0;
            }
        }
    }
}
