using Transaction.Tests.Unit.UnitTests.Presentation.Controllers.Clients;

namespace Transaction.Tests.Unit.UnitTests.Presentation.Controllers.Base;

public abstract class BaseControllerTests : ApiUnitTestBase
{
    protected TransactionsClient? TransactionsClient { get; private set; }

    protected MerchantsClient? MerchantsClient { get; private set; }

    [SetUp]
    public void InitializeClients()
    {
        if (HttpClient == null)
        {
            return;
        }

        TransactionsClient = new TransactionsClient(HttpClient);
        MerchantsClient = new MerchantsClient(HttpClient);
    }
}
