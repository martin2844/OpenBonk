using System;
using System.IO;
using System.Security.Cryptography;

public static class SimpleEncryptor
{
	private static readonly byte[] Key = { 0xd9, 0x40, 0x84, 0x0d, 0x5a, 0xe7, 0xc7, 0x90, 0x7b, 0x09, 0x24, 0x37, 0xbc, 0x0c, 0x5b, 0x44, 0xaa, 0xf7, 0x0e, 0x27, 0x3e, 0x12, 0xd0, 0xfb, 0x4d, 0xa2, 0xb8, 0xc7, 0x67, 0xcc, 0x91, 0x1d };
	private static readonly byte[] IV = { 0x37, 0x86, 0x4e, 0xf1, 0x5c, 0x24, 0xbc, 0x0a, 0xcb, 0xc6, 0x0e, 0x39, 0x78, 0xef, 0x1f, 0x06 };

	public static string Encrypt(string plainText)
	{
		using (var aes = Aes.Create())
		{
			aes.Key = Key;
			aes.IV = IV;
			using (var encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
			using (var memory = new MemoryStream())
			using (var crypto = new CryptoStream(memory, encryptor, CryptoStreamMode.Write))
			using (var writer = new StreamWriter(crypto))
			{
				writer.Write(plainText);
				writer.Flush();
				crypto.FlushFinalBlock();
				return Convert.ToBase64String(memory.ToArray());
			}
		}
	}

	public static string Decrypt(string encryptedText)
	{
		byte[] bytes = Convert.FromBase64String(encryptedText);
		using (var aes = Aes.Create())
		{
			aes.Key = Key;
			aes.IV = IV;
			using (var decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
			using (var memory = new MemoryStream(bytes))
			using (var crypto = new CryptoStream(memory, decryptor, CryptoStreamMode.Read))
			using (var reader = new StreamReader(crypto))
				return reader.ReadToEnd();
		}
	}
}
