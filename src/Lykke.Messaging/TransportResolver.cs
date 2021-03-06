﻿using System;
using System.Collections.Generic;

namespace Lykke.Messaging
{
    public class TransportResolver : ITransportResolver
    {
        private readonly Dictionary<string, TransportInfo> m_Transports = new Dictionary<string, TransportInfo>();
        private readonly Dictionary<string, JailStrategy> m_JailStrategies = new Dictionary<string, JailStrategy>
        {
            {"None", JailStrategy.None},
            {"MachineName", JailStrategy.MachineName},
            {"Guid", JailStrategy.Guid},
        };

        //TODO: need to register transports in some better way
        public TransportResolver(IDictionary<string, TransportInfo> transports, IDictionary<string, JailStrategy> jailStrategies = null)
        {
            if (transports == null)
                throw new ArgumentNullException(nameof(transports));

            m_Transports = new Dictionary<string, TransportInfo>(transports);

            if (jailStrategies != null)
            {
                foreach (var jailStrategy in jailStrategies)
                {
                    if (m_JailStrategies.ContainsKey(jailStrategy.Key))
                        throw new ArgumentOutOfRangeException(
                            nameof(jailStrategies), $"Jail strategy with key {jailStrategy.Key} already registered.");

                    m_JailStrategies.Add(jailStrategy.Key, jailStrategy.Value);
                }
            }

            foreach (var transportInfo in m_Transports)
            {
                if(!m_JailStrategies.TryGetValue(transportInfo.Value.JailStrategyName ?? "None", out var strategy))
                    throw new ArgumentOutOfRangeException(
                        nameof(jailStrategies),
                        string.Format(
                            "Incorrect jail strategy with name {1} set for transport {0}. Make sure jail strategy {1} is registered for transport configuration.",
                            transportInfo.Key,
                            transportInfo.Value.JailStrategyName));

                transportInfo.Value.JailStrategy = strategy;
            }
        }

        #region ITransportResolver Members

        public TransportInfo GetTransport(string transportId)
        {
            return m_Transports.TryGetValue(transportId, out var transport) ? transport : null;
        }

        #endregion
    }
}