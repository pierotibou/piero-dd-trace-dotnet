using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading.Tasks;

namespace Samples.Wcf
{
    public class CustomReplyChannelRequestContext : RequestContext
    {
        private RequestContext _innerContext;
        private Message _requestMessage;

        public CustomReplyChannelRequestContext(RequestContext innerContext)
        {
            _innerContext = innerContext;
        }

        public override Message RequestMessage
        {
            get
            {
                if (_requestMessage == null)
                {
                    _requestMessage = _innerContext.RequestMessage;
                    // throw new CustomFailedAuthenticationFaultException();
                }

                return _requestMessage;
            }
        }

        public override void Abort()
            => _innerContext.Abort();

        public override IAsyncResult BeginReply(Message message, AsyncCallback callback, object state)
            => _innerContext.BeginReply(message, callback, state);

        public override IAsyncResult BeginReply(Message message, TimeSpan timeout, AsyncCallback callback, object state)
            => _innerContext.BeginReply(message, timeout, callback, state);

        public override void Close()
            => _innerContext.Close();

        public override void Close(TimeSpan timeout)
            => _innerContext.Close(timeout);

        public override void EndReply(IAsyncResult result)
            => _innerContext.EndReply(result);

        public override void Reply(Message message)
            => _innerContext.Reply(message);

        public override void Reply(Message message, TimeSpan timeout)
            => _innerContext.Reply(message, timeout);

        public class CustomFailedAuthenticationFaultException : Exception
        {
        }
    }
}
