using BlackDigital.Domain.Security;
using Konscious.Security.Cryptography;
using System.Security.Cryptography;
using System.Text;

namespace BlackDigital.AspNet.Security
{
    /// <summary>
    /// Argon2id implementation of <see cref="IPasswordHashService"/>.
    /// Produces self-contained hash strings in the PHC string format that
    /// embed the salt and parameters used for hashing.
    /// </summary>
    public class Argon2idPasswordHashService : IPasswordHashService
    {
        private const int Argon2Version = 19; // v0x13

        private readonly int _memorySize;
        private readonly int _iterations;
        private readonly int _parallelism;
        private readonly int _saltLength;
        private readonly int _hashLength;

        /// <summary>
        /// Initializes a new instance of the <see cref="Argon2idPasswordHashService"/> class
        /// with the specified Argon2id parameters.
        /// </summary>
        /// <param name="memorySize">The memory size in KB. Must be at least 8.</param>
        /// <param name="iterations">The number of iterations. Must be at least 1.</param>
        /// <param name="parallelism">The degree of parallelism. Must be at least 1.</param>
        /// <param name="saltLength">The salt length in bytes. Must be at least 8.</param>
        /// <param name="hashLength">The hash length in bytes. Must be at least 4.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when any parameter is below its minimum value.</exception>
        public Argon2idPasswordHashService(
            int memorySize = 65536,
            int iterations = 3,
            int parallelism = 1,
            int saltLength = 16,
            int hashLength = 32)
        {
            if (memorySize < 8)
                throw new ArgumentOutOfRangeException(nameof(memorySize), "Memory size must be at least 8 KB.");

            if (iterations < 1)
                throw new ArgumentOutOfRangeException(nameof(iterations), "Iterations must be at least 1.");

            if (parallelism < 1)
                throw new ArgumentOutOfRangeException(nameof(parallelism), "Parallelism must be at least 1.");

            if (saltLength < 8)
                throw new ArgumentOutOfRangeException(nameof(saltLength), "Salt length must be at least 8 bytes.");

            if (hashLength < 4)
                throw new ArgumentOutOfRangeException(nameof(hashLength), "Hash length must be at least 4 bytes.");

            _memorySize = memorySize;
            _iterations = iterations;
            _parallelism = parallelism;
            _saltLength = saltLength;
            _hashLength = hashLength;
        }

        /// <summary>
        /// Hashes the specified password using a randomly generated salt and
        /// returns a self-contained Argon2id hash string.
        /// </summary>
        /// <param name="password">The password to hash.</param>
        /// <returns>A hash string in PHC format containing the parameters, salt and hash.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="password"/> is null or empty.</exception>
        public string HashPassword(string password)
        {
            if (string.IsNullOrEmpty(password))
                throw new ArgumentException("Password cannot be null or empty.", nameof(password));

            byte[] salt = RandomNumberGenerator.GetBytes(_saltLength);
            byte[] hash = ComputeHash(password, salt, _memorySize, _iterations, _parallelism, _hashLength);

            return $"$argon2id$v={Argon2Version}$m={_memorySize},t={_iterations},p={_parallelism}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
        }

        /// <summary>
        /// Verifies a password against a previously generated Argon2id hash string
        /// using a constant-time comparison.
        /// </summary>
        /// <param name="password">The password to verify.</param>
        /// <param name="hashedPassword">The hash string to verify against.</param>
        /// <returns><c>true</c> when the password matches the hash; otherwise, <c>false</c>.</returns>
        public bool VerifyPassword(string password, string hashedPassword)
        {
            if (string.IsNullOrEmpty(password))
                return false;

            if (string.IsNullOrEmpty(hashedPassword))
                return false;

            var parts = hashedPassword.Split('$', StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length != 5 || parts[0] != "argon2id")
                return false;

            try
            {
                if (!parts[1].StartsWith("v="))
                    return false;

                if (!int.TryParse(parts[1].AsSpan(2), out int version) || version != Argon2Version)
                    return false;

                var parameters = parts[2].Split(',');
                if (parameters.Length != 3)
                    return false;

                int memorySize = 0;
                int iterations = 0;
                int parallelism = 0;

                foreach (var parameter in parameters)
                {
                    if (parameter.StartsWith("m="))
                        memorySize = int.Parse(parameter.AsSpan(2));
                    else if (parameter.StartsWith("t="))
                        iterations = int.Parse(parameter.AsSpan(2));
                    else if (parameter.StartsWith("p="))
                        parallelism = int.Parse(parameter.AsSpan(2));
                    else
                        return false;
                }

                if (memorySize <= 0 || iterations <= 0 || parallelism <= 0)
                    return false;

                byte[] salt = Convert.FromBase64String(parts[3]);
                byte[] expectedHash = Convert.FromBase64String(parts[4]);

                if (salt.Length < 8 || expectedHash.Length < 4)
                    return false;

                byte[] actualHash = ComputeHash(password, salt, memorySize, iterations, parallelism, expectedHash.Length);

                return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
            }
            catch
            {
                return false;
            }
        }

        private static byte[] ComputeHash(
            string password,
            byte[] salt,
            int memorySize,
            int iterations,
            int parallelism,
            int hashLength)
        {
            byte[] passwordBytes = Encoding.UTF8.GetBytes(password);

            using var argon2 = new Argon2id(passwordBytes)
            {
                Salt = salt,
                MemorySize = memorySize,
                Iterations = iterations,
                DegreeOfParallelism = parallelism
            };

            return argon2.GetBytes(hashLength);
        }
    }
}
