// --------------------------------------------------------------------------------------------
// SEOS.cs
// This sample code reads SEOS card number on OMNIKEY 5x27CK and 5023 contactless card readers 
// using CardWerk's SmartCardAPI (professional)
// Copyright © 2017-2019 CardWerk Technologies
// -------------------------------------------------------------------------------------------- 
// 09NOV2019 MJ reduce number of dependencies
// 29JAN2018 MJ add timer for CSN, CN processing; tested with 5427 Gen1/2, OK5127 CK mini, and 5023 readers
//              fix: required by OK5427 CK Gen 2 reader: EndSession() with every BeginSession() 
// 31MAY2017 MJ copied from iCLASS console sample as starting point for SEOS
// 26APR2017 MJ copied and used as starting point for iCLASS sample that is CORE compatible
// 20APR2017 MJ thoroughly tested events
// 18APR2017 MJ initial version
//
using System;
using System.Threading;
using System.Diagnostics;
using Subsembly.SmartCard;
using SmartCardAPI.CardModule.HID.SEOS;
using SmartCardAPI.DataModule.Wiegand;

public class SEOS_main
{
    static int cardInsertions = 0;
    static string programInfo = String.Format("SEOS sample code rev.09NOV2019 \n{0}", SMARTCARDAPI.ApiVersionInfo);
    static bool ALWAYS_LOOK_FOR_MORE_CARD_READERS = true;

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
       
        string error = "";
        CardTerminalSlot aCardSlot = aEventArgs.Slot;
        CardActivationResult nActivationResult;
        Console.Beep();
        Console.Clear();
        Console.WriteLine(programInfo);
        Console.WriteLine(String.Format("  Total readers: {0}", CardTerminalManager.Singleton.SlotCount));
        Console.WriteLine(String.Format("Card insertions: {0}", ++cardInsertions));

        try
        {
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
            byte[] atr = aCard.GetATR();
            if (atr.Length == 0) throw new Exception("Invalid ATR");
            Console.WriteLine("ATR: " + CardHex.FromByteArray(atr, 0, atr.Length));

            bool supportedReader = readerName.Contains("5023") || readerName.Contains("5x27") || readerName.Contains("5127") || readerName.Contains("5427");

            if (!supportedReader)
            {
                
                throw new Exception(" unsupported reader " + readerName);

            }

                byte[] seosATR = CardHex.ToByteArray("3B80800101");
            if (CardHex.IsEqual(atr, seosATR))
            {
                SeosCard seos = new SeosCard(aCard);
                if(seos!=null)
                {
                    Console.WriteLine(seos.GetVersion());
                }
                if (seos.IsReady)
                {
                    byte[] randomUID = seos.GetCardSerialNumber();
                    if (randomUID == null)
                    {
                        Console.WriteLine("ERROR: no UID");
                    }
                    else
                    {
                        Console.WriteLine("SEOS random UID: " + CardHex.FromByteArray(randomUID));
                    }

                    byte[] rawPacsBits = seos.GetRawWiegandData();
                    if (rawPacsBits != null)
                    {
                        Console.WriteLine("raw PACS: " + CardHex.FromByteArray(rawPacsBits));
                        WiegandData wiegandData = new WiegandData(rawPacsBits);
                        long cardNumber = wiegandData.Extract(WiegandDataFormat.HID_H10301, WiegandDataIdentifier.CARD_NUMBER);
                        Console.WriteLine("SEOS card number (applying H10301 format): " + cardNumber);
                    }
                    else
                    {
                        // after a few (<10) successful SEOS PACS data reads, the reader just returns 0x6F00
                        // CMD FF70076B (VENDOR COMMAND) 30 06DCA434468563697AB6197521CECF497F01A3D79B3E70F1B5E651FE4B023164BD3F0CE5819CE25C0A1518DB9E2CA41F 00 
                        // RSP 6F00
                        // recovery only via reboot (tested with 5427 CKfw 4.02.0100)
                        // 5127CK Mini even goes mute (no more card detection at all) after first successful SEOS card mumber read!
                        // Console.WriteLine("ERROR: no PACS bits returned");
                        throw new Exception("no PACS bits returned. I/O interrupted?");
                    }

                }
                

            }
            else
            {

                Console.Beep(1000, 1000);
                Console.WriteLine("ERROR: this is not a SEOS card.");
            }
        }
        catch (Exception x)
        {
            Console.Beep(1000, 1000);
            error = "ERROR " + x.Message;
        }
        finally
        {
            // nottn
        }
        if (error != "")
        {
            Console.WriteLine(error);
        }
        Console.WriteLine("END of SEOS sample for HID SEOS cards");
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
            Console.WriteLine(String.Format("Looking for {0} more reader(s)",slotCountMinimum-slotCount));
        }
    }
}