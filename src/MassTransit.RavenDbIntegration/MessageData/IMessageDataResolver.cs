using System;

namespace MassTransit.RavenDbIntegration.MessageData
{
    public interface IMessageDataResolver
    {
        /// <summary>
        ///     Returns the path for the specified address
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        string GetPath(Uri address);

        /// <summary>
        ///     Returns the address for the specified path
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        Uri GetAddress(string path);
    }
}