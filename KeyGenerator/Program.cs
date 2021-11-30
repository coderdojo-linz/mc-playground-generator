using System.Security.Cryptography;

var rsa = RSA.Create();
var exportedPrivateKey = rsa.ExportRSAPrivateKey()!;
// var exportedPublicKey = rsa.ExportRSAPublicKey()!;

var parameters = rsa.ExportParameters(false);

Console.WriteLine($"\"SignPrivateKey\": \"{Convert.ToBase64String(exportedPrivateKey)}\"");
Console.WriteLine($"\"SignParamsModulus\": \"{Convert.ToBase64String(parameters.Modulus!)}\"");
Console.WriteLine($"\"SignParamsExponent\": \"{Convert.ToBase64String(parameters.Exponent!)}\"");
