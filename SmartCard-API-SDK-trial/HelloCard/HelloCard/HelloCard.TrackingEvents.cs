using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Subsembly.SmartCard;
using SmartCardAPI.CardModule.CLICS;

namespace HelloCard.Console
{
    static class HelloCard
    {
        /// <summary>
        /// This method is called when a card is inserted. We try to power it up and, show the ATR
        /// </summary>
        /// <param name="aSender"></param>
        /// <param name="aEventArgs"></param>	
        public static void CardInsertedEvent(object aSender, CardTerminalEventArgs aEventArgs)
        {
            CardTerminalSlot aCardSlot = aEventArgs.Slot;
            CardActivationResult nActivationResult;
            Console.Beep();
            Console.Clear();
            // Acquire any processor card (T=0 or T=1) that may be present in the given card terminal slot
            string readerName = aCardSlot.CardTerminalName;
            Console.WriteLine("\ncard inserted in " + readerName);

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

            // do something here; we can exchange APDUs or call a CardModule that exposes card functionality of some news cards

            if (atr.Length == 20)
            {
                ClicsCard clicsCard = new ClicsCard(aCard);
                if (clicsCard.IsStorageCard)
                {
                    // have a look at HelloMifare, HelloNfcTag, HelloICLASS sample code for contactless storage cards
                    // we would for example read/write/load keys using NXP Mifare Classic card
                    Console.WriteLine(clicsCard.GetVersion());
                    Console.WriteLine("contactless storage card detected");
                    Console.WriteLine("CardName: " + clicsCard.CardName);

                    // here is an APDU that every contactless card supports
                    CardCommandAPDU getUID = new CardCommandAPDU(0xFF, 0xCA, 0, 0, 256);
                    CardResponseAPDU respose = aCard.SendCommand(getUID); // we are using the CardHendle object to send command APDU 
                    if (respose.IsSuccessful)
                    {
                        Console.WriteLine("UID=" + CardHex.FromByteArray(respose.GetData())); // the response APDU contains data (UID in this case)
                    }
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="aSender"></param>
        /// <param name="aEventArgs"></param>	
        public static void CardRemovedEvent(object aSender, CardTerminalEventArgs aEventArgs)
        {
            CardTerminalSlot aCardSlot = aEventArgs.Slot;
            Console.Beep(1000, 200);
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
        public static void TerminalFoundEvent(object aSender, CardTerminalEventArgs aEventArgs)
        {

            if (CardTerminalManager.Singleton.StartedUp)
            {
                DisplayNumberOfAvailableReaders();
                Console.WriteLine("Found reader: " + aEventArgs.Slot.CardTerminalName);
            }

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="aSender"></param>
        /// <param name="aEventArgs"></param>	
        public static void TerminalLostEvent(object aSender, CardTerminalEventArgs aEventArgs)
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
            }
        }

    }
    
        

}
