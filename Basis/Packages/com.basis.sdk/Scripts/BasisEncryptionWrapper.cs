using UnityEngine;
using System.IO;
using System.Security.Cryptography;
using System;
using System.Threading.Tasks;
using static BasisProgressReport;

public static class BasisEncryptionWrapper
{
    private const int SaltSize = 16; // Size of the salt in bytes
    private const int KeySize = 32; // Size of the key in bytes (256 bits)
    private const int IvSize = 16; // Size of the IV in bytes (128 bits)
    public static async Task<byte[]> EncryptDataAsync(string UniqueID,byte[] dataToEncrypt, BasisPassword RandomizedPassword, BasisProgressReport reportProgress = null)
    {
        reportProgress.ReportProgress(UniqueID, 0f, "Encrypting Data");
        var encryptedData = await Task.Run(async () => await Encrypt(UniqueID, RandomizedPassword, dataToEncrypt, reportProgress)); // Run encryption on a separate thread
        reportProgress.ReportProgress(UniqueID, 100f, "Encrypting Data");
        return encryptedData;
    }

    public static async Task<byte[]> DecryptDataAsync(string UniqueID, byte[] dataToDecrypt, BasisPassword Randomizedpassword, BasisProgressReport reportProgress = null)
    {
        reportProgress.ReportProgress(UniqueID, 0f, "Decrypting Data");
        var decryptedData = await Task.Run(async () => await Decrypt(UniqueID, Randomizedpassword.VP, dataToDecrypt, reportProgress)); // Run decryption on a separate thread
        reportProgress.ReportProgress(UniqueID, 100f, "Decrypting Data");
        return decryptedData.Item1;
    }

    private static async Task<byte[]> Encrypt(string UniqueID, BasisPassword password, byte[] dataToEncrypt, BasisProgressReport reportProgress = null)
    {
        byte[] salt = new byte[SaltSize];
        using (var rng = new RNGCryptoServiceProvider())
        {
            rng.GetBytes(salt); // Fill the salt with random bytes
        }

        reportProgress.ReportProgress(UniqueID, 10f, "Encrypting Data");

        using (var key = new Rfc2898DeriveBytes(password.VP, salt, 10000))
        {
            var keyBytes = key.GetBytes(KeySize);
            var iv = new byte[IvSize];
            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(iv); // Generate a random IV
            }

            reportProgress.ReportProgress(UniqueID, 20f, "Encrypting Data");

            using (var aes = Aes.Create())
            {
                aes.Key = keyBytes;
                aes.IV = iv;

                using (var msEncrypt = new MemoryStream())
                {
                    // Write the salt and IV to the memory stream
                  await  msEncrypt.WriteAsync(salt, 0, salt.Length);
                  await  msEncrypt.WriteAsync(iv, 0, iv.Length);

                    using (var cryptoStream = new CryptoStream(msEncrypt, aes.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                      await  cryptoStream.WriteAsync(dataToEncrypt, 0, dataToEncrypt.Length);
                    }

                    reportProgress.ReportProgress(UniqueID, 90f, "Encrypting Data");

                    // Get the encrypted data from the memory stream
                    return msEncrypt.ToArray();
                }
            }
        }
    }

    private static async Task<(byte[], byte[], byte[])> Decrypt(string UniqueID, string RandomizedString, byte[] dataToDecrypt, BasisProgressReport reportProgress = null)
    {
        if (dataToDecrypt == null || dataToDecrypt.Length == 0)
        {
            Debug.LogError("Missing Data To Decrypt");
            return new(null, null, null);
        }

        reportProgress.ReportProgress(UniqueID,10f, "Decrypting Data");

        using (var msDecrypt = new MemoryStream(dataToDecrypt))
        {
            // Read the salt and IV from the memory stream
            byte[] salt = new byte[SaltSize];
           await msDecrypt.ReadAsync(salt, 0, SaltSize);

            byte[] iv = new byte[IvSize];
           await msDecrypt.ReadAsync(iv, 0, IvSize);

            reportProgress.ReportProgress(UniqueID, 20f, "Decrypting Data");

            // Generate the key using the password and salt
            using (var key = new Rfc2898DeriveBytes(RandomizedString, salt, 10000))
            {
                var keyBytes = key.GetBytes(KeySize);

                using (var aes = Aes.Create())
                {
                    aes.Key = keyBytes;
                    aes.IV = iv;

                    // Use a CryptoStream to decrypt the remaining data
                    using (var cryptoStream = new CryptoStream(msDecrypt, aes.CreateDecryptor(), CryptoStreamMode.Read))
                    {
                        using (var msOutput = new MemoryStream())
                        {
                            cryptoStream.CopyTo(msOutput);
                            byte[] output = msOutput.ToArray();
                            reportProgress.ReportProgress(UniqueID, 90f, "Decrypting Data");

                            return (output, salt, iv);
                        }
                    }
                }
            }
        }
    }

