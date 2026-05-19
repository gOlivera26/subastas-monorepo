namespace PortalSubastas.Providers.Application.ResponseDto.Common;

public class OperationResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    public int? Code { get; set; }
    public int TotalRows { get; set; }

    public static OperationResponse<T> SuccessResponse(T data, int total = 0)
    {
        return new OperationResponse<T>
        {
            Success = true,
            Message = "Operación exitosa.",
            Data = data,
            Code = 200,
            TotalRows = total
        };
    }

    public static OperationResponse<T> SuccessResponseMassive(T data, int total = 0)
    {
        return new OperationResponse<T>
        {
            Success = true,
            Message = "Operación masiva exitosa.",
            Data = data,
            Code = 200,
            TotalRows = total
        };
    }

    public static OperationResponse<T> NotFoundResponse()
    {
        return new OperationResponse<T>
        {
            Success = false,
            Message = "No se encontraron resultados.",
            Data = default,
            Code = 404
        };
    }

    public static OperationResponse<T> CustomErrorResponse(int code, string message, T? data = default)
    {
        return new OperationResponse<T>
        {
            Success = false,
            Message = message,
            Data = data,
            Code = code
        };
    }

    public static OperationResponse<T> ErrorResponse(string exception)
    {
        return new OperationResponse<T>
        {
            Success = false,
            Message = exception,
            Data = default,
            Code = 500
        };
    }

    public static OperationResponse<T> UnauthorizedResponse(string message = "No autorizado")
    {
        return new OperationResponse<T>
        {
            Success = false,
            Message = message,
            Data = default,
            Code = 401
        };
    }

    public static OperationResponse<T> BadRequestResponse(string message)
    {
        return new OperationResponse<T>
        {
            Success = false,
            Message = message,
            Data = default,
            Code = 400
        };
    }

    public static OperationResponse<T> ErrorFileResponse(string exception, object exceptionDetails)
    {
        return new OperationResponse<T>
        {
            Success = false,
            Message = exception,
            Data = default,
            Code = 500
        };
    }
}
