using System;
using System.Collections.Generic;
using System.Text;
using Autofac;
using JustSaying;
using JustSaying.Messaging.MessageHandling;

namespace YoutubeDownloader
{
    public class HandlerResolver : IHandlerResolver
    {
        private readonly IComponentContext _contianer;

        public HandlerResolver(IComponentContext contianer)
        {
            _contianer = contianer;
        }

        public IHandlerAsync<T> ResolveHandler<T>(HandlerResolutionContext context)
        {
            return (IHandlerAsync<T>)_contianer.Resolve<IHandlerAsync<T>>();
        }
    }
}
