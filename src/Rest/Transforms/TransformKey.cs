
namespace BlackDigital.AspNet.Rest.Transforms
{
    /// <summary>
    /// Identifies a transform rule by its key, version and direction.
    /// Used as the dictionary key for registered transform rules.
    /// </summary>
    public readonly struct TransformKey : IEquatable<TransformKey>
    {
        /// <summary>
        /// Gets the direction (input/output) this key applies to.
        /// </summary>
        public TransformDirection Direction { get; }

        /// <summary>
        /// Gets the logical key that groups related transform rules
        /// (typically the action or endpoint identifier).
        /// </summary>
        public string Key { get; }

        /// <summary>
        /// Gets the API version associated with this key.
        /// </summary>
        public string Version { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TransformKey"/> struct.
        /// </summary>
        /// <param name="key">The logical key for the transform.</param>
        /// <param name="version">The API version associated with the key.</param>
        /// <param name="direction">The direction the transform applies to.</param>
        public TransformKey(string key, string version, TransformDirection direction)
        {
            Direction = direction;
            Version = version;
            Key = key;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TransformKey"/> struct
        /// with the default <see cref="TransformDirection.Input"/> direction.
        /// </summary>
        /// <param name="key">The logical key for the transform.</param>
        /// <param name="version">The API version associated with the key.</param>
        public TransformKey(string key, string version)
        {
            Direction = TransformDirection.Input;
            Version = version;
            Key = key;
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>A hash code combining direction, key and version.</returns>
        public override int GetHashCode()
            => HashCode.Combine(Direction, Key, Version);

        /// <summary>
        /// Determines whether the specified object is equal to this instance.
        /// </summary>
        /// <param name="obj">The object to compare with this instance.</param>
        /// <returns><c>true</c> if the object is a <see cref="TransformKey"/> equal to this instance; otherwise, <c>false</c>.</returns>
        public override bool Equals(object? obj)
            => obj is TransformKey other && Equals(other);

        /// <summary>
        /// Determines whether another <see cref="TransformKey"/> is equal to this instance.
        /// </summary>
        /// <param name="other">The key to compare with this instance.</param>
        /// <returns><c>true</c> if both keys have the same direction, key and version; otherwise, <c>false</c>.</returns>
        public bool Equals(TransformKey other)
            => Direction == other.Direction
            && Key == other.Key
            && Version == other.Version;

        /// <summary>
        /// Determines whether two <see cref="TransformKey"/> instances are equal.
        /// </summary>
        /// <param name="left">The left operand.</param>
        /// <param name="right">The right operand.</param>
        /// <returns><c>true</c> if both keys are equal; otherwise, <c>false</c>.</returns>
        public static bool operator ==(TransformKey left, TransformKey right)
            => left.Equals(right);

        /// <summary>
        /// Determines whether two <see cref="TransformKey"/> instances are not equal.
        /// </summary>
        /// <param name="left">The left operand.</param>
        /// <param name="right">The right operand.</param>
        /// <returns><c>true</c> if the keys differ; otherwise, <c>false</c>.</returns>
        public static bool operator !=(TransformKey left, TransformKey right)
            => !left.Equals(right);

        /// <summary>
        /// Returns a string representation of this key in the format
        /// <c>{Key}:{Version}:{Direction}</c>.
        /// </summary>
        /// <returns>A string that represents this key.</returns>
        public override string ToString()
            => $"{Key}:{Version}:{Direction}";
    }
}
