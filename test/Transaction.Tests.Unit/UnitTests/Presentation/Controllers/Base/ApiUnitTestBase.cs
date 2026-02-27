using Microsoft.AspNetCore.Mvc.Testing;
using Transaction.Application.Constants;
using Transaction.Tests.Unit.Setup;

namespace Transaction.Tests.Unit.UnitTests.Presentation.Controllers.Base;

public abstract class ApiUnitTestBase
{
    protected HttpClient? HttpClient;
    protected IServiceProvider? ServiceProvider;
    private WebApplicationFactory<Program>? _factory;

    [SetUp]
    public void Initialize()
    {
        _factory = TestSetup.CreateWebApplicationFactory();
        HttpClient = _factory.CreateClient();
        ServiceProvider = _factory.Services;

        SetDefaultHeaders();
    }

    [TearDown]
    public void TearDown()
    {
        HttpClient?.Dispose();
        _factory?.Dispose();
    }

    private void SetDefaultHeaders()
    {
        HttpClient!.DefaultRequestHeaders.Clear();
        HttpClient!.DefaultRequestHeaders.Add(RequestHeaderKeys.CorrelationId, Guid.NewGuid().ToString());
    }
}
