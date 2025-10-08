
namespace BlackDigital.Mvc.Rest.Trasnforms
{
    public readonly struct TransformKey : IEquatable<TransformKey>
    {
        public TransformDirection Direction { get; }

        public string Key { get; }

        public string Version { get; }

        public TransformKey(string key, string version, TransformDirection direction)
        {
            Direction = direction;
            Version = version;
            Key = key;
        }

        public TransformKey(string key, string version)
        {
            Direction = TransformDirection.Input;
            Version = version;
            Key = key;
        }

        public override int GetHashCode()
            => HashCode.Combine(Direction, Key, Version);

        public override bool Equals(object? obj)
            => obj is TransformKey other && Equals(other);

        public bool Equals(TransformKey other)
            => Direction == other.Direction
            && Key == other.Key
            && Version == other.Version;

        public static bool operator ==(TransformKey left, TransformKey right)
            => left.Equals(right);

        public static bool operator !=(TransformKey left, TransformKey right)
            => !left.Equals(right);

        public override string ToString()
            => $"{Key}:{Version}:{Direction}";
    }
}
