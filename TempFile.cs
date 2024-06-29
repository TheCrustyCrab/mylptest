namespace LPApi
{
    public class TempFile : IDisposable
    {
        public string Name { get; }

        public TempFile(string data, string ext)
        {
            Name = $"{Guid.NewGuid()}.{ext}";
            File.WriteAllText(Name, data);
        }

        public void Dispose()
        {
            File.Delete(Name);
        }
    }
}
