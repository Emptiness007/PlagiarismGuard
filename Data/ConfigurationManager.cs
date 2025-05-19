using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace PlagiarismGuard.Data
{
    public static class ConfigurationManager
    {
        private static readonly string ConfigFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "dbconfig.bin");

        public static void SaveConnectionString(string connectionString)
        {
            try
            {
                byte[] encryptedData = ProtectedData.Protect(
                    Encoding.UTF8.GetBytes(connectionString),
                    null,
                    DataProtectionScope.CurrentUser
                );
                File.WriteAllBytes(ConfigFilePath, encryptedData);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Не удалось сохранить конфигурацию базы данных.", ex);
            }
        }

        public static string LoadConnectionString()
        {
            try
            {
                if (!File.Exists(ConfigFilePath))
                {
                    return null;
                }
                byte[] encryptedData = File.ReadAllBytes(ConfigFilePath);
                byte[] decryptedData = ProtectedData.Unprotect(
                    encryptedData,
                    null,
                    DataProtectionScope.CurrentUser
                );
                return Encoding.UTF8.GetString(decryptedData);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Не удалось загрузить конфигурацию базы данных.", ex);
            }
        }
    }
}
