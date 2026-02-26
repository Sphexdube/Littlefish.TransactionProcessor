namespace Transaction.Application.Constants;

public static class ValidationMessages
{
    public const string TransactionIdRequired = "Transaction ID is required";

    public const string MerchantIdRequired = "Merchant ID is required";

    public const string AmountRequired = "Amount is required";

    public const string CurrencyRequired = "Currency is required";

    public const string CurrencyInvalidLength = "Currency must be 3 characters";

    public const string TypeRequired = "Transaction type is required";

    public const string TypeInvalid = "Transaction type must be PURCHASE, REFUND, or REVERSAL";

    public const string OccurredAtRequired = "OccurredAt timestamp is required";

    public const string OriginalTransactionIdRequiredForRefund = "OriginalTransactionId is required for REFUND transactions";

    public const string BatchSizeInvalid = "Batch must contain between 100 and 5000 transactions";
}
