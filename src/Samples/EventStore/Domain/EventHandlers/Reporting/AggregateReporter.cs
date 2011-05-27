using System.Diagnostics;

using Domain.Events;

using Lokad.Cqrs;

namespace Domain.EventHandlers.Reporting
{
    public class AggregateReporter : Define.Subscribe<MessageCreated>, Define.Subscribe<MessageEdited>
    {
        #region Implementation of IConsume<in AggregateCreated>

        public void Consume(MessageCreated message)
        {
            Trace.TraceInformation("UPDATING REPORTING DB FOR MESSAGE {0}[{1}]...", message.Message, message.Id);
        }

        #endregion

        #region Implementation of IConsume<in MessageEdited>

        public void Consume(MessageEdited message)
        {
            Trace.TraceInformation("UPDATING REPORTING DB FOR MESSAGE {0}[{1}]...", message.Message, message.Id);
        }

        #endregion
    }
}