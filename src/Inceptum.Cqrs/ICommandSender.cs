﻿using System;
using Inceptum.Cqrs.Configuration;

namespace Inceptum.Cqrs
{
    public interface ICommandSender : IDisposable
    {
        void SendCommand<T>(T command, string boundedContext, CommandPriority priority=CommandPriority.Normal);
    }
}