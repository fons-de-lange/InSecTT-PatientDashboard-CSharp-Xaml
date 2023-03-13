// --------------------------------------------------------------------------------------------
// Mifare.cs
// This sample code shows how to access NXP Mifare Classic cards using CardWerk's SmartCardAPI (professional)
// CardWerk SmartCard API
// Copyright © 2017-2019 CardWerk Technologies
// -------------------------------------------------------------------------------------------- 
// 30OCT2019 MJ renamed sample code in preparation for new SDK format
// 08JUL2019 MJ moving some code to unit tests
// 04MAY2018 MJ copied from HelloCard.console
// 02JUN2017 MJ move informative code to HelloCardHelper.cs (card info, reference to more sample code)
// 24MAY2017 MJ add reference to storage card sample code if such acrd is detected
// 20APR2017 MJ thoroughly tested events
// 18APR2017 MJ initial version
//
// ============================= www.cardwerk.com =============================================
using System;
using System.Threading;
using System.Diagnostics;
using Subsembly.SmartCard;
using SmartCardAPI.CardModule.MifareClassic;

namespace SampleCode_MifareCard
{

    public class Program
    {
        static int cardInsertions = 0;
        static string programInfo = String.Format("HelloMifare rev.02AUG2019 \n{0}", SMARTCARDAPI.ApiVersionInfo);
        static bool ALWAYS_LOOK_FOR_MORE_CARD_READERS = true; // increases the number of readers we are looking for

