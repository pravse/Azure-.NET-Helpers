using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;

///
namespace AzurePlatform
{

    ///  <summary>
    ///
    ///  </summary>
    ///  

    /// <summary>
    /// 
    /// </summary>
    public interface IAzureQueueMessage 
    {
        string Content { get; }

        void Delete();

        void Send();
    }

    /// <summary>
    /// 
    /// </summary>  
    public interface IAzureQueue
    {
        // IsValid
        bool IsValid();

        // Initialize
        void Initialize(string queueName);

        // Create message to send
        IAzureQueueMessage CreateMessage(string Content);

        // Send a message
        void SendMessage(IAzureQueueMessage message);

        // Receive a message
        void ReceiveMessage(out IAzureQueueMessage message);

        // Close
        void Close();
    }

}
