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

            // updated condition to calculate base on cent amount
            private static double CalculateBaseDiscountPercent(long totalAmount)
            {
                double baseDiscountPercent = 0;

                if (totalAmount < 20000)
                {
                    return baseDiscountPercent;
                }

                if (totalAmount >= 20000 && totalAmount <= 50000)
                {
                    return baseDiscountPercent = 0.05;
                }

                if (totalAmount >= 50100 && totalAmount <= 80000)
                {
                    return baseDiscountPercent = 0.07;
                }
                
                if (totalAmount >= 80100 && totalAmount <= 120000)
                {
                    return baseDiscountPercent = 0.10;
                }


                return baseDiscountPercent = 0.15;
            }

            // updated condition to calculate base on cent amount
            private static double ConditionalDiscountPercent(long totalAmount)
            {
                double conditionalDiscountPercent = 0;
                // Prime number check (above RM500)
                if (totalAmount > 50000 && IsPrime(totalAmount/100))
                {
                    conditionalDiscountPercent += 0.08;
                }

                // Ends with 5 and above RM900
                if (totalAmount > 90000 && totalAmount/100 % 10 == 5)
                {
                    conditionalDiscountPercent += 0.10;
                }

                return conditionalDiscountPercent;
            }

            public static (long totalDiscount, long finalAmount, double totalDiscountPercent) CalculateDiscount(long totalAmount)
            {

                double baseDiscountPercent = CalculateBaseDiscountPercent(totalAmount);

                double conditionalDiscountPercent = ConditionalDiscountPercent(totalAmount);

                double totalDiscountPercent = baseDiscountPercent + conditionalDiscountPercent;

                //Calculate total discount percent, ensure it does not exceed 20%
                if (totalDiscountPercent > 0.20)
                {
                    totalDiscountPercent = 0.20;
                }

                long totalDiscount = (long)Math.Round(totalAmount * totalDiscountPercent);
                long finalAmount = totalAmount - totalDiscount;

                return (totalDiscount, finalAmount, totalDiscountPercent);
            }
        }
    }
}
