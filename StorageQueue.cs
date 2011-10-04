using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;
using Microsoft.WindowsAzure.ServiceRuntime;

///
namespace AzurePlatform
{

    /// <summary>
    /// 
    /// </summary>
    public class StorageQueueMessage : IAzureQueueMessage
    {
        private CloudQueue thisQueue;
        public CloudQueueMessage azureMessage;


        public StorageQueueMessage(CloudQueue queue, string Content)
        {
            thisQueue = queue;
            azureMessage = new CloudQueueMessage(Content);
        }

        public StorageQueueMessage(CloudQueue queue, CloudQueueMessage Message)
        {
            thisQueue = queue;
            azureMessage = Message;
        }

        public string Content { get { return azureMessage.AsString; } }
        public void Delete() { thisQueue.DeleteMessage(azureMessage); }
        public void Send() { thisQueue.AddMessage(azureMessage); }
    } // class AzureTableQueueMessage

 
    /// <summary>
    /// 
    /// </summary>
    public class StorageQueue : IAzureQueue
    {
        private  AzureAccount cloudAccount;
        private  CloudQueue physicalQueue;

        public StorageQueue(AzureAccount azureAccount)
        {
            cloudAccount = azureAccount;
        }

        // IsValid
        public bool IsValid()
        {
            return cloudAccount.IsValid;
        }

        // Initialize
        public void Initialize(string queueName)
        {
            // create the queue
            try
            {
                // ?? why is this API behavior different from Tables?
                physicalQueue = cloudAccount.QueueClient.GetQueueReference(queueName);
                physicalQueue.CreateIfNotExist();
            }
            catch (StorageClientException ex)
            {
                // TODO: not sure what to do with the exception
                throw ex;
            }
        }

        // Create message to send
        public IAzureQueueMessage CreateMessage(string Content)
        {
            return new StorageQueueMessage(physicalQueue, Content);
        }

        // Send a message
        public void SendMessage(IAzureQueueMessage message)
        {
            message.Send();
        }

        // Receive a message
        public void ReceiveMessage(out IAzureQueueMessage message)
        {
            CloudQueueMessage cloudMessage = physicalQueue.GetMessage();
            if (null == cloudMessage)
            {
                message = null;
            }
            else
            {
                message = new StorageQueueMessage(physicalQueue, cloudMessage);
            }
        }

        // Close
        public void Close()
        {
            physicalQueue.Delete();
        }

    }

}
