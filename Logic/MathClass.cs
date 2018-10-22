namespace Logic
{
    public static class MathClass
    {
        public static decimal Add(this decimal value1, decimal value2)
        {
            return value1 + value2;
        }
        public static decimal Subtract(this decimal value1, decimal value2)
        {
            return value1 - value2;
        }
        public static decimal Multiply(this decimal value1, decimal value2)
        {
            return value1 * value2;
        }
        public static decimal Divide(this decimal value1, decimal value2)
        {
            return value1 / value2;
        }
        public static decimal AbsoluteValue(this decimal value)
        {
            if (value < 0)
            {
                value *= -1;
            }
            return value;
        }
        public static decimal AddThreeValues(this decimal value1, 
            decimal value2, decimal value3)
        {
            decimal result = 0;
            result = value1.Add(result);
            result = Add(value2, result);
            result = result.Add(value3);
            return result;
        }
    }
}
