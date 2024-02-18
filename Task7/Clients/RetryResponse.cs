using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Task7.Clients;

public record RetryResponse(TimeSpan Delay) : IResponse;
