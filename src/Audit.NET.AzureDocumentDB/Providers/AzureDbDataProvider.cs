﻿using System;
using System.IO;
using System.Text;
using Audit.Core;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Audit.AzureDocumentDB.Providers
{
    /// <summary>
    /// Azure Document DB data access
    /// </summary>
    /// <remarks>
    /// Settings:
    /// - ConnectionString: Server url
    /// - AuthKey: Auth key for the Azure API
    /// - Database: Database name
    /// - Collection: Collection name
    /// </remarks>
    public class AzureDbDataProvider : AuditDataProvider
    {
        private string _connectionString;
        private string _authKey;
        private string _database;
        private string _collection;

        public string ConnectionString
        {
            get { return _connectionString; }
            set { _connectionString = value; }
        }

        public string AuthKey
        {
            get { return _authKey; }
            set { _authKey = value; }
        }

        public string Database
        {
            get { return _database; }
            set { _database = value; }
        }

        public string Collection
        {
            get { return _collection; }
            set { _collection = value; }
        }

        public override object InsertEvent(AuditEvent auditEvent)
        {
            var client = GetClient();
            var collectionUri = GetCollectionUri();
            Document doc = client.CreateDocumentAsync(collectionUri, auditEvent).Result;
            return doc.Id;
        }

        public override async Task<object> InsertEventAsync(AuditEvent auditEvent)
        {
            var client = GetClient();
            var collectionUri = GetCollectionUri();
            Document doc = await client.CreateDocumentAsync(collectionUri, auditEvent);
            return doc.Id;
        }

        public override void ReplaceEvent(object docId, AuditEvent auditEvent)
        {
            var client = GetClient();
            var docUri = UriFactory.CreateDocumentUri(_database, _collection, docId.ToString());
            Document doc;
            using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(auditEvent.ToJson())))
            {
                doc = JsonSerializable.LoadFrom<Document>(ms);
                doc.Id = docId.ToString();
            }
            client.ReplaceDocumentAsync(docUri, doc).Wait();
        }

        public override async Task ReplaceEventAsync(object docId, AuditEvent auditEvent)
        {
            var client = GetClient();
            var docUri = UriFactory.CreateDocumentUri(_database, _collection, docId.ToString());
            Document doc;
            using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(auditEvent.ToJson())))
            {
                doc = JsonSerializable.LoadFrom<Document>(ms);
                doc.Id = docId.ToString();
            }
            await client.ReplaceDocumentAsync(docUri, doc);
        }

        public override T GetEvent<T>(object docId)
        {
            var client = GetClient();
            //var docUri = UriFactory.CreateDocumentUri(_database, _collection, docId.ToString());
            var collectionUri = GetCollectionUri();
            var sql = new SqlQuerySpec($"SELECT * FROM {_collection} WHERE {_collection}.id = @id",
                new SqlParameterCollection(new SqlParameter[] { new SqlParameter() { Name = "@id", Value = docId.ToString() } }));
            return client.CreateDocumentQuery(collectionUri, sql)
                .AsEnumerable()
                .FirstOrDefault();
        }

        public override async Task<T> GetEventAsync<T>(object eventId)
        {
            return await Task.FromResult(GetEvent<T>(eventId));
        }

        private bool TestConnection()
        {
            try
            {
                var client = GetClient();
                client.OpenAsync().Wait();
                return true;
            }
            catch
            {
                return false;
            }
        }

        private DocumentClient GetClient()
        {
            return new DocumentClient(new Uri(_connectionString), _authKey);
        }

        private Uri GetCollectionUri()
        {
            return UriFactory.CreateDocumentCollectionUri(_database, _collection);
        }

        #region Events Query        
#pragma warning disable CS3001 // Argument feedOptions is not CLS-compliant with default null, just ignore the warning.
        /// <summary>
        /// Returns an IQueryable that enables the creation of queries against the audit events stored on Azure Document DB.
        /// </summary>
        /// <param name="feedOptions">The options for processing the query results feed.</param>
        public IQueryable<AuditEvent> QueryEvents(FeedOptions feedOptions = null)
        {
            var client = GetClient();
            var collectionUri = GetCollectionUri();
            return client.CreateDocumentQuery<AuditEvent>(collectionUri, feedOptions);
        }

        /// <summary>
        /// Returns an IQueryable that enables the creation of queries against the audit events stored on Azure Document DB, for the audit event type given.
        /// </summary>
        /// <typeparam name="T">The AuditEvent type</typeparam>
        /// <param name="feedOptions">The options for processing the query results feed.</param>
        public IQueryable<T> QueryEvents<T>(FeedOptions feedOptions = null) where T : AuditEvent
        {
            var client = GetClient();
            var collectionUri = GetCollectionUri();
            return client.CreateDocumentQuery<T>(collectionUri, feedOptions);
        }

        /// <summary>
        /// Returns an enumeration of audit events for the given Azure Document DB SQL expression.
        /// </summary>
        /// <param name="sqlExpression">The Azure Document DB SQL expression</param>
        /// <param name="feedOptions">The options for processing the query results feed.</param>
        public IEnumerable<AuditEvent> EnumerateEvents(string sqlExpression, FeedOptions feedOptions = null)
        {
            var client = GetClient();
            var collectionUri = GetCollectionUri();
            return client.CreateDocumentQuery<AuditEvent>(collectionUri, sqlExpression, feedOptions);
        }
        /// <summary>
        /// Returns an enumeration of audit events for the given Azure Document DB SQL expression and the event type given.
        /// </summary>
        /// <typeparam name="T">The AuditEvent type</typeparam>
        /// <param name="sqlExpression">The Azure Document DB SQL expression</param>
        /// <param name="feedOptions">The options for processing the query results feed.</param>
        public IEnumerable<T> EnumerateEvents<T>(string sqlExpression, FeedOptions feedOptions = null) where T : AuditEvent
        {
            var client = GetClient();
            var collectionUri = GetCollectionUri();
            return client.CreateDocumentQuery<T>(collectionUri, sqlExpression, feedOptions);
        }
#pragma warning restore CS3001 // Argument type is not CLS-compliant
        #endregion
    }
}
