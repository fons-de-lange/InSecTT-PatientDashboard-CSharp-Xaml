////////////////////////////////////////////////////////////////////////////////////////////////////
// file:	DESFire_helper.cs
//
// summary:	Implements a helper class for SmartCardAPI using SDK DESFire EV1 card module
// 
//          Copyright 2012-2020 CardWerk Technologies
//          
////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Diagnostics;
using Subsembly.SmartCard;
using SmartCardAPI.CardModule.DESFIRE;

namespace DESFireModuleTester.console
{
    /// <summary>   A desfire test application. </summary>
    public static class DESFIRE_TEST_APPLICATION
    {
        /// <summary>   The factory default key. </summary>
        public static byte[] FACTORY_DEFAULT_KEY = new byte[16];
        /// <summary>   The aid. </summary>
        public static int AID = 0x010203;
        /// <summary>   Identifier for the file. </summary>
        public static int FILE_ID = 1;
        /// <summary>   Number of keys. </summary>
        public static int NUMBER_OF_KEYS = 3;
        /// <summary>   The key 0. </summary>
        public static byte[] KEY_0 = CardHex.ToByteArray("000102030405060708090A0B0C0D0E0F");
        /// <summary>   The first key. </summary>
        public static byte[] KEY_1 = CardHex.ToByteArray("0102030405060708090A0B0C0D0E0F00");
        /// <summary>   The second key. </summary>
        public static byte[] KEY_2 = CardHex.ToByteArray("02030405060708090A0B0C0D0E0F0001");
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>
    /// Helper class to initialize a test card application area we can use to test DESFire methods.
    /// </summary>
    ////////////////////////////////////////////////////////////////////////////////////////////////////

