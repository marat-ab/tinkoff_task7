﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Task7.Handlers;

public record FailureStatus(DateTime? LastRequestTime, int RetriesCount) : IApplicationStatus;