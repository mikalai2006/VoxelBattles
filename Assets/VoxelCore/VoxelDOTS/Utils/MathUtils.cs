using System.Security.Cryptography;
using System.Text;

public static class MathUtils
{
    // Алфавит без похожих друг на друга символов (0, O, 1, I, L)
    private const string Alphabet = "23456789ABCDEFGHJKLMNPQRSTUVWXYZ";

    public static string GenerateLobbyCode(int length = 6)
    {
        StringBuilder result = new StringBuilder(length);
        byte[] randomByte = new byte[1];

        using (var rng = RandomNumberGenerator.Create())
        {
            while (result.Length < length)
            {
                rng.GetBytes(randomByte);
                int randomIndex = randomByte[0] % Alphabet.Length;

                // Избавляемся от легкого смещения распределения (для идеальной случайности)
                if (randomByte[0] < 256 - (256 % Alphabet.Length))
                {
                    result.Append(Alphabet[randomIndex]);
                }
            }
        }
        return result.ToString();
    }
}
