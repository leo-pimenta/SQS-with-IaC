namespace Infra.Validations
{
    public class Validate
    {
        public static void This<TException>(bool isValid, string errorMessage) where TException : Exception
        {
            if (!isValid)
            {
                var exceptionInstance = Activator.CreateInstance(typeof(TException), errorMessage) as Exception;
                throw exceptionInstance ?? new ArgumentException("Validation failure");
            }
        }

        public static void This(bool isValid, string errorMessage)
        {
            if (!isValid)
            {
                throw new ArgumentException(errorMessage);
            }
        }
    }
}