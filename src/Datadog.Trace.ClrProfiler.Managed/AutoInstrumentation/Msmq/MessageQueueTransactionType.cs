﻿namespace Datadog.Trace.ClrProfiler.AutoInstrumentation.Msmq
{
    /// <summary>
    /// message queue transaction type
    /// </summary>
    public enum MessageQueueTransactionType
    {
        /// <summary>
        /// Operation will not be transactional
        /// </summary>
        None = 0,

        /// <summary>
        /// A transaction type used for Microsoft Transaction Server (MTS) or COM+ 1.0 Services.
        /// If there is already an MTS transaction context, it will be used when sending
        /// or receiving the message.
        /// </summary>
        Automatic = 1,

        /// <summary>
        /// A transaction type used for single internal transactions.
        /// </summary>
        Single = 3
    }
}