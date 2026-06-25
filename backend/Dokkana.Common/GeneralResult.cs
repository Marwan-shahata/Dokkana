namespace Dokkana.Common
{
    // Standard wrapper for API responses so every endpoint returns a consistent shape.
    public class GeneralResult
    {
        public bool Success { get; set; }
        public string? Message { get; set; }

        public static GeneralResult Ok(string? message = null)
            => new() { Success = true, Message = message };

        public static GeneralResult Fail(string message)
            => new() { Success = false, Message = message };
    }

    public class GeneralResult<T> : GeneralResult
    {
        public T? Data { get; set; }

        public static GeneralResult<T> Ok(T data, string? message = null)
            => new() { Success = true, Data = data, Message = message };

        public static new GeneralResult<T> Fail(string message)
            => new() { Success = false, Message = message };
    }
}
