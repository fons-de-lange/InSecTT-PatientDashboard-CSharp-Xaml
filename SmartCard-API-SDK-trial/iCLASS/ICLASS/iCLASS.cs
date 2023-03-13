// --------------------------------------------------------------------------------------------
// iCLASS.cs
// SmartCardAPI sample code for iCLASS cards on HID OMNIKEY readers using SmartCardAPI (professional)
// Copyright © 2017-2019 CardWerk Technologies
// -------------------------------------------------------------------------------------------- 
// 13NOV2019 MJ new: ReaderModule.OMNIKEY.dll
// 06JUN2019 MJ change: read has encryption flags as parameter; default '00' for no encryption
// 14MAR2019 MJ fix: 5422CL not detected as iCLASS supporting reader 
// 26APR2017 MJ copied and used as starting point for iCLASS sample that is CORE compatible
// 20APR2017 MJ thoroughly tested events
// 18APR2017 MJ initial version
//
using System;
using System.Threading;
using System.Diagnostics;
using Subsembly.SmartCard;
using SmartCardAPI.CardModule.HID.ICLASS; // this also requires OMNIKEY reader modules
using SmartCardAPI.DataModule.Wiegand;

public class iCLASS_main
{
    static int cardInsertions = 0;
    static string programInfo = String.Format("iCLASS sample code rev.13NOV2019 \n{0}", SMARTCARDAPI.ApiVersionInfo);
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

        try
        {

            // =========================== ATR DETECTION ======================================
            // Every card accessed through PC/SC must return an Answer To Reset (ATR). 
            // this ATR is available through a cardHandle object
            byte[] atr = aCard.GetATR();
            if (atr.Length == 0) throw new Exception("Invalid ATR");
            Console.WriteLine("ATR: " + CardHex.FromByteArray(atr, 0, atr.Length));

            PicoPassCard picoPass = new PicoPassCard(aCard); // HID legacy iCLASS card
            if (picoPass == null)
            {
               throw new Exception( "Can't instantiate iCLASS card module");
            }
            Console.WriteLine(picoPass.GetVersion());

            // check for iCLASS card
            if (!picoPass.IsICLASS)
            {
                throw new Exception("Invalid card");
            }

            // read CSN (same as UID for this card) via PicoPassCard class
            // if this fails for xchip-basded OMNIKEY readers, check EXE folder - ONMNIKEY sync API (scardsyn.dll, scardsynx64.dll) might be missing
            byte[] csn = picoPass.GetCardSerialNumber();
            Console.WriteLine("CSN: " + CardHex.FromByteArray(csn));

           bool READ_ICLASS_CARD_NUMBER = true;
           bool READ_WRITE_DEVELOPER_MEMORY = false; // to access memory area outside HID-issued PACS data application

           if (READ_ICLASS_CARD_NUMBER)
           {
               Console.WriteLine(string.Format("TEST==>> read PACS card number"));

               // todo: iCLASS SE detection


               // read HID PACS bit data
               byte[] pacsBits = picoPass.GetRawWiegandData(); // takes care of reader dependent secure channel protocols internally
               if (pacsBits == null)
               {
                   Console.WriteLine("ERROR: Reading PACS bit data failed. Is card using custom keys?");
               }
               else
               {

                   Console.WriteLine("PACS bits: " + CardHex.FromByteArray(pacsBits));
                   WiegandData wiegandData = new WiegandData(pacsBits);
                   if (wiegandData == null)
                   {
                       throw new Exception("Can't access Wiegand data module");
                   }
                   Console.WriteLine(wiegandData.GetVersion());
                   //int cardNumber = wiegandData.Extract(WiegandDataFormat.HID_CORP1000, WiegandDataIdentifier.CARD_NUMBER);
                   //Console.WriteLine("iCLASS card number (applying HID Corporate 1000 format): " + cardNumber);

                   long cardNumber = wiegandData.Extract(WiegandDataFormat.HID_H10301, WiegandDataIdentifier.CARD_NUMBER);
                   Console.WriteLine("iCLASS card number (applying H10301 format): " + cardNumber);
                   Console.WriteLine("-> We use this format because it is very popular.");
                   Console.WriteLine("-> Try a different format if this is not the expected data.");
               }
           }
           else
           {
               Console.WriteLine("iCLASS card number (skipped test)");
           }

            // iCLASS cards have an area outside HID-issued application that can be used as third-party card data
            if (READ_WRITE_DEVELOPER_MEMORY)
            {
                Console.WriteLine(string.Format("TEST==>> Rread/write iCLASS card developer memory area"));
                // we don't change any data here
                // instead we read current content and then write it back 
                // authenticate under key in key slot 0x23
                bool authenticated = picoPass.Authenticate(0, 0, 2, 0x23);

                if(!authenticated)
                {
                    throw new Exception("Authentication failed");
                }
                // read from block 19
                byte[] dataBlock19 = picoPass.Read(19);
                if (dataBlock19 == null)
                {
                    throw new Exception("Can't read card data");
                }
                Console.WriteLine(string.Format(" read block 19:     {0}", CardHex.FromByteArray(dataBlock19)));

                bool writeOK = picoPass.Write(19, dataBlock19);
                if (!writeOK)
                {
                    throw new Exception("Can't write card data");
                }
                Console.WriteLine(string.Format("write block 19:     {0}", CardHex.FromByteArray(dataBlock19)));
            }

        }
        catch(Exception ex)
        {
            Console.WriteLine("ERROR: " + ex.Message);
        }
        finally
        {
            aCard.Dispose();
        }
        Console.WriteLine("END of iCLASS card test.");
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
        if (slotCount == 0) slotCountMinimum = 1;
        Console.WriteLine("Available readers: " + slotCount);
        if (slotCount < slotCountMinimum)
        {
            Console.WriteLine(String.Format("Looking for {0} more reader(s)",slotCountMinimum-slotCount));
        }
    }
}