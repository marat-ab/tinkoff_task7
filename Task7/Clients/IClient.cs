using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Task7.Clients;

public interface IClient
{
    Task<IResponse> GetApplicationStatus(string id, CancellationToken cancellationToken);
}
