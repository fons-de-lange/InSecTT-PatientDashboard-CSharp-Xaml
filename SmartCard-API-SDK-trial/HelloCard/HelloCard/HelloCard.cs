// --------------------------------------------------------------------------------------------
// HelloCard.cs
// This sample code shows how to connect to a smart card system using SmartCardAPI (professional)
// Copyright © 2017-2019 CardWerk Technologies
// -------------------------------------------------------------------------------------------- 
// 21OCT2019 MJ improve minimum reader count 
// 30AUG2018 MJ built with smartcard.dll version 5.0.18.830 for RDP test on Windows Server 2008
// 02JUN2017 MJ move informative code to HelloCardHelper.cs (card info, reference to more sample code)
// 24MAY2017 MJ add reference to storage card sample code if such acrd is detected
// 20APR2017 MJ thoroughly tested events
// 18APR2017 MJ initial version
// ============================= www.cardwerk.com =============================================
using System;
using System.Threading;
using System.Diagnostics;
using Subsembly.SmartCard;
using SmartCardAPI.CardModule.CLICS;

namespace HelloCard
{

    public class Program
    {
        static int cardInsertions = 0;
        static string programInfo = String.Format("HelloCard rev.21OCT2019 \n{0}", SMARTCARDAPI.ApiVersionInfo);
        static bool ALWAYS_LOOK_FOR_MORE_CARD_READERS = false; // to support readers that are connected after program start
        static int readerCountAtStartup = 0;

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
                readerCountAtStartup = CardTerminalManager.Singleton.Startup(true); // true: auto register PC/SC readers
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
                    CardTerminalManager.Singleton.SlotCountMinimum = 1;
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
                Console.WriteLine("END of HelloCard");
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
            if (atr.Length == 20) // PCSC part 3 ATRs are 20 bytes long, so let's check for a contactless storage card
            {
                ClicsCard clicsCard = new ClicsCard(aCard);
                if(clicsCard==null)
                {
                    return;
                }
                if (clicsCard.IsStorageCard)
                {
                    HelloCardHelper.PrintCardInfo(clicsCard); // run a few simple card access tests for a few popular card types
                }
            }
            else
            {
                HelloCardHelper.PrintCardInfo(atr);
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
                if(ALWAYS_LOOK_FOR_MORE_CARD_READERS && CardTerminalManager.Singleton.SlotCount == CardTerminalManager.Singleton.SlotCountMinimum)
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
        /// Displays number of readers currently available. Unless overwritten by the host application,
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