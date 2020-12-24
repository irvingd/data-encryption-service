using System.Collections.Generic;

namespace DataEncryptionService.Storage
{
    public static class Extensions
    {
        static public void AddEncryptedValues(this List<EncryptedDataItem> list, IEnumerable<EncryptedValue> values)
        {
            foreach (var item in values)
            {
                list.Add(new EncryptedDataItem()
                {
                    Name = item.Name,
                    Cipher = item.Cipher,
                    Hash = item.Hash
                });
            }
        }
    }
}
