namespace DataEncryptionService
{
    public enum HashMethod
    {
        None    = 0,
        //MD5     = 1,
        HMAC256 = 10,
        HMAC384 = 20,
        HMAC512 = 30,
        SHA2_256 = 110,
        SHA2_384 = 120,
        SHA2_512 = 130,
    }
}