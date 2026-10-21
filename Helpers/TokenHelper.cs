using System.Security.Cryptography;
using System.Text;

namespace EmployeeSelfServiceAPI.Helpers;

public static class TokenHelper
{
    public static string GenerateToken() =>
        Convert.ToHexString(RandomNumberGenerator.GetBytes(32));

    public static string Hash(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}