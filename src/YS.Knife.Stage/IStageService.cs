﻿using System;
using System.Threading;
using System.Threading.Tasks;

namespace YS.Knife.Stage
{
    public interface IStageService
    {
        string StageName { get; }
        string EnvironmentName { get; }
        Task Run(CancellationToken cancellationToken = default);
    }
}
