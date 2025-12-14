using System;

namespace StarkAid.WindowsForms.Models;

public class PaymentRequiredException : Exception
{
    public string? RawBody { get; }
    public int? RequiredCoins { get; }

    public PaymentRequiredException(string message, string? rawBody = null, int? requiredCoins = null) : base(message)
    {
        RawBody = rawBody;
        RequiredCoins = requiredCoins;
    }
}

