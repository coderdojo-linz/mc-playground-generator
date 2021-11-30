using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;

namespace GeneratorFunction;

public class PinManager
{
    private readonly IConfiguration configuration;

    public PinManager(IConfiguration configuration)
    {
        this.configuration = configuration;
    }

    public string CreatePin(DateTime notBefore, TimeSpan validPeriod)
    {
        var payload = new byte[sizeof(long) * 2];
        BitConverter.GetBytes(notBefore.Ticks).CopyTo(payload, 0);
        BitConverter.GetBytes(validPeriod.Ticks).CopyTo(payload, sizeof(long));

        using var hasher = SHA256.Create();
        var payloadHash = hasher.ComputeHash(payload);

        using var rsa = RSA.Create();
        rsa.ImportRSAPrivateKey(Convert.FromBase64String(configuration["SignPrivateKey"]), out var _);
        var rsaFormatter = new RSAPKCS1SignatureFormatter(rsa);
        rsaFormatter.SetHashAlgorithm("SHA256");
        var payloadSignature = rsaFormatter.CreateSignature(payloadHash);

        var pin = new byte[payload.Length + payloadSignature.Length];
        payload.CopyTo(pin, 0);
        payloadSignature.CopyTo(pin, payload.Length);

        return Convert.ToBase64String(pin);
    }

    public Pin? ParsePin(string pin)
    {
        var pinBytes = Convert.FromBase64String(pin);
        var notBefore = new DateTime(BitConverter.ToInt64(pinBytes, 0), DateTimeKind.Utc);
        var validPeriod = new TimeSpan(BitConverter.ToInt64(pinBytes, sizeof(long)));

        using var hasher = SHA256.Create();
        var payloadHash = hasher.ComputeHash(pinBytes[..(sizeof(long) * 2)]);

        var rsaKeyInfo = new RSAParameters
        {
            Modulus = Convert.FromBase64String(configuration["SignParamsModulus"]),
            Exponent = Convert.FromBase64String(configuration["SignParamsExponent"]),
        };
        using var rsa = RSA.Create();
        rsa.ImportParameters(rsaKeyInfo);
        var rsaFormatter = new RSAPKCS1SignatureDeformatter(rsa);
        rsaFormatter.SetHashAlgorithm("SHA256");
        if (!rsaFormatter.VerifySignature(payloadHash, pinBytes[(sizeof(long) * 2)..]))
        {
            return null;
        }

        return new(notBefore, validPeriod);
    }

    public record struct Pin(
        DateTime NotBefore,
        TimeSpan ValidPeriod);
}

