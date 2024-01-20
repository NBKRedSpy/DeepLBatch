using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace DeepLBatch
{
    internal class DbRepository : IDbRepository
    {
        private LiteDB.LiteDatabase _liteDb { get; set; }

        private LiteDatabase _db;

        public DbRepository(DeepLBatch.LiteDatabase db)
        {
            _db = db;
            _liteDb = db.Db;
        }

        public Translation Get(string indexKey)
        {
            return _liteDb.GetCollection<Translation>().FindOne(x => x.IndexKey == indexKey);
        }


        /// <summary>
        /// Adds or updates the translation to the cache.
        /// Note - Will throw error if an item exists but the translation
        /// but the ID has not been set.
        /// </summary>
        /// <param name="translation"></param>
        public void Add(Translation translation)
        {
            _liteDb.GetCollection<Translation>().Upsert(translation);
        }

        /// <summary>
        /// Tries to upsert an entry.  If an entry exists with an identical IndexKey,
        /// The item in the database will be updated.
        /// </summary>
        /// <param name="translation">The translation to add/update.  
        /// If an existing item exists, the ID will be set to the existing item.</param>
        /// <returns>Returns true if an existing item was updated.</returns>
        public bool UpsertByKey(Translation translation)
        {
            Translation? result = null;
            ILiteCollection<Translation> transCollecation = _liteDb.GetCollection<Translation>();

            bool wasUpdated = false;

            if (translation.ID is null)
            {
                //Try to get existing by key
                result = transCollecation.FindOne(x => x.IndexKey == translation.IndexKey);
                
                if(result is not null)
                {
                    translation.ID = result.ID;
                    wasUpdated = true;
                }
            }

            transCollecation.Upsert(translation);
            return wasUpdated;
        }

        public void ResetDatabase()
        {
            _db.ResetDatabase();
        }

        
    }
}
