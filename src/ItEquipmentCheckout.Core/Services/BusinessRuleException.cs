namespace ItEquipmentCheckout.Core.Services;

public sealed class BusinessRuleException : Exception
{
    public BusinessRuleException(string code, string message)
        : base(message)
    {
        Code = code;
    }

    public string Code { get; }
}
