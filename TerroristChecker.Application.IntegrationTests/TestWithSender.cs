using MediatR;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TerroristsChecker.Application.IntegrationTests;

public abstract class TestWithSender : IClassFixture<TestWebApplicationFactory>
{
    private readonly IServiceScope _scope;
    protected readonly ISender Sender;

    protected TestWithSender(TestWebApplicationFactory factory)
    {
        _scope = factory.Services.CreateScope();

        Sender = _scope.ServiceProvider.GetRequiredService<ISender>();
    }
}
