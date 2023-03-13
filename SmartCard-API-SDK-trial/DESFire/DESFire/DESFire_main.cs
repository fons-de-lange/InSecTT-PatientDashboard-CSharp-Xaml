////////////////////////////////////////////////////////////////////////////////////////////////////
// file:	DESFire_main.cs
// 
// summary:	This sample code for SmartCardAPI SDK using DESFire EV1 card module
//          
//          Copyright 2018-2020 CardWerk Technologies
// 
// HIST:
// 01NOV2019 MJ improve lost/found reader handling
// 11AUG2019 MJ test with latest libraries (including creation of test application)
// 27NOV2018 MJ initial version copied from HelloCard sample code
// 
////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Diagnostics; 
using System.Threading;
using Subsembly.SmartCard;
using SmartCardAPI.CardModule.DESFIRE;

namespace DESFireModuleTester.console
{
    ////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>   HelloDESFire provides you some easy-to-use sample code to access DESFire EV1 cards
    ///             This includes card formatting, creation of an application as well as file creation 
    ///             and setup for access protected under an AES application key </summary>
    /// 
    /// <remarks>   This sample code requires the following assemplies that are part of SmartCardAPI SDK:
    ///             smartcard.dll, CardModule.DESFIRE.dll
    ///             
    ///             Note that NXP DESFire EV1 card specification is available directly from NXP under NDA.
    ///             </remarks>
    /// 
    ////////////////////////////////////////////////////////////////////////////////////////////////////
    public class HelloDESFire_main
    {
        /// <summary>   The card insertions. </summary>
        static int cardInsertions = 0;
        /// <summary>   Information describing the program. </summary>
        static string programInfo = String.Format("DESFire console sample rev.01NOV2019 \n{0}", SMARTCARDAPI.ApiVersionInfo);
        /// <summary>   increases the number of readers we are looking for. </summary>
        static bool ALWAYS_LOOK_FOR_MORE_CARD_READERS = true;

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Main entry-point for this console application. </summary>
        ///
        /// <param name="args"> An array of command-line argument strings. </param>
        ///
        /// <returns>   Exit-code for the process - 0 for success, else an error code. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        // Note: Added event raising when card has been read
        public class CardReadEventArgs: EventArgs
        {
            public string StringReadFromCard;
        }
    
        public event EventHandler<CardReadEventArgs> CardDataReadEvent;
        public event EventHandler<EventArgs> CardRemovedFromReaderEvent;



