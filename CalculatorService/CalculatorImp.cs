using Interfaces;

namespace CalculatorService
{
    public class CalculatorImp : ICalculator
    {
        public int Add(int a, int b)
        {
            return a + b;
        }
    }
}