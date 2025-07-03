namespace FourtitudeMiddleware.Helpers
{
    public class NumberHelpers
    {
        public static class NumberHelper
        {
            public static bool IsPrime(long number)
            {
                if (number <= 1)
                {
                    return false;
                }
                if (number == 2)
                {
                    return true;
                }
                if (number % 2 == 0)
                {
                    return false;
                }
                var boundary = (long)Math.Floor(Math.Sqrt(number));
                for (long i = 3; i <= boundary; i += 2)
                {
                    if (number % i == 0)
                    {
                        return false;
                    }
                }
                return true;
            }

            public static (long totalDiscount, long finalAmount) CalculateDiscount(long totalAmount)
            {
                double baseDiscountPercent = 0;
                if (totalAmount >= 200 && totalAmount <= 500)
                {
                    baseDiscountPercent = 0.05;
                }
                else if (totalAmount >= 501 && totalAmount <= 800)
                {
                    baseDiscountPercent = 0.07;
                }
                else if (totalAmount >= 801 && totalAmount <= 1200)
                {
                    baseDiscountPercent = 0.10;
                }
                else if (totalAmount > 1200)
                {
                    baseDiscountPercent = 0.15;
                }

                double conditionalDiscountPercent = 0;
                // Prime number check (above 500)
                if (totalAmount > 500 && NumberHelper.IsPrime(totalAmount))
                {
                    conditionalDiscountPercent += 0.08;
                }
                // Ends with 5 and above 900
                if (totalAmount > 900 && totalAmount % 10 == 5)
                {
                    conditionalDiscountPercent += 0.10;
                }

                double totalDiscountPercent = baseDiscountPercent + conditionalDiscountPercent;
                if (totalDiscountPercent > 0.20)
                {
                    totalDiscountPercent = 0.20;
                }

                long totalDiscount = (long)Math.Round(totalAmount * totalDiscountPercent);
                long finalAmount = totalAmount - totalDiscount;

                return (totalDiscount, finalAmount);
            }
        }
    }
}
