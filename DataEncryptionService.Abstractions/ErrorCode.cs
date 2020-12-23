namespace DataEncryptionService
{
    public enum ErrorCode
    {
        None                                        = 0,

        Generic_Undefined_Error                     = 100,
        Generic_Missing_Configuration               = 101,
        Generic_Invalid_Configuration               = 102,

        Network_Undefined_Error                     = 1000,
        Network_Connection_Error                    = 1001,         

        Crypto_Undefined_Error                      = 2000,
        Crypto_Missing_Default_Encryption_Key       = 2001,
        Crypto_Encryption_Key_Not_Found             = 2002,
        Crypto_Encryption_Context_Not_Set           = 2003,
        Crypto_Access_Denied_To_Key_or_Service      = 2004,
        Crypto_Missing_Configuration                = 2005,
        Crypto_Invalid_Configuration                = 2006,
        Crypto_Service_Not_Available                = 2007,
        Crypto_Invalid_Service_Parameters           = 2008,
        Crypto_Service_Not_Found                    = 2009,
        Crypto_Invalid_Tagged_Data_List             = 2010,
        Crypto_Engine_Not_Available                 = 2011,
        Crypto_Functionality_Not_Supported          = 2012,
        Crypto_Encryption_Key_Not_Specified         = 2013,
        Crypto_Encryption_Context_Not_Specified     = 2014,

        Storage_Undefined_Error                     = 3000,
        Storage_Cannot_Commit_Values                = 3001,
        Storage_Labeled_Values_Failed_To_Load       = 3002,
        Storage_Labeled_Values_Not_Found            = 3003,
        Storage_Label_Not_Found_Cannot_Delete        = 3004,
        Storage_Error_Deleting_Data                 = 3005,
    }
}
