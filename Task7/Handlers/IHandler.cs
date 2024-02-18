using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Task7.Handlers;

public interface IHandler
{
    Task<IApplicationStatus> GetApplicationStatus(string id);
}