        static int Main(string[] args)
        {
            Console.WriteLine(programInfo);

            // SmartCardAPI provides convenient events to track cards and readers. 
            CardTerminalManager.Singleton.CardInsertedEvent += new CardTerminalEventHandler(CardInsertedEvent);
            CardTerminalManager.Singleton.CardRemovedEvent += new CardTerminalEventHandler(CardRemovedEvent);
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
                while (!Console.KeyAvailable)
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

        /// <summary>
        /// This method is called when a card is inserted. We try to power it up and, show the ATR
        /// </summary>
        /// <param name="aSender"></param>
        /// <param name="aEventArgs"></param>	
        static void CardInsertedEvent(object aSender, CardTerminalEventArgs aEventArgs)
        {
            CardTerminalSlot aCardSlot = aEventArgs.Slot;
            CardActivationResult nActivationResult;
            Console.Beep();
            Console.Clear();
            Console.WriteLine(programInfo);
            Console.WriteLine(String.Format("  Total readers: {0}", CardTerminalManager.Singleton.SlotCount));
            Console.WriteLine(String.Format("Card insertions: {0}", ++cardInsertions));


            // Acquire any processor card (T=0 or T=1) that may be present in the given card terminal slot
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

            // =========================== ATR DETECTION ======================================
            // Every card accessed through PC/SC must return an Answer To Reset (ATR). 
            // this ATR is available through a cardHandle object
            byte[] atr = aCard.GetATR();
            if (atr.Length == 0) throw new Exception("Invalid ATR");
            Console.WriteLine("ATR: " + CardHex.FromByteArray(atr, 0, atr.Length));

            int keyStructure = -1; // invalid value
            int keyNumber = -1;    // invalid value


            MifareClassicCard mifareClassicCard = new MifareClassicCard(aCard);
            if (!mifareClassicCard.IsValid)
            {
                Console.WriteLine("ERROR - not a Mifare Classic card");
                return;
            }

            Console.WriteLine("OK - Mifare Classic detected"); 

            // ======================= LOAD KEY ===============================
            // unless the card key already resides on the reader, we need to load it
            // to the card reader first before we can use it by key reference
            // Setting key structure and key number correctly can be a little tricky. 
            // Smart card readers may require knowledge of vendor-specific key storage number/reference. 

            // Please let us know if you have keystructure/number settings that work well
            // with readers not shown oin this sample code.
            // =================================================================




            // default settings; works for readers like ACR122U
            keyStructure = (PCSC_PART3_KEY_STRUCTURE.PLAIN_TRANSMISSION | PCSC_PART3_KEY_STRUCTURE.VOLATILE_MEMORY);
            keyNumber = 0; // key storage location on reader; application does not need to know the exact storage location

            // HID/OMNIKEY readers:
            if (aCard.Slot.CardTerminalName.Contains("OMNIKEY"))
            {

                keyStructure = (PCSC_PART3_KEY_STRUCTURE.PLAIN_TRANSMISSION | PCSC_PART3_KEY_STRUCTURE.NON_VOLATILE_MEMORY);
                //keyStructure = (PCSC_PART3_KEY_STRUCTURE.PLAIN_TRANSMISSION | PCSC_PART3_KEY_STRUCTURE.VOLATILE_MEMORY); // in case you don't want to store the key permanently
                keyNumber = 0; // key storage location on reader; application does not need to know the exact storage location
            }

            // SCM/Identive readers:
            if (aCard.Slot.CardTerminalName.Contains("Contactless"))
            {
                keyStructure = (PCSC_PART3_KEY_STRUCTURE.PLAIN_TRANSMISSION | PCSC_PART3_KEY_STRUCTURE.VOLATILE_MEMORY);
                keyNumber = 0x60;
            }

            int keyType = PCSC_PART3.KEY_TYPE_MIFARE_A;


            // quick reader cfg to ensure we have a valid card key in the reader
            if (mifareClassicCard.LoadKey(keyStructure, keyNumber, MIFARE.TRANSPORT_KEY_NXP) == false)
            {
                throw new Exception("ERROR: Key Load failed.  keyStructure and keyNumber are reader specific. Please add the correct value according to reader specification.");
            }
            Console.WriteLine("OK - load default NXP transport key to card reader key slot 0.");
            int addressOfMemoryBlock = 4; // 0,1...
            int sector = 1; // 0,1....      
            
            keyType = PCSC_PART3.KEY_TYPE_MIFARE_A; // this is the most common key type
            if (mifareClassicCard.Authenticate(addressOfMemoryBlock, keyType, keyNumber) == false)
            {
                Console.WriteLine("WARNING: authentication under transport key failed. Repeating authentication.");
                if (mifareClassicCard.Authenticate(addressOfMemoryBlock, keyType, keyNumber) == false)
                {
                    Console.WriteLine("WARNING: authentication under key type A failed. Ending test sequence.");
                    return;
                }
                Console.WriteLine("OK-(2nd attempt) authenticate under key type A for access to sector " + sector + ", block " + addressOfMemoryBlock);
            }
            Console.WriteLine("OK-authenticate under key type A for access to sector " + sector + ", block " + addressOfMemoryBlock);

            byte[] data = mifareClassicCard.Read(addressOfMemoryBlock, 16); // Mifare card blocks are 16 byte long!               
            if (data == null)
            {
                Console.WriteLine("Can't access block " + addressOfMemoryBlock + " " + mifareClassicCard.ERROR);
            }
            else
            {
                Console.WriteLine("OK - card data: " + CardHex.FromByteArray(data));
            }

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="aSender"></param>
        /// <param name="aEventArgs"></param>	
        static void CardRemovedEvent(object aSender, CardTerminalEventArgs aEventArgs)
        {
            CardTerminalSlot aCardSlot = aEventArgs.Slot;
            Console.Beep(1000, 200);
            Console.Beep(1000, 200);
            Console.Write("\ncard removed from " + aCardSlot.CardTerminalName);
            Console.Write("\nplease insert card");
        }


        /// <summary>
        /// This method is called whenever SmartCardAPI indicates a terminal that has been 
        /// added to the current tracking list.
        /// </summary>
        /// <param name="aSender"></param>
        /// <param name="aEventArgs"></param>
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="aSender"></param>
        /// <param name="aEventArgs"></param>	
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



        /// <summary>
        /// We display reader properties available via CardTerminal class. Note that availability
        /// of these properties depends on the terminal.
        /// </summary>
        /// <param name="aCardSlot">current slot, activated and with powered card</param>
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

        /// <summary>
        /// Displays number of readers currently available. Unless overwritted by the host application,
        /// the requested number of readers is at least one or the number of readers connected at program 
        /// start. Whichever is greater.
        /// </summary>
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
}// namespace