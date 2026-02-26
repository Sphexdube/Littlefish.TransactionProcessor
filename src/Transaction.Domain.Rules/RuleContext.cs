using Transaction.Domain.Entities;

namespace Transaction.Domain.Rules;

public record RuleContext(TransactionRecord Transaction, Tenant Tenant, decimal CurrentDailyMerchantTotal);
