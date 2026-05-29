public readonly struct StackPurchaseResult
{
    public StackPurchaseResult(bool success, string message)
    {
        Success = success;
        Message = message;
    }

    public bool Success { get; }
    public string Message { get; }

    public static StackPurchaseResult Ok(string message)
    {
        return new StackPurchaseResult(true, message);
    }

    public static StackPurchaseResult Fail(string message)
    {
        return new StackPurchaseResult(false, message);
    }
}
