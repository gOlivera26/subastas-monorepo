namespace PortalSubastas.Reporting.Application.ResponseDto.Common;

public sealed class OperationResponse<T>
{
    public bool Success { get; init; }
    public int Code { get; init; }
    public string Message { get; init; } = string.Empty;
    public T? Data { get; init; }

    public static OperationResponseBuilder<T> CreateBuilder() => new();

    public sealed class OperationResponseBuilder<TBuilder>
    {
        private bool _success = true;
        private int _code = 200;
        private string _message = string.Empty;
        private TBuilder? _data;

        public OperationResponseBuilder<TBuilder> WithSuccess(bool success)
        {
            _success = success;
            return this;
        }

        public OperationResponseBuilder<TBuilder> WithCode(int code)
        {
            _code = code;
            return this;
        }

        public OperationResponseBuilder<TBuilder> WithMessage(string message)
        {
            _message = message;
            return this;
        }

        public OperationResponseBuilder<TBuilder> WithData(TBuilder? data)
        {
            _data = data;
            return this;
        }

        public OperationResponse<TBuilder> Build()
        {
            return new OperationResponse<TBuilder>
            {
                Success = _success,
                Code = _code,
                Message = _message,
                Data = _data
            };
        }
    }
}