    public static class HelloDESFire_helper
    {
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Sets host-side test key values. </summary>
        ///
        /// <param name="desfireCard">  DESFire card object</param>
        /// <param name="keyNumber">    The key number. </param>
        /// <param name="key">          The key value. </param>
        ///
        /// <returns>   True if it succeeds, false if it fails. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public static bool SetTestKey(DESFireCardEdge desfireCard, int keyNumber, byte[] key)
        {
            if(key!=null)
            {
                if(key.Length != 16 || //AES and 2KTDES
                   keyNumber > 8)      // we work with up to 8 test keys for now
                {
                    return false;
                }
            }
            return true;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Creates test application. </summary>
        ///
        /// <param name="desfireCard">  . </param>
        /// <param name="accessRights"> The access rights. </param>
        /// <param name="cryptoMode">   (Optional) The crypto mode. </param>
        ///
        /// <returns>   True if it succeeds, false if it fails. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public static bool CreateTestApplication(
            DESFireCardEdge desfireCard, 
            DESFireCardAccessRights accessRights,
            byte cryptoMode = DESFire.CRM_3DES_DF4)
        {
            if (!desfireCard.SelectApplication(0))
            {
                Debug.WriteLine("Can't select DESFire card root application");
                return false;
            }
            Debug.WriteLine("OK-SELECT card root");
            if (!desfireCard.Authenticate(0, DESFIRE_TEST_APPLICATION.FACTORY_DEFAULT_KEY)) // requires DES root key!
            {
               Debug.WriteLine("Can't authenticate under root key");
               return false;
            }
            Debug.WriteLine("OK-AUTHENTICATE under card root key");


            byte test_applicationMasterKeySettings =
               DESFireCardKeySettings.CONFIGURATION_CHANGEABLE |
               DESFireCardKeySettings.FREE_DIRECTORY_LIST_ACCESS_WITHOUT_MASTER_KEY |
               DESFireCardKeySettings.APP_KEY_REQUIRED_TO_CHANGE_ITSELF | 
               DESFireCardKeySettings.MASTER_KEY_CHANGEABLE;

            
            byte[] isoDfName = new byte[0];
            DESFireCardKeySettings appKeySettings = new DESFireCardKeySettings();
            appKeySettings.SetMasterKeySettings(test_applicationMasterKeySettings);
            appKeySettings.SetCryptoMethod(cryptoMode);
            appKeySettings.SetNumberOfApplicationKeys(DESFIRE_TEST_APPLICATION.NUMBER_OF_KEYS);

            if (!desfireCard.CreateApplication(DESFIRE_TEST_APPLICATION.AID, appKeySettings))
            {
                Debug.WriteLine("Can't create test application");
                return false;
            }
            Console.WriteLine("OK-CREATE test application");

            if (!desfireCard.SelectApplication(DESFIRE_TEST_APPLICATION.AID))
            {
                Debug.WriteLine("Can't select test application");
                return false;
            }
            Debug.WriteLine("OK-SELECT test application");


            if (!_authenticate(desfireCard, cryptoMode, 0, DESFIRE_TEST_APPLICATION.FACTORY_DEFAULT_KEY))
            {
                Debug.WriteLine("ERROR: authentication failed");
                return false;
            }
            Debug.WriteLine("OK-AUTHENTICATE under application master key");



            DESFireCardFileSettings fileSettings = new DESFireCardFileSettings(DESFireFileType.STANDARD_DATA, DESFire.COMM_MODE_PLAIN, accessRights, 256);
            if (!desfireCard.CreateFile(DESFIRE_TEST_APPLICATION.FILE_ID, fileSettings))
            {
                Debug.WriteLine("Can't create test file 1");
                return false;
            }
            Console.WriteLine("OK-CREATE test file within test application");

            int[] fileIds = desfireCard.GetFileIDs();


            if (!_authenticate(desfireCard, cryptoMode, 1, DESFIRE_TEST_APPLICATION.FACTORY_DEFAULT_KEY))
            {
                Debug.WriteLine("ERROR: authentication failed");
                return false;
            }
            Debug.WriteLine("OK-AUTHENTICATE under application key 1");
            if (!desfireCard.ChangeKey(cryptoMode, 1, DESFIRE_TEST_APPLICATION.FACTORY_DEFAULT_KEY, DESFIRE_TEST_APPLICATION.KEY_1))
            {
                Debug.WriteLine("Can't change key 1");
                return false;
            }

            if (!_authenticate(desfireCard, cryptoMode, 2, DESFIRE_TEST_APPLICATION.FACTORY_DEFAULT_KEY))
            {
                Debug.WriteLine("ERROR: authentication failed");
                return false;
            }
            Debug.WriteLine("OK-AUTHENTICATE under application key 2");
            if (!desfireCard.ChangeKey(cryptoMode, 2, DESFIRE_TEST_APPLICATION.FACTORY_DEFAULT_KEY, DESFIRE_TEST_APPLICATION.KEY_2))
            {
                Debug.WriteLine("Can't change key 2");
                return false;
            }
            Console.WriteLine("OK-CHANGE test keys");



            // select AID to cancel any active authentication  traces
            if (!desfireCard.SelectApplication(DESFIRE_TEST_APPLICATION.AID))
            {
                Debug.WriteLine("Can't select DESFire card root application");
                return false;
            }
            return true;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Authenticates. </summary>
        ///
        /// <param name="desfireCard">  . </param>
        /// <param name="cryptoMode">   The crypto mode. </param>
        /// <param name="keyNumber">    The key number. </param>
        /// <param name="keyValue">     The key value. </param>
        ///
        /// <returns>   True if it succeeds, false if it fails. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        static bool _authenticate(DESFireCardEdge desfireCard, byte cryptoMode, int keyNumber, byte[] keyValue)
        {
            // authenticate under application change key according to application settings
            if (cryptoMode == DESFire.CRM_AES)
            {
                if (!desfireCard.AuthenticateAES(keyNumber, keyValue))
                {
                    Debug.WriteLine("Can't authenticate under master application key");
                    return false;
                }
            }
            else if (!desfireCard.Authenticate(keyNumber, keyValue))
            {
                Debug.WriteLine("Can't authenticate under master application key");
                return false;
            }
            Debug.WriteLine("OK-AUTHENTICATE");
            return true;
           
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Formats a DESFire card. Note though, that this TEST method only works if the card is still
        /// protected under default factory key.
        /// </summary>
        ///
        /// <param name="desfireCard">  . </param>
        ///
        /// <returns>   True if it succeeds, false if it fails. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public static bool FormatCard(DESFireCardEdge desfireCard)
        {
            if (!desfireCard.SelectApplication(0))
            {
                Debug.WriteLine("Can't select DESFire card root application");
                return false;
            }
            Debug.WriteLine("OK-SELECT root");
            if (!desfireCard.Authenticate(0, DESFIRE_TEST_APPLICATION.FACTORY_DEFAULT_KEY))
            {
               Debug.WriteLine("Can't authenticate under root key");
               return false;
            }
            Debug.WriteLine("OK-AUTHENTICATE under root key");
            
            if (!desfireCard.FormatPICC())
            {
                Debug.WriteLine("Can't format DESFire card");
                return false;
            }
            if (!desfireCard.SelectApplication(0)) // this also resets any host-side crypto 
            {
                Debug.WriteLine("Can't select DESFire card root application");
                return false;
            }
            Debug.WriteLine("OK-FORMAT DESFire card");
            return true;
        }
    }
}
