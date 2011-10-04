///
///   Set of classes to simplify interaction with Azure
///
///
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Description;
using Microsoft.ServiceBus.Messaging;


///
///   Key Concepts:
///     1) Azure Account
///     2) Storage
///         2 a)  SQL Storage
///         2 b)  NoSQL Storage
///         2 c)  Blob Storage
///     3) Messaging
///         3 a)  Synchronous messages (WCF)
///         3 b)  Asynchronous message queues
///         3 b)  Storage queues
///     4) .....
///
namespace AzurePlatform
{
    /// <summary>
    /// 
    /// </summary>
    public class AzureStorageOptions
    {
        public string   StorageAccountName;
        public int      RetryCount;
        public TimeSpan RetryInterval;

        public AzureStorageOptions(string storageAccountName, int retryCount, TimeSpan retryInterval)
        {
            Debug.Assert(null != storageAccountName);
            StorageAccountName = storageAccountName;
            RetryCount = retryCount;
            RetryInterval = retryInterval;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public class AzureAppFabricOptions
    {
        public string AppFabricOwnerName;
        public string AppFabricOwnerKey;
        public string ServiceBusNameSpace;
        public string ServiceBusScheme;

        public AzureAppFabricOptions(string appFabricOwnerName, string appFabricOwnerKey, string serviceBusNameSpace, string serviceBusScheme)
        {
            Debug.Assert(null != appFabricOwnerName);
            Debug.Assert(null != appFabricOwnerKey);
            Debug.Assert(null != serviceBusNameSpace);
            Debug.Assert(null != serviceBusScheme);

            AppFabricOwnerName   = appFabricOwnerName;
            AppFabricOwnerKey    = appFabricOwnerKey;
            ServiceBusNameSpace  = serviceBusNameSpace;
            ServiceBusScheme     = serviceBusScheme;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public class AzureDiagnosticsOptions
    {
        public int       LogTransferPeriod;
        public LogLevel  LogTransferLevel;
        public string    ConnectionName;

        public AzureDiagnosticsOptions(int logTransferPeriod, LogLevel logTransferLevel, string connectionName)
        {
            Debug.Assert(null != connectionName);

            LogTransferPeriod = logTransferPeriod;
            LogTransferLevel  = logTransferLevel;
            ConnectionName    = connectionName;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public class AzureOptions
    {
        public AzureStorageOptions      StorageOptions;
        public AzureAppFabricOptions    AppFabricOptions;
        public AzureDiagnosticsOptions  DiagnosticsOptions;

        public AzureOptions(AzureStorageOptions storageOptions, AzureAppFabricOptions appFabricOptions, AzureDiagnosticsOptions diagnosticsOptions)
        {
            Debug.Assert(null != storageOptions);
            Debug.Assert(null != appFabricOptions);
            Debug.Assert(null != diagnosticsOptions);

            StorageOptions = storageOptions;
            AppFabricOptions = appFabricOptions;
            DiagnosticsOptions = diagnosticsOptions;
        }
    }

    ///  <summary>
    ///
    ///  </summary>
    ///  
    public class AzureAccount
    {
        // storage account handles
        public CloudStorageAccount  Account;
        public CloudTableClient     TableClient;
        public TableServiceContext  TableServiceContext;
        public CloudBlobClient      BlobClient;
        public CloudQueueClient     QueueClient;

        // appfabric handles
        public SharedSecretCredential    MyManagementCredentials;
        public System.Uri                ServiceUri; 
        public ServiceBusNamespaceClient NamespaceClient; 

        
        public bool IsValid;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="storageAccountName"></param>
        /// <param name="appFabricOwnerName"></param>
        /// <param name="appFabricOwnerKey"></param>
        /// <param name="serviceBusNameSpace"></param>
        public AzureAccount(AzureOptions azureOptions)
        {
            Debug.Assert(null != azureOptions);

            IsValid = false;

            try
            {
                InitializeAzureStorage(azureOptions.StorageOptions);

                InitializeAppFabric(azureOptions.AppFabricOptions);

                // establish diagnostics
                InitializeDiagnostics(azureOptions.DiagnosticsOptions);
                
                IsValid = true;
            }
            catch (Exception)
            {
                // Not sure what to do with the exception. 
                // But IsValid will be false ....
            }
        }

        void InitializeDiagnostics(AzureDiagnosticsOptions options)
        {
            Debug.Assert(null != options);

            DiagnosticMonitorConfiguration dmc = DiagnosticMonitor.GetDefaultInitialConfiguration();

            dmc.Logs.ScheduledTransferPeriod = TimeSpan.FromMinutes(options.LogTransferPeriod);
            dmc.Logs.ScheduledTransferLogLevelFilter = options.LogTransferLevel;

            DiagnosticMonitor.Start(options.ConnectionName, dmc);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="storageAccountName"></param>
        private void InitializeAzureStorage(AzureStorageOptions options)
        {
            Debug.Assert(null != options);

            // TODO: put in the logic to appropriately load configuration
            bool useRoleEnvironment = false;
            string storageConnectionStringValue = null;

            if (useRoleEnvironment)
            {
                storageConnectionStringValue = RoleEnvironment.GetConfigurationSettingValue(options.StorageAccountName);
            }
            else
            {
                storageConnectionStringValue = ConfigurationManager.ConnectionStrings[options.StorageAccountName].ConnectionString;
            }

            Debug.Assert(null != storageConnectionStringValue);

            Account = CloudStorageAccount.Parse(storageConnectionStringValue);

            // This will not work unless run in the DevFabric or in Azure
            // Trace.WriteLine("Azure account credentials:" + Account.Credentials.ToString(), "Information");

            TableClient = Account.CreateCloudTableClient();
            TableClient.RetryPolicy = RetryPolicies.Retry(4, TimeSpan.Zero);
            TableServiceContext = TableClient.GetDataServiceContext();

            BlobClient = Account.CreateCloudBlobClient();
            BlobClient.RetryPolicy = RetryPolicies.Retry(4, TimeSpan.Zero);

            QueueClient = Account.CreateCloudQueueClient();
            QueueClient.RetryPolicy = RetryPolicies.Retry(4, TimeSpan.Zero);

        }

        private void InitializeAppFabric(AzureAppFabricOptions options)
        {
            Debug.Assert(null != options);

            // 1: Create the right credentials
            MyManagementCredentials = TransportClientCredentialBase.CreateSharedSecretCredential(options.AppFabricOwnerName, options.AppFabricOwnerKey);

            // 2: create the uri for the servicebus
            ServiceUri = ServiceBusEnvironment.CreateServiceUri(options.ServiceBusScheme, options.ServiceBusNameSpace, string.Empty);

            // 3: Create the Uri
            NamespaceClient = new ServiceBusNamespaceClient(ServiceUri, MyManagementCredentials);
        }

    } // Azure account

   
}