        public  int StartCardReading()
        {
            Console.WriteLine(programInfo);
           

            // SmartCardAPI provides convenient events to track cards and readers. 
            CardTerminalManager.Singleton.CardInsertedEvent += new CardTerminalEventHandler(CardInsertedEvent);
            CardTerminalManager.Singleton.CardRemovedEvent += new CardTerminalEventHandler(CardRemovedEvent);
            //CardRemovedFromReaderEvent.
            //CardDataReadEvent?.Invoke(this, new CardReadEventArgs { StringReadFromCard = dataReadFromCard });


            CardTerminalManager.Singleton.CardTerminalLostEvent += new CardTerminalEventHandler(TerminalLostEvent);
            CardTerminalManager.Singleton.CardTerminalFoundEvent += new CardTerminalEventHandler(TerminalFoundEvent);

            try
            {
                int readerCountAtStartup = CardTerminalManager.Singleton.Startup(true); // true: auto register PC/SC readers
                DisplayNumberOfAvailableReaders();

                if (readerCountAtStartup > 0)
                {
                    string[] readerList = CardTerminalManager.Singleton.GetSlotNames();
                    int index = 0;
                    foreach (string reader in readerList)
                    {
                        Console.WriteLine(string.Format("Reader #{0}: {1}", index++, reader));
                    }
                    if (ALWAYS_LOOK_FOR_MORE_CARD_READERS)
                    {
                        // By default SlotCountMinimum is set to the number of readers detected at startup.
                        // We increase this value to continue looking for more readers.
                        CardTerminalManager.Singleton.SlotCountMinimum = readerCountAtStartup + 1;
                    }
                }
                else
                {
                    Console.WriteLine("ERROR: No card reader/terminal available on this system!");
                    Console.WriteLine("Please verify your PC/SC smart card system.\n" +
                                      "Is a smart card reader attached?\n" +
                                      "Is your PC/SC smart card service up and running?\n" +
                                      "Is the reader driver installed?\n");
                }
                while (true ) //!Console.KeyAvailable)
                {
                    Console.Write(".");
                    Thread.Sleep(500);
                }

            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.Message);
            }
            finally
            {
                if (CardTerminalManager.Singleton.StartedUp)
                {
                    CardTerminalManager.Singleton.Shutdown();
                }
                Console.WriteLine("END of HelloCard.Console");
            }
            return Environment.ExitCode != 0 ? Environment.ExitCode : 100;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// This method is called when a card is inserted. We try to power it up and, show the ATR.
        /// </summary>
        ///
        /// <exception cref="Exception">    Thrown when an exception error condition occurs. </exception>
        ///
        /// <param name="aSender">      . </param>
        /// <param name="aEventArgs">   . </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        void CardInsertedEvent(object aSender, CardTerminalEventArgs aEventArgs)
        {
            CardTerminalSlot aCardSlot = aEventArgs.Slot;
            CardActivationResult nActivationResult;
            Console.Beep();
            //Console.Clear();
            Console.WriteLine(programInfo);
            Console.WriteLine(String.Format("  Total readers: {0}", CardTerminalManager.Singleton.SlotCount));
            Console.WriteLine(String.Format("Card insertions: {0}", ++cardInsertions));

            string readerName = aCardSlot.CardTerminalName;
            Console.WriteLine("card inserted in " + readerName);

            CardHandle aCard = aCardSlot.AcquireCard((CardTypes.T0 | CardTypes.T1), out nActivationResult); // power up card

            if (nActivationResult != CardActivationResult.Success)
            {
                Debug.Assert(aCard == null);

                switch (nActivationResult)
                {
                    case CardActivationResult.NoCard:
                        Console.WriteLine("Please insert card");
                        break;
                    case CardActivationResult.UnresponsiveCard:
                        Console.WriteLine("Unresponsive card.");
                        break;
                    case CardActivationResult.InUse:
                        Console.WriteLine("Card in use.");
                        break;
                    default:
                        Console.WriteLine("Can't power up card!");
                        break;
                }
                return;
            }
            DisplayReaderProperties(aCardSlot);

            try
            {
                DESFireCardEdge desfireCard = new DESFireCardEdge(aCard);
                if (desfireCard == null)
                {
                    throw new Exception ("Can't instantiate DESFire card object");
                }
                Console.WriteLine("DESFire API version: " + desfireCard.GetApiVersion());
                Console.WriteLine("OK-DESFire card detected.");
                byte[] atr = aCard.GetATR();
                Console.WriteLine(String.Format("Card ATR={0}", CardHex.FromByteArray(atr)));
                Console.WriteLine("UID=" + CardHex.FromByteArray(desfireCard.UID));

                // ================================================================================
                // caution: this will erase all card data
                // Note that you can only format the card if it is still protected under default card key
                // 


                bool formatCard = false;  // NOTE: Now set to false, otherwise Card will be formatted
                if (formatCard)
                {
                    if(!HelloDESFire_helper.FormatCard(desfireCard))
                    {
                        throw new Exception("Can't format card.");
                    }
                    Console.WriteLine("OK-DESFire card formatted.");
                }

                // NOTE: Code below will not be executed due to DESFIRE_TEST_APPLICATION.AID already being selected
                // So no new application will be created.
                if (!desfireCard.SelectApplication(DESFIRE_TEST_APPLICATION.AID))
                {
                    Console.WriteLine("Creating test application");

                    byte cryptoMode = DESFire.CRM_AES;
                    
                    DESFireCardAccessRights accessRights = new DESFireCardAccessRights(DESFireCardAccessRights.ALWAYS, 1, 1, 0); // access rights to read, write, read/write, change
                    if (!HelloDESFire_helper.CreateTestApplication(desfireCard, accessRights, cryptoMode)) 
                    {
                        throw new Exception("Can't initialize (on-card) test application");
                    }
                }
                Console.WriteLine("OK-DESFire test application available for further testing.");

                var yyyy = desfireCard.GetFileIDs();
                var zzzz = desfireCard.GetDFNames();
                var aaaa = desfireCard.GetApplicationIDs();
                byte[] cardData = desfireCard.Read(1, 0, 256); // read 16 bytes at offset 0
                if(cardData==null)
                {
                    throw new Exception("Can't read data from file 1");
                }
                Console.WriteLine("OK-read:  " + CardHex.FromByteArray(cardData));
                
                // New: show data in File 1 as string.
                //cardData = desfireCard.Read(1, 0, 256); // read 256 bytes at offset 0
                var dataReadFromCard = GetStringFromCardData(cardData);
                Console.WriteLine("Data Read from file on Card: \n" + dataReadFromCard);
                CardDataReadEvent?.Invoke(this, new CardReadEventArgs { StringReadFromCard = dataReadFromCard });



                // NOTE: Only write if formatting is TRUE: authenticate and  write data 
                if (formatCard)
                {
                    byte[] dataToWrite = ConvertString2ByteArray(BareMinimumPatient);
                    if (!desfireCard.AuthenticateAES(1, DESFIRE_TEST_APPLICATION.KEY_1))
                    {
                        throw new Exception("Can't authenticate under application key 1");
                    }

                    Console.WriteLine("OK-authenticate under application key 1");

                    if (!desfireCard.Write(1, dataToWrite, 0, dataToWrite.Length)) // write 256 bytes max at offset 0
                    {
                        throw new Exception("Can't write data to file 1");
                    }

                    Console.WriteLine("OK-write new card data");
                }


                // NOTE: Code below was commented out: To check if data written is same as data read back

                //var xxx = desfireCard.GetFileIDs();
                //if (!CardHex.IsEqual(cardData,dataToWrite))
                //{
                //    throw new Exception("Data integrity error - data written different from data read");
                //}
                //Console.WriteLine("OK-validated new card data: " + CardHex.FromByteArray(dataToWrite));

            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR: " + ex.Message);
                CardDataReadEvent?.Invoke(this, new CardReadEventArgs { StringReadFromCard = null });
            }
            finally
            {
                aCard.Disconnect();
                aCard.Dispose();
            }
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>    Adds a value to 'value'. </summary>
        ///
        /// <param name="data">  The data. </param>
        /// <param name="value"> The value. </param>
        ///
        /// <returns>    A byte[]. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        private static string BareMinimumMedStaffMember =
            "{ " +
                "\"Id\": \"2\"," +
                "\"resourceType\" : \"MedStaff\"" +
            "}";
        private static string BareMinimumPatient =
            "{ " +
            "\"Id\": \"3\"," +
            "\"resourceType\" : \"Patient\"" +
            "}";


        static string doctor = 
        "{ " +
            "\"resourceType\" : \"PractitionerRole\" ," +
            "\"code\" : { " +
                "\"coding\": [" +
                    "{" +
                        "\"code\": \"MedStaff\" ," +
                        "\"display\": \"MD\"" + 
                    "}" +
                    //",{" +
                    //    "\"code\": \"MD\" ," +
                    //    "\"display\": \"Cardiovascular Surgeon\"" +
                    //"}" +
                "]" +
                //"\"text\": \"role\"" +
            "}," +

            "\"person\" : { " +
                "\"id\" : \"0\"," +
                //"\"reference\" : \"Practitioner/InSecTT\"" +
                "\"display\" : \"Cardiovascular Surgeon\"" +
                //"\"name\" : \"Floyd Reynolds\"," +
                //"\"title\" : \"Head of the Cardiac Surgical Department\"," +
                //"\"occupation\" : \"Cardiovascular Surgeon\"" +
            "}," +

            "\"organization\" : {" +
                "\"id\" : \"0\"," +
                "\"display\" : \"New Amsterdam Medical Center\"" +

                //"\"department\" : \"Department of Surgery\"" +
               
            "}" +
        "}";

        static string patient =
            "{ " +
            "\"resourceType\" : \"PatientRole\" ," +
            "\"code\" : { " +
            "\"coding\": [" +
            "{" +
            "\"code\": \"Person\" ," +
            "\"display\": \"Patient\"" +
            "}" +
            //",{" +
            //    "\"code\": \"MD\" ," +
            //    "\"display\": \"Cardiovascular Surgeon\"" +
            //"}" +
            "]" +
            //"\"text\": \"role\"" +
            "}," +

            "\"person\" : { " +
            "\"id\" : \"2\"," +
            //"\"reference\" : \"Practitioner/InSecTT\"" +
            "\"display\" : \"Patient\"" +
            //"\"name\" : \"Floyd Reynolds\"," +
            //"\"title\" : \"Head of the Cardiac Surgical Department\"," +
            //"\"occupation\" : \"Cardiovascular Surgeon\"" +
            "}," +



            "\"organization\" : {" +
            "\"id\" : \"0\"," +
            "\"display\" : \"New Amsterdam Medical Center\"" +

            //"\"department\" : \"Department of Surgery\"" +

            "}" +
            "}";


        static byte[] ConvertString2ByteArray(string inputString)
        {
            var charArray = inputString.ToCharArray();
            var byteArray = new byte[charArray.Length];
            for (int i = 0; i < charArray.Length; i++)
                byteArray[i] = (byte)charArray[i];

            return byteArray;
        }

        static byte[] addValue(byte[] data, int value)
        {
            var x = patient.ToCharArray();
            var byteArray = new byte[x.Length];
            for (int i = 0; i < x.Length; i++)
                byteArray[i] = (byte)x[i];

            return byteArray;
            //var z = byteArray.ToString();

            byte[] newData = new byte[data.Length];
            for (int i = 0; i < newData.Length; i++)
            {
                newData[i] = (byte)(data[i] + (byte)value);
            }



            return newData;
        }

        public static string GetStringFromCardData(byte[] cardData)
        {
            char[] newData = new char[cardData.Length];
            for (int i = 0; i < cardData.Length; i++)
                newData[i] = (char)cardData[i];

            var zzz = new string(newData);
            return zzz;
        }


        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Card removed event. </summary>
        ///
        /// <param name="aSender">      . </param>
        /// <param name="aEventArgs">   . </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        void CardRemovedEvent(object aSender, CardTerminalEventArgs aEventArgs)
        {
            CardTerminalSlot aCardSlot = aEventArgs.Slot;
            Console.Beep(1000, 200);
            Console.Beep(1000, 200);
            Console.Write("\ncard removed from " + aCardSlot.CardTerminalName);
            Console.Write("\nplease insert card");

            CardRemovedFromReaderEvent?.Invoke(this, EventArgs.Empty);

        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// This method is called whenever SmartCardAPI indicates a terminal that has been added to the
        /// current tracking list.
        /// </summary>
        ///
        /// <param name="aSender">      . </param>
        /// <param name="aEventArgs">   . </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        static void TerminalFoundEvent(object aSender, CardTerminalEventArgs aEventArgs)
        {

            if (CardTerminalManager.Singleton.StartedUp)
            {
                DisplayNumberOfAvailableReaders();
                Console.WriteLine("Found reader: " + aEventArgs.Slot.CardTerminalName);
                if (ALWAYS_LOOK_FOR_MORE_CARD_READERS && CardTerminalManager.Singleton.SlotCount == CardTerminalManager.Singleton.SlotCountMinimum)
                {
                    CardTerminalManager.Singleton.SlotCountMinimum += 1; // let's continue looking for more readers
                }
            }

        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Terminal lost event. </summary>
        ///
        /// <param name="aSender">      . </param>
        /// <param name="aEventArgs">   . </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        static void TerminalLostEvent(object aSender, CardTerminalEventArgs aEventArgs)
        {
            if (CardTerminalManager.Singleton.StartedUp)
            {
                Console.WriteLine("\nlost reader " + aEventArgs.Slot.CardTerminalName);
                if (CardTerminalManager.Singleton.SlotCount == 0)
                {
                    Console.WriteLine("Please connect reader so we can start accessing smart cards");
                    // start looking for reader insertion
                    // done automatically by the singleton. The singleton raises a "found reader" event if it finds a new reader.
                }
                else if (CardTerminalManager.Singleton.SlotCountMinimum + 1 > CardTerminalManager.Singleton.SlotCount)
                {
                    CardTerminalManager.Singleton.SlotCountMinimum -= 1;
                }
            }
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// We display reader properties available via CardTerminal class. Note that availability of
        /// these properties depends on the terminal.
        /// </summary>
        ///
        /// <param name="aCardSlot">    current slot, activated and with powered card. </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        static void DisplayReaderProperties(CardTerminalSlot aCardSlot)
        {
            string defaultInfo = "not available";
            // we now have a reader and a powered card

            Console.WriteLine("*** Card terminal info ***");

            string manufacturerName = aCardSlot.CardTerminal.ManufacturerName;
            if (manufacturerName == null) manufacturerName = defaultInfo;
            Console.WriteLine("manufacturer: " + manufacturerName);

            string productName = aCardSlot.CardTerminal.ProductName;
            if (productName == null) productName = defaultInfo;
            Console.WriteLine("name:      " + productName);

            string productVersion = aCardSlot.CardTerminal.ProductVersion;
            if (productVersion == null) productVersion = defaultInfo;
            Console.WriteLine("version: " + productVersion);

            string productSerialNumber = aCardSlot.CardTerminal.ProductSerialNumber;
            if (productSerialNumber == null) productSerialNumber = defaultInfo;
            Console.WriteLine("serial number: " + productSerialNumber);

            string productAdditionalInfo = aCardSlot.CardTerminal.ProductAdditionalInfo;
            if (productAdditionalInfo == null) productAdditionalInfo = defaultInfo;
            Console.WriteLine("additional info:  " + productAdditionalInfo);

            string physicalConnection = aCardSlot.CardTerminal.PhysicalConnection;
            if (physicalConnection == null) physicalConnection = defaultInfo;
            Console.WriteLine("physical connection:  " + physicalConnection);

            Console.WriteLine("*** End of card terminal info ***");

        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Displays number of readers currently available. Unless overwritted by the host application,
        /// the requested number of readers is at least one or the number of readers connected at program
        /// start. Whichever is greater.
        /// </summary>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        static void DisplayNumberOfAvailableReaders()
        {
            int slotCount = CardTerminalManager.Singleton.SlotCount;
            int slotCountMinimum = CardTerminalManager.Singleton.SlotCountMinimum;
            Console.WriteLine("Available readers: " + slotCount);
            if (slotCount < slotCountMinimum)
            {
                Console.WriteLine(String.Format("Looking for {0} more reader(s)", slotCountMinimum - slotCount));
            }
        }

       
    } //program
}

