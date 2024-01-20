namespace DeepLBatch
{
    internal interface IDbRepository
    {
        Translation Get(string indexKey);

        public void Add(Translation translation);

        public void ResetDatabase();

        public bool UpsertByKey(Translation translation);
    }
}