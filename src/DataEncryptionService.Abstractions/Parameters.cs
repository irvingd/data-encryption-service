using System;
using System.Collections.Generic;

namespace DataEncryptionService
{
    public static class Parameters
    {
        public static TValue GetValue<TValue>(Dictionary<string, object> kvPairs, string keyName, TValue defaultValue)
        {
            if (null != kvPairs && kvPairs.TryGetValue(keyName, out object objValue))
            {
                return (TValue)Convert.ChangeType(objValue, typeof(TValue));
            }

            return defaultValue;
        }

        //static public void Validate(Guid? guidParam, string paramName, bool valueRequired = true)
        //{
        //    // TODO:
        //}

        //static public void Validate(DateTime? dateTimeParam, string paramName, bool valueRequired = false, bool utcRequired = true)
        //{
        //    if (valueRequired && !dateTimeParam.HasValue)
        //    {
        //        throw new ArgumentException("A date-time value is required for this parameter.", paramName);
        //    }

        //    if (dateTimeParam.HasValue && utcRequired && (dateTimeParam.Value.Kind != DateTimeKind.Utc))
        //    {
        //        throw new ArgumentException("The date time value needs to be specificied in UTC.", paramName);
        //    }
        //}
    }
}