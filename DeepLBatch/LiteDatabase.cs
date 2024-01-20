using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;

namespace DeepLBatch
{
    internal class LiteDatabase : IDisposable
    {
        private readonly string FileDbName = "TranslationCache.db";

        private const int DbVersion = 1;

        public LiteDB.LiteDatabase Db { get; init; }

        public string DbPath
        {
            get
            {
                return Path.Combine(AppContext.BaseDirectory, FileDbName);
            }
        }


        public LiteDatabase()
        {
            Db = new LiteDB.LiteDatabase(DbPath);
            Init();
        }

        /// <summary>
        /// Inits a database
        /// </summary>
        /// <returns>true if a new database was created.</returns>
        private void Init()
        {

            Db.UserVersion = DbVersion;
            ILiteCollection<Translation> translationCollection = Db.GetCollection<Translation>();

            translationCollection.EnsureIndex(x => x.IndexKey, true);
        }
        

        public void ResetDatabase()
        {
            Init();

            if(Db.BeginTrans() == false)
            {
                throw new ApplicationException("Unable to start transation");
            }

            foreach (string collectionName in Db.GetCollectionNames())
            {

                Db.GetCollection(collectionName).DeleteAll();
            }

            Db.Commit();
        }

        public void Dispose()
        {
            if(Db is not null)
            {
                Db.Dispose();
            }
        }
    }
}