    public static async Task ReadFileAsync(string UniqueID, string filePath, Func<byte[], Task> processChunk, BasisProgressReport reportProgress = null, int bufferSize = 4194304)
    {
        reportProgress.ReportProgress(UniqueID, 0f, "Reading Data");
        var fileSize = new FileInfo(filePath).Length;
        var buffer = new byte[bufferSize];
        long totalRead = 0;

        using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
        {
            int bytesRead;
            while ((bytesRead = await fs.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                totalRead += bytesRead;
                await processChunk(buffer[..bytesRead]);
                reportProgress.ReportProgress(UniqueID, (float)totalRead / fileSize * 100f, "Reading Data");
            }
        }
        reportProgress.ReportProgress(UniqueID, 100f, "Reading Data");
    }

    public static async Task WriteFileAsync(string UniqueID,string filePath, byte[] data, FileMode fileMode, BasisProgressReport reportProgress = null, int bufferSize = 4194304)
    {
        reportProgress.ReportProgress(UniqueID, 0f, "Writing Data");
        long totalWritten = 0;

        using (var fs = new FileStream(filePath, fileMode, FileAccess.Write, FileShare.None, bufferSize, useAsync: true))
        {
            int offset = 0;
            while (offset < data.Length)
            {
                int bytesToWrite = Math.Min(bufferSize, data.Length - offset);
                await fs.WriteAsync(data, offset, bytesToWrite);
                totalWritten += bytesToWrite;
                offset += bytesToWrite;

                // Report progress periodically
                reportProgress.ReportProgress(UniqueID, (float)totalWritten / data.Length * 100f, "Writing Data");
            }
        }

        // Report 100% completion
        reportProgress.ReportProgress(UniqueID, 100f, "Writing Data");
    }

    public struct BasisPassword
    {
        public string VP;
    }

    public static async Task EncryptFileAsync(string UniqueID, BasisPassword password, string inputFilePath, string outputFilePath, BasisProgressReport reportProgress, int bufferSize = 4194304)
    {
        byte[] dataToEncrypt = await ReadAllBytesAsync(UniqueID, inputFilePath, reportProgress);
        var encryptedData = await EncryptDataAsync(UniqueID,dataToEncrypt, password, reportProgress);
        await WriteFileAsync(UniqueID,outputFilePath, encryptedData, FileMode.Create, reportProgress, bufferSize);
    }

    public static async Task DecryptFileAsync(string UniqueID, BasisPassword password, string inputFilePath, string outputFilePath, BasisProgressReport reportProgress, int bufferSize = 4194304)
    {
        byte[] dataToDecrypt = await ReadAllBytesAsync(UniqueID, inputFilePath, reportProgress);
        if (dataToDecrypt == null || dataToDecrypt.Length == 0)
        {
            throw new Exception("Data requested was null or empty");
        }
        var decryptedData = await DecryptDataAsync(UniqueID, dataToDecrypt, password, reportProgress);
        await WriteFileAsync(UniqueID, outputFilePath, decryptedData, FileMode.Create, reportProgress,bufferSize);
    }

    public static async Task<byte[]> DecryptFileAsync(string UniqueID, BasisPassword password, string inputFilePath, BasisProgressReport reportProgress, int bufferSize = 4194304)
    {
        byte[] dataToDecrypt = await ReadAllBytesAsync(UniqueID, inputFilePath, reportProgress, bufferSize);
        if (dataToDecrypt == null || dataToDecrypt.Length == 0)
        {
            Debug.LogError("Data requested was null or empty");
            return null;
        }
        var decryptedData = await DecryptDataAsync(UniqueID, dataToDecrypt, password, reportProgress);
        return decryptedData;
    }

    private static async Task<byte[]> ReadAllBytesAsync(string UniqueID, string filePath, BasisProgressReport reportProgress, int bufferSize = 4194304) // Default 4MB buffer size
    {
        reportProgress.ReportProgress(UniqueID, 0f, "reading Data");

        var fileInfo = new FileInfo(filePath);
        byte[] data = new byte[fileInfo.Length];

        using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize, useAsync: true))
        {
            int totalRead = 0;
            int bytesRead;
            byte[] buffer = new byte[bufferSize];

            while ((bytesRead = await fs.ReadAsync(buffer, 0, Math.Min(bufferSize, data.Length - totalRead))) > 0)
            {
                Buffer.BlockCopy(buffer, 0, data, totalRead, bytesRead);
                totalRead += bytesRead;
                reportProgress.ReportProgress(UniqueID, (float)totalRead / fileInfo.Length * 100f, "reading Data");
            }
        }

        reportProgress.ReportProgress(UniqueID, 100f, "reading Data");
        return data;
    }
}
