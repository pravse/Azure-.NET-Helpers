using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Description;
using Microsoft.ServiceBus.Messaging;


///
namespace AzurePaaS
{

    /// <summary>
    /// 
    /// </summary>
    public class AzureSBQueue : IAzureQueue
    {
        private AzureAccount cloudAccount;
        private Microsoft.ServiceBus.Messaging.Queue physicalQueue;

        public class AzureSBMessage : IAzureQueueMessage
        {
        }

        public AzureSBQueue(AzureAccount azureAccount)
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
                // Create a queue
                physicalQueue = cloudAccount.NamespaceClient.CreateQueue(queueName);

                // 5: create a factory
                MessagingFactory messageFactory = MessagingFactory.Create(serviceUri, myManagementCredentials);

                // 6: create a queue client
                QueueClient queueClient = messageFactory.CreateQueueClient(trialQueue);

                // 7: create a message sender
                MessageSender messageSender = queueClient.CreateSender();
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
            return new AzureTableQueueMessage(physicalQueue, Content);
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
            message = new AzureTableQueueMessage(physicalQueue, cloudMessage);
        }

        // Close
        public void Close()
        {
            physicalQueue.Delete();
        }

        /****
         *            // 
            

            // 8: create a message to send
            BrokeredMessage trialMessage = BrokeredMessage.CreateMessage();
            trialMessage.Properties["Foo"] = "Hello World";
            trialMessage.Label = "HW 1";

            // 9: send the message
            messageSender.Send(trialMessage);

            // 10: create a message receiver
            MessageReceiver messageReceiver = queueClient.CreateReceiver();

            // 11: read a message from the queue
            BrokeredMessage recvdMessage;
            messageReceiver.TryReceive(out recvdMessage);

            // 12: check integrity of the message
            Debug.Assert(recvdMessage.Label.Equals("HW 1"));

            // 13: close the message
            recvdMessage.Complete();

         * ****/
    }
}
