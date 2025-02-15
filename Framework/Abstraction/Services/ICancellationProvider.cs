﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CandyLauncher.Abstraction.Services
{
    public interface ICancellationProvider
    {
        CancellationToken CancellationToken { get; }
        bool IsCancellationRequested { get; }
    }
}
