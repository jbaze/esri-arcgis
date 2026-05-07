namespace UmojoParkingPoC.Domain
{
    public class ApiResult
    {
        public bool Success { get; protected set; }
        public string ErrorMessage { get; protected set; }

        public static ApiResult Ok() => new ApiResult { Success = true };
        public static ApiResult Fail(string error) => new ApiResult { Success = false, ErrorMessage = error };
    }

    public class ApiResult<T> : ApiResult
    {
        public T Data { get; private set; }

        public static ApiResult<T> Ok(T data) => new ApiResult<T> { Success = true, Data = data };
        public static new ApiResult<T> Fail(string error) => new ApiResult<T> { Success = false, ErrorMessage = error };
    }
}
